using System.Diagnostics;
using System.Security.Cryptography;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
///   Freshness verification for the committed i18n code generation.
/// </summary>
/// <remarks>
///   <c>dotnet elg gen</c> writes <c>Properties/Lang*.g.cs</c> from the locale JSON and the result is
///   committed, so a locale edit that skipped the generator ships an artifact whose dictionaries are
///   frozen at the previous value and whose typed sections lack the new key.  The generator is the only
///   oracle for what that artifact should hold — re-deriving the keys here would be a second copy of its
///   rules — so this target regenerates every <c>&lt;Elg&gt;</c> project and compares the bytes.
/// </remarks>
partial class Build
{
  /// <summary>The extension the generator emits its artifacts with.</summary>
  const string GeneratedFilePattern = "*.g.cs";

  /// <summary>
  ///   Regenerates every <c>&lt;Elg&gt;</c> project and fails when the committed output differs.
  /// </summary>
  Target VerifyGeneratedLang => _ => _
      .Executes(() =>
      {
        EnsureGeneratorIsInstalled();

        var projects = FindElgProjects();
        if (projects.Count == 0)
          throw new InvalidOperationException(
              "No project declares <Elg>, so the discovery scan found nothing to verify. Fix the scan " +
              "before trusting this target.");

        var before = SnapshotGenerated(projects);

        foreach (var project in projects)
        {
          Log.Information("Regenerating i18n code for {Project}", project);
          DotNet($"elg gen --project \"{project}\"");
        }

        var stale = ChangedFiles(before, SnapshotGenerated(projects));
        if (stale.Count > 0)
        {
          foreach (var file in stale)
            Log.Error("Differs from the generator's output: {File}", file);

          throw new InvalidOperationException(
              $"{stale.Count} committed generated file(s) no longer match `dotnet elg gen`. Run the " +
              "generator and commit the result: the locale JSON moved and the artifact did not.");
        }

        Log.Information("Generated i18n code is fresh ({Count} project(s)).", projects.Count);
      });

  /// <summary>The generator is the oracle, so a missing tool has to fail with the install line.</summary>
  static void EnsureGeneratorIsInstalled()
  {
    using var probe = Process.Start(new ProcessStartInfo("dotnet", "elg --version")
    {
      RedirectStandardOutput = true,
      RedirectStandardError = true,
    });

    probe!.WaitForExit();
    if (probe.ExitCode != 0)
      throw new InvalidOperationException(
          "The i18n generator is not installed. Install it with: " +
          "dotnet tool install -g everlong.globalization.tools");
  }

  /// <summary>Every project that opted into code generation, discovered rather than listed by hand.</summary>
  List<AbsolutePath> FindElgProjects()
  {
    string[] roots = [RootDirectory / "src", RootDirectory / "examples", RootDirectory / "tests"];

    return roots
        .Where(root => Directory.Exists(root))
        .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
        .Where(path => !IsBuildOutput(path))
        .Where(path => File.ReadAllText(path).Contains("<Elg>", StringComparison.Ordinal))
        .Select(path => (AbsolutePath)path)
        .OrderBy(path => path.ToString(), StringComparer.OrdinalIgnoreCase)
        .ToList();
  }

  /// <summary>A content hash per generated file, so the comparison does not read git state.</summary>
  static Dictionary<string, string> SnapshotGenerated(IEnumerable<AbsolutePath> projects)
  {
    var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var project in projects)
    {
      foreach (var file in Directory
                   .EnumerateFiles(project.Parent, GeneratedFilePattern, SearchOption.AllDirectories)
                   .Where(path => !IsBuildOutput(path)))
      {
        snapshot[file] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
      }
    }

    return snapshot;
  }

  /// <summary>The files a regeneration added, removed or rewrote.</summary>
  static List<string> ChangedFiles(Dictionary<string, string> before, Dictionary<string, string> after)
    => before.Keys
        .Concat(after.Keys)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(path => !before.TryGetValue(path, out var was) ||
                       !after.TryGetValue(path, out var now) ||
                       !string.Equals(was, now, StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToList();

  /// <summary>Build output is never committed, so it stays out of the comparison.</summary>
  static bool IsBuildOutput(string path)
    => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(segment => segment is "obj" or "bin");
}
