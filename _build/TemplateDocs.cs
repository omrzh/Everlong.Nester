using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

/// <summary>
///   Documentation verification for the template tour.
/// </summary>
/// <remarks>
///   <c>readme.md</c> is the NuGet package readme for <c>Everlong.Nester.Avalonia</c> / <c>.Wpf</c> and
///   for the templates package, so it is rendered off a package page where a relative path cannot
///   resolve and a mistyped reference label renders as plain text.  The tour is the tutorial, and it
///   names files in the *generated* project — a product of <c>PackTemplates</c> staging
///   (<c>Template.Shared</c> + <c>Template.Avalonia</c> + the <c>_build/templates</c> stubs), not of the
///   <c>examples/</c> tree.  These checks are the mechanical failure classes of that setup; whether a
///   sentence is still *true* stays a review question.
/// </remarks>
partial class Build
{
  static readonly string[] GuideDocuments =
  [
    @"docs\guide\get-started.md",
    @"docs\guide\navigation.md",
    @"docs\guide\interaction.md",
  ];

  const string TourHeading = "### Tour";
  const string FenceLine = "```";

  /// <summary>
  ///   Verifies the readme's tour paths against the staged template content, and the markdown rules a
  ///   package page cannot survive breaking.
  /// </summary>
  Target VerifyTemplateDocs => _ => _
      .DependsOn(PackTemplates)
      .Executes(() =>
      {
        var readme = RootDirectory / "readme.md";
        AbsolutePath[] templateRoots =
        [
          BuildTempRoot / "templates" / "content" / "NesterApp.Avalonia",
          BuildTempRoot / "templates" / "content" / "NesterApp.Wpf",
        ];

        foreach (var root in templateRoots)
        {
          if (!Directory.Exists(root))
            throw new InvalidOperationException(
                $"Staged template content is missing: {root}. PackTemplates must stage it first.");
        }

        var markdown = File.ReadAllText(readme);
        var violations = new List<string>();

        violations.AddRange(FindMissingTourPaths(markdown, templateRoots));
        violations.AddRange(FindRelativeLinks(readme.Name, markdown));
        violations.AddRange(FindUnresolvedReferenceLabels(readme.Name, markdown));

        foreach (var document in new[] { readme }.Concat(GuideDocuments.Select(d => RootDirectory / d)))
        {
          if (!File.Exists(document))
          {
            violations.Add($"missing document: {document}");
            continue;
          }

          violations.AddRange(FindUnbalancedFences(document.Name, File.ReadAllText(document)));
        }

        if (violations.Count > 0)
        {
          foreach (var violation in violations)
            Log.Error("{Violation}", violation);

          throw new InvalidOperationException(
              $"{violations.Count} documentation violation(s). Fix them before publishing: the package " +
              "page cannot resolve relative paths, and the tour must name files the generated project has.");
        }

        Log.Information("Template documentation verified (tour paths, links, labels, fences).");
      });

  /// <summary>Every file the readme tour names must exist in the staged template content.</summary>
  static IEnumerable<string> FindMissingTourPaths(string readme, AbsolutePath[] templateRoots)
  {
    var start = readme.IndexOf(TourHeading, StringComparison.Ordinal);
    if (start < 0)
    {
      yield return $"the readme has no '{TourHeading}' section";
      yield break;
    }

    var end = readme.IndexOf("\n---", start, StringComparison.Ordinal);
    var tour = end < 0 ? readme[start..] : readme[start..end];

    foreach (var span in Regex.Matches(tour, @"`([^`\n]+)`").Select(m => m.Groups[1].Value))
    {
      if (span.StartsWith('['))
        continue;
      if (!span.Contains('/') && !Regex.IsMatch(span, @"\.(cs|axaml|xaml|json)$"))
        continue;

      // A row may abbreviate a path to its file name, or name a glob (`Dialogs/*.cs`).
      var isGlob = span.Contains('*');
      var probe = isGlob ? span[..span.IndexOf('*')].TrimEnd('/') : span;
      if (probe.Length == 0)
        continue;                                     // a glob with no fixed prefix — nothing to check

      var found = isGlob
          ? templateRoots.Any(root => Directory.Exists(Path.Combine(root, probe)))
          : probe.Contains('/')
              ? templateRoots.Any(root => File.Exists(Path.Combine(root, probe)) ||
                                         Directory.Exists(Path.Combine(root, probe)))
              : templateRoots.Any(root => Directory.Exists(root) &&
                                          Directory.EnumerateFiles(root, probe, SearchOption.AllDirectories).Any());

      if (!found)
        yield return $"tour path is not in the staged template content: {span}";
    }
  }

  /// <summary>A package page cannot resolve a relative link.</summary>
  static IEnumerable<string> FindRelativeLinks(string name, string markdown)
    => Regex.Matches(markdown, @"\]\(([^)]+)\)")
        .Cast<Match>()
        .Select(m => m.Groups[1].Value)
        .Where(target => !target.StartsWith("http://", StringComparison.Ordinal)
                         && !target.StartsWith("https://", StringComparison.Ordinal)
                         && !target.StartsWith('#'))
        .Select(target => $"{name}: relative link target — use an absolute github.com URL: {target}");

  /// <summary>A mistyped reference label renders as plain text without any warning.</summary>
  static IEnumerable<string> FindUnresolvedReferenceLabels(string name, string markdown)
  {
    var definitions = Regex.Matches(markdown, @"^\[([^\]]+)\]:\s*(\S+)", RegexOptions.Multiline)
        .Cast<Match>()
        .ToList();
    var labels = definitions
        .Select(m => m.Groups[1].Value.ToLowerInvariant())
        .ToHashSet();

    foreach (var definition in definitions)
    {
      var target = definition.Groups[2].Value;
      if (!target.StartsWith("http://", StringComparison.Ordinal) &&
          !target.StartsWith("https://", StringComparison.Ordinal))
      {
        yield return $"{name}: reference definition [{definition.Groups[1].Value}] is not absolute: {target}";
      }
    }

    // Code spans come out first: `[Routable]` in a span is an attribute name, not a link.
    var body = Regex.Replace(markdown, "```.*?```", string.Empty, RegexOptions.Singleline);
    body = Regex.Replace(body, "`[^`\n]*`", string.Empty);
    body = Regex.Replace(body, @"^\[[^\]]+\]:.*$", string.Empty, RegexOptions.Multiline);

    foreach (var match in Regex.Matches(body, @"(!?)\[([^\]\n]*)\](\[[^\]\n]*\]|)").Cast<Match>())
    {
      if (match.Groups[1].Value == "!")
        continue;                                     // an image, not a reference

      var label = match.Groups[3].Value.Length > 0
          ? match.Groups[3].Value[1..^1]
          : match.Groups[2].Value;

      if (label.Length > 0 && !labels.Contains(label.ToLowerInvariant()))
        yield return $"{name}: reference link [{label}] has no definition";
    }
  }

  /// <summary>An unbalanced fence swallows every section behind it on a rendered page.</summary>
  static IEnumerable<string> FindUnbalancedFences(string name, string markdown)
  {
    var fences = markdown
        .Split('\n')
        .Count(line => line.TrimStart().StartsWith(FenceLine, StringComparison.Ordinal));

    if (fences % 2 != 0)
      yield return $"{name}: unbalanced code fences ({fences} fence lines)";
  }
}
