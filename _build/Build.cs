using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
  public static int Main() => Execute<Build>(x => x.Publish);

  AbsolutePath ArtifactsDir => RootDirectory / "artifacts";
  AbsolutePath BuildTempRoot => TemporaryDirectory / "_build";

  static readonly string[] FullReleaseProjects =
  [
    @"src\Everlong.Nester\Everlong.Nester.csproj",
    @"src\Everlong.Nester.Avalonia\Everlong.Nester.Avalonia.csproj",
    @"src\Everlong.Nester.Wpf\Everlong.Nester.Wpf.csproj",
  ];

  static readonly string[] TemplateValidationProjects =
  [
    @"examples\Template.Avalonia\Template.Avalonia.csproj",
    @"examples\Template.Wpf\Template.Wpf.csproj",
  ];

  static readonly string[] AnalyzerProjects =
  [
    @"src\Everlong.Nester.Generators\Everlong.Nester.Generators.csproj",
    @"src\Everlong.Nester.CodeFixers\Everlong.Nester.CodeFixers.csproj",
  ];

  // ─── Targets ────────────────────────────────────────────────────────────

  Target CleanOutput => _ => _
      .Executes(() =>
      {
        if (Directory.Exists(BuildTempRoot))
        {
          Log.Information("Cleaning {BuildTempRoot}...", BuildTempRoot);
          ClearReadOnlyRecursively(BuildTempRoot);
          Directory.Delete(BuildTempRoot, recursive: true);
        }

        if (Directory.Exists(ArtifactsDir))
        {
          Log.Information("Cleaning {ArtifactsDir}...", ArtifactsDir);
          ClearReadOnlyRecursively(ArtifactsDir);
          Directory.Delete(ArtifactsDir, recursive: true);
        }
        Directory.CreateDirectory(ArtifactsDir);
        Directory.CreateDirectory(BuildTempRoot);
      });

  Target ValidateTemplates => _ => _
      .DependsOn(CleanOutput)
      .Executes(() =>
      {
        Log.Information("Validating template example projects in Release...");
        for (var i = 0; i < TemplateValidationProjects.Length; i++)
        {
          var project = TemplateValidationProjects[i];
          var projectPath = RootDirectory / project;
          var outputPath = GetBuildOutputPath(project);
          Directory.CreateDirectory(outputPath);

          Log.Information("[{Index}/{Total}] Building {Project}", i + 1, TemplateValidationProjects.Length, project);
          DotNetBuild(s => s
              .SetProjectFile(projectPath)
              .SetConfiguration("Release")
              .SetVerbosity(DotNetVerbosity.minimal)
              .SetProperty("BaseOutputPath", outputPath + @"\"));
        }
      });

  Target PackTemplates => _ => _
      .DependsOn(CleanOutput, PrepareAnalyzerArtifacts, ValidateTemplates, VerifyFontCoverage)
      .Executes(() =>
      {
        var stage = BuildTempRoot / "templates";
        stage.CreateOrCleanDirectory();

        var contentRoot = stage / "content";
        var avalonia = contentRoot / "NesterApp.Avalonia";
        var wpf = contentRoot / "NesterApp.Wpf";

        Log.Information("Staging Avalonia template content...");
        (RootDirectory / "examples" / "Template.Shared").Copy(avalonia, ExistsPolicy.MergeAndOverwrite);
        // `Assets/Fonts` is excluded with its `CjkFontCollection`: the generated app is the desktop
        // host, which resolves CJK through the platform, so the class would register an empty
        // collection and the face would be 0.33 MB nothing loads.  The examples keep both for the
        // Browser and Android hosts.
        (RootDirectory / "examples" / "Template.Avalonia").Copy(avalonia, ExistsPolicy.MergeAndOverwrite,
            excludeDirectory: d => d.Name is "bin" or "obj" or "nester-topology" or "Fonts",
            excludeFile: f => f.Name is "Template.Avalonia.csproj");
        File.Copy(RootDirectory / "_build" / "templates" / "avalonia" / "NesterApp.csproj", avalonia / "NesterApp.csproj");
        File.Copy(RootDirectory / "_build" / "templates" / "avalonia" / "app.manifest", avalonia / "app.manifest");
        File.Copy(RootDirectory / "_build" / "templates" / "avalonia" / "Program.cs", avalonia / "Program.cs");

        Log.Information("Staging WPF template content...");
        (RootDirectory / "examples" / "Template.Shared").Copy(wpf, ExistsPolicy.MergeAndOverwrite);
        (RootDirectory / "examples" / "Template.Wpf").Copy(wpf, ExistsPolicy.MergeAndOverwrite,
            excludeDirectory: d => d.Name is "bin" or "obj" or "nester-topology",
            excludeFile: f => f.Name is "Template.Wpf.csproj");
        File.Copy(RootDirectory / "_build" / "templates" / "wpf" / "NesterApp.csproj", wpf / "NesterApp.csproj");

        File.Copy(RootDirectory / "readme.md", stage / "readme.md");
        File.Copy(RootDirectory / "_build" / "templates" / "Everlong.Nester.Templates.csproj", stage / "Everlong.Nester.Templates.csproj");

        // The staged content keeps literal package versions (a generated project has no
        // central package management of its own), so the staging tree opts out of the
        // repo's.  It sits above `content/`, never inside it: nothing reaches the
        // template package.
        File.WriteAllText(stage / "Directory.Packages.props",
                          """
                          <Project>
                            <PropertyGroup>
                              <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                            </PropertyGroup>
                          </Project>

                          """);

        // The dotnet-new template engine evaluates `#if`/`#endif` in file content
        // against template symbols; an unknown symbol silently strips the block
        // (a whole guarded file becomes empty).  Fail the pack instead of shipping
        // a corrupt template.
        foreach (var file in stage.GlobFiles("content/**/*.cs"))
        {
          if (Regex.IsMatch(File.ReadAllText(file), @"^\s*#(if|elif|else|endif)\b", RegexOptions.Multiline))
          {
            throw new InvalidOperationException(
                $"Template content must not contain preprocessor conditionals — the template engine would strip the block: {file}");
          }
        }

        var version = GetAppVersion();
        Log.Information("Injecting version {Version} into template projects...", version);
        (avalonia / "NesterApp.csproj").UpdateText(x => x.Replace("__NESTER_VERSION__", version));
        (wpf / "NesterApp.csproj").UpdateText(x => x.Replace("__NESTER_VERSION__", version));

        Log.Information("Packing Everlong.Nester.Templates v{Version}...", version);
        DotNetPack(s => s
            .SetProject(stage / "Everlong.Nester.Templates.csproj")
            .SetConfiguration("Release")
            .SetOutputDirectory(ArtifactsDir)
            .SetVerbosity(DotNetVerbosity.minimal)
            .SetProperty("Version", version)
            .SetProperty("PackageVersion", version));
      });

  Target PrepareAnalyzerArtifacts => _ => _
      .Executes(() =>
      {
        Log.Information("Preparing analyzer artifacts in default bin paths...");
        foreach (var project in AnalyzerProjects)
        {
          var projectPath = RootDirectory / project;
          DotNetBuild(s => s
              .SetProjectFile(projectPath)
              .SetConfiguration("Release")
              .SetVerbosity(DotNetVerbosity.minimal));
        }
      });

  Target FullReleaseBuild => _ => _
      .DependsOn(CleanOutput, PrepareAnalyzerArtifacts)
      .Executes(() =>
      {
        var projects = GetOrderedPackableProjects();
        for (var i = 0; i < FullReleaseProjects.Length; i++)
        {
          var project = NormalizePath(FullReleaseProjects[i]);
          if (!projects.Contains(project, StringComparer.OrdinalIgnoreCase))
            continue;

          var projectPath = RootDirectory / project;
          var outputPath = GetBuildOutputPath(project);
          Directory.CreateDirectory(outputPath);

          Log.Information("[{Index}/{Total}] Building full Release targets: {Project}", i + 1, FullReleaseProjects.Length, project);
          DotNetBuild(s => s
              .SetProjectFile(projectPath)
              .SetConfiguration("Release")
              .SetVerbosity(DotNetVerbosity.minimal)
              .SetProperty("BaseOutputPath", outputPath + @"\"));
        }
      });

  Target PackProjects => _ => _
      .DependsOn(ValidateTemplates, FullReleaseBuild)
      .Executes(() =>
      {
        var projects = GetOrderedPackableProjects();

        Log.Information("Cleaning {Count} project(s)...", projects.Length);
        foreach (var project in projects)
        {
          var projectPath = RootDirectory / project;
          var outputPath = GetBuildOutputPath(project);
          Directory.CreateDirectory(outputPath);

          DotNetClean(s => s
              .SetProject(projectPath)
              .SetConfiguration("Release")
              .SetVerbosity(DotNetVerbosity.minimal)
              .SetProperty("BaseOutputPath", outputPath + @"\"));
        }

        Log.Information("Building and packing {Count} project(s)...", projects.Length);
        for (var i = 0; i < projects.Length; i++)
        {
          var project = projects[i];
          var projectPath = RootDirectory / project;
          var outputPath = GetBuildOutputPath(project);

          Log.Information("[{Index}/{Total}] Packing {Project}", i + 1, projects.Length, project);

          DotNetBuild(s => s
              .SetProjectFile(projectPath)
              .SetConfiguration("Release")
              .SetVerbosity(DotNetVerbosity.minimal)
              .SetProperty("BaseOutputPath", outputPath + @"\"));

          DotNetPack(s => s
              .SetProject(projectPath)
              .SetConfiguration("Release")
              .SetOutputDirectory(ArtifactsDir)
              .SetVerbosity(DotNetVerbosity.minimal)
              .EnableNoBuild()
              .SetProperty("BaseOutputPath", outputPath + @"\"));
        }
      });

  Target Publish => _ => _
      .DependsOn(PackProjects, PackTemplates, VerifyTemplateDocs, VerifyGeneratedLang)
      .Executes(() =>
      {
        if (Directory.Exists(BuildTempRoot))
        {
          ClearReadOnlyRecursively(BuildTempRoot);
          Directory.Delete(BuildTempRoot, recursive: true);
        }

        var nupkgs = ArtifactsDir.GlobFiles("*.nupkg");
        Log.Information("Pack output: {ArtifactsDir}", ArtifactsDir);
        Log.Information("Successfully published {Count} package(s).", nupkgs.Count);
      });

  // ─── Helpers ────────────────────────────────────────────────────────────

  string GetAppVersion()
  {
    var props = XDocument.Load(RootDirectory / "NesterVersion.props");
    return props.Descendants("AppVersion").First().Value.Trim();
  }

  static void ClearReadOnlyRecursively(string path)
  {
    foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
    {
      var attributes = File.GetAttributes(file);
      if ((attributes & FileAttributes.ReadOnly) != 0)
        File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
    }
  }

  string GetBuildOutputPath(string projectRelativePath)
  {
    var safeName = Regex.Replace(projectRelativePath, @"[\\/:*?""<>| ]", "_");
    return Path.Combine(BuildTempRoot, safeName);
  }

  static string NormalizePath(string path) =>
      path.Replace('/', '\\').TrimStart('.', '\\');

  string[] GetOrderedPackableProjects()
  {
    var solutionPath = RootDirectory / "Everlong.Nester.slnx";
    var doc = XDocument.Load(solutionPath);

    var srcFolder = doc.Descendants("Folder")
        .FirstOrDefault(f => (string?)f.Attribute("Name") == "/src/")
        ?? throw new InvalidOperationException("Cannot find /src/ folder in solution");

    var projects = new List<string>();
    foreach (var projectNode in srcFolder.Descendants("Project"))
    {
      var path = (string?)projectNode.Attribute("Path");
      if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        continue;

      var fullPath = RootDirectory / path;
      if (!fullPath.FileExists())
        continue;

      var content = File.ReadAllText(fullPath);
      if (Regex.IsMatch(content, @"(?is)<IsPackable>\s*false\s*</IsPackable>"))
        continue;
      if (!Regex.IsMatch(content, @"(?is)<PackageId>.+?</PackageId>"))
        continue;

      projects.Add(NormalizePath(path));
    }

    return TopologicalSort(projects);
  }

  string[] TopologicalSort(List<string> projects)
  {
    var projectSet = new HashSet<string>(projects, StringComparer.OrdinalIgnoreCase);
    var dependsOn = projects.ToDictionary(p => p, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    var dependents = projects.ToDictionary(p => p, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    var inDegree = projects.ToDictionary(p => p, _ => 0, StringComparer.OrdinalIgnoreCase);

    foreach (var project in projects)
    {
      var projectPath = (string)(RootDirectory / project);
      var projectDir = Path.GetDirectoryName(projectPath)!;
      var projectDoc = XDocument.Load(projectPath);

      foreach (var projRef in projectDoc.Descendants("ProjectReference"))
      {
        var include = (string?)projRef.Attribute("Include");
        if (string.IsNullOrWhiteSpace(include))
          continue;

        var refFull = Path.GetFullPath(Path.Combine(projectDir, include));
        if (!refFull.StartsWith(RootDirectory, StringComparison.OrdinalIgnoreCase))
          continue;

        var refRelative = NormalizePath(Path.GetRelativePath(RootDirectory, refFull));
        if (!projectSet.Contains(refRelative))
          continue;
        if (!dependsOn[project].Add(refRelative))
          continue;

        dependents[refRelative].Add(project);
        inDegree[project]++;
      }
    }

    var ready = new List<string>(projects.Where(p => inDegree[p] == 0).OrderBy(p => p));
    var ordered = new List<string>(projects.Count);

    while (ready.Count > 0)
    {
      var next = ready.OrderBy(p => p).First();
      ready.Remove(next);
      ordered.Add(next);

      foreach (var dep in dependents[next].OrderBy(p => p))
      {
        if (--inDegree[dep] == 0)
          ready.Add(dep);
      }
    }

    if (ordered.Count != projects.Count)
      throw new InvalidOperationException("Circular dependency detected among packable projects.");

    return [.. ordered];
  }
}
