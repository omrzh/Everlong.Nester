using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Tests.Helpers;

/// <summary>
///   Builds a single concatenated text snapshot from a generator run — the
///   diagnostics followed by every generated tree.  Text snapshots (`.verified.txt`)
///   never participate in compilation and stay readable in the IDE.
/// </summary>
internal static class SnapshotBuilder
{
  /// <summary>
  ///   The generator's own version inside a <c>[GeneratedCode]</c> attribute.  It moves with the package,
  ///   so it is scrubbed to <c>&lt;ASSEMBLY_VERSION&gt;</c> — the placeholder CommunityToolkit.Mvvm's
  ///   snapshots use for the same attribute — and a release does not rewrite every baseline.
  /// </summary>
  private static readonly Regex GeneratedCodeVersion = new(
    """\[global::System\.CodeDom\.Compiler\.GeneratedCode\("([^"]*)", "[^"]*"\)\]""",
    RegexOptions.Compiled);

  /// <summary>
  ///   The separator the tree path is snapshotted with.  Roslyn composes it from the generator's
  ///   coordinates with the platform directory separator, so a Windows-authored baseline would
  ///   otherwise never match a Linux run.
  /// </summary>
  private const char SnapshotPathSeparator = '\\';

  internal static string Build(GeneratorDriverRunResult runResult)
  {
    var parts = new List<string>();

    if (runResult.Diagnostics.Length > 0)
    {
      parts.Add(string.Join(Environment.NewLine,
        runResult.Diagnostics.Select(static d => $"{d.Severity} {d.Id}: {d.GetMessage()}")));
    }

    parts.AddRange(runResult.GeneratedTrees
      .Select(static t => (Path: t.FilePath.Replace('/', SnapshotPathSeparator), Tree: t))
      .OrderBy(static t => t.Path, StringComparer.Ordinal)
      .Select(static t => $"// {t.Path}{Environment.NewLine}{t.Tree.GetText()}"));

    return GeneratedCodeVersion.Replace(
      string.Join(Environment.NewLine + Environment.NewLine, parts),
      static match => $"[global::System.CodeDom.Compiler.GeneratedCode(\"{match.Groups[1].Value}\", <ASSEMBLY_VERSION>)]");
  }
}
