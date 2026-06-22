using System.Collections.Immutable;
using System.Reflection;
using Everlong.Nester.Generators.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Everlong.Nester.Tests.Analyzers;

/// <summary>
///   The partial precondition for code generation: the annotated type itself (NSTR0001) and every
///   type enclosing it (NSTR0008).
/// </summary>
public class PartialKeywordAnalyzerTests
{
  private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
  private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
  private static readonly MetadataReference SystemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
  private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

  private static string BuildSource(string pageBody) => $$"""
using System;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Routing
{
  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public class RoutableAttribute : Attribute;
}

namespace Everlong.Nester.Presentation
{
  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public class ViewLocatorAttribute : Attribute;
}

namespace TestApp
{
{{pageBody}}
}
""";

  private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string pageBody)
  {
    var compilation = CSharpCompilation.Create(
      "TestApp",
      [CSharpSyntaxTree.ParseText(BuildSource(pageBody))],
      [CorlibReference, SystemRuntimeReference, SystemCollectionsReference, NetStandardReference],
      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    var analyzer = new PartialKeywordAnalyzer();
    return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)).GetAnalyzerDiagnosticsAsync();
  }

  private static string ReportedText(Diagnostic diagnostic)
    => diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan);

  [Fact]
  public async Task AnnotatedTypeNotPartial_ReportsNstr0001()
  {
    var diagnostics = await AnalyzeAsync("""
  [Routable]
  public class Page {}
""");

    var diagnostic = Assert.Single(diagnostics);
    Assert.Equal("NSTR0001", diagnostic.Id);
    Assert.Equal("Page", ReportedText(diagnostic));
  }

  [Fact]
  public async Task NestedTarget_NonPartialEnclosingType_ReportsNstr0008()
  {
    var diagnostics = await AnalyzeAsync("""
  public class Container
  {
    [Routable]
    public partial class Page {}
  }
""");

    var diagnostic = Assert.Single(diagnostics);
    Assert.Equal("NSTR0008", diagnostic.Id);
    Assert.Equal("Container", ReportedText(diagnostic));
    Assert.Contains("Container", diagnostic.GetMessage());
    Assert.Contains("Page", diagnostic.GetMessage());
  }

  [Fact]
  public async Task NestedTarget_TargetAndEnclosingTypeNotPartial_ReportsBoth()
  {
    var diagnostics = await AnalyzeAsync("""
  public class Container
  {
    [Routable]
    public class Page {}
  }
""");

    Assert.Equal(2, diagnostics.Length);
    Assert.Contains(diagnostics, static d => d.Id == "NSTR0001" && d.GetMessage().Contains("Page"));
    Assert.Contains(diagnostics, static d => d.Id == "NSTR0008" && d.GetMessage().Contains("Container"));
  }

  [Fact]
  public async Task DeeplyNestedTarget_ReportsEveryEnclosingType()
  {
    var diagnostics = await AnalyzeAsync("""
  public class Outer
  {
    public class Inner
    {
      [Routable]
      public partial class Page {}
    }
  }
""");

    Assert.Equal(2, diagnostics.Length);
    Assert.All(diagnostics, static d => Assert.Equal("NSTR0008", d.Id));
    Assert.Equal(["Inner", "Outer"], diagnostics.Select(ReportedText).Order());
  }

  [Fact]
  public async Task NestedTarget_AllTypesPartial_IsClean()
  {
    var diagnostics = await AnalyzeAsync("""
  public partial class Container
  {
    [Routable]
    public partial class Page {}
  }
""");

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task NestedTypeWithoutGenerationAttribute_IsClean()
  {
    var diagnostics = await AnalyzeAsync("""
  public class Container
  {
    public class Page {}
  }
""");

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task NestedViewLocatorTrigger_NonPartialEnclosingType_ReportsNstr0008()
  {
    var diagnostics = await AnalyzeAsync("""
  public class Container
  {
    [ViewLocator]
    public partial class AppViewProvider {}
  }
""");

    var diagnostic = Assert.Single(diagnostics);
    Assert.Equal("NSTR0008", diagnostic.Id);
    Assert.Equal("Container", ReportedText(diagnostic));
  }
}
