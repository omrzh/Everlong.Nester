using System.Collections.Immutable;
using System.Reflection;
using Everlong.Nester.Generators.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Everlong.Nester.Tests.Analyzers;

/// <summary>
///   The [ViewFor&lt;T&gt;]-anchored instantiation rules: a view the locator
///   instantiates must be a class it can name (NSTR1004) with a public or
///   internal parameterless constructor (NSTR1003).  Reports only on
///   [ViewFor]-annotated, non-abstract classes declared in source.
/// </summary>
public class ViewInstantiationAnalyzerTests
{
  private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
  private static readonly MetadataReference SystemRuntimeReference = MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location);
  private static readonly MetadataReference SystemCollectionsReference = MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location);
  private static readonly MetadataReference NetStandardReference = MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location);

  private static string BuildSource(string pageBody) => $$"""
using System;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Presentation
{
  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public class ViewForAttribute<TViewModel> : Attribute where TViewModel : notnull;
}

namespace Avalonia.Controls
{
  public class Control {}
}

namespace TestApp
{
  public class ViewModel {}

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

    var analyzer = new ViewInstantiationAnalyzer();
    return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer)).GetAnalyzerDiagnosticsAsync();
  }

  [Fact]
  public async Task ViewForView_NoParameterlessConstructor_ReportsNstr1020()
  {
    var page = """
  [ViewFor<ViewModel>]
  public class ParametricView : Avalonia.Controls.Control
  {
    public ParametricView(string title) {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    var diag = Assert.Single(diagnostics);
    Assert.Equal("NSTR1003", diag.Id);
    Assert.Contains("ParametricView", diag.GetMessage());
  }

  [Fact]
  public async Task ViewForView_PublicParameterlessConstructor_IsClean()
  {
    var page = """
  [ViewFor<ViewModel>]
  public class PublicView : Avalonia.Controls.Control
  {
    public PublicView() {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task ViewForView_ImplicitDefaultConstructor_IsClean()
  {
    var page = """
  [ViewFor<ViewModel>]
  public class ImplicitView : Avalonia.Controls.Control {}
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task ViewForView_InternalParameterlessConstructor_IsClean()
  {
    var page = """
  [ViewFor<ViewModel>]
  public class InternalView : Avalonia.Controls.Control
  {
    internal InternalView() {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task PlainView_WithoutViewFor_IsNotReported()
  {
    var page = """
  public class PlainView : Avalonia.Controls.Control
  {
    public PlainView(string title) {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task AbstractViewForView_IsNotReported()
  {
    var page = """
  [ViewFor<ViewModel>]
  public abstract class AbstractView : Avalonia.Controls.Control
  {
    protected AbstractView(string title) {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }

  [Fact]
  public async Task GenericViewForView_ReportsNstr1004()
  {
    var page = """
  [ViewFor<ViewModel>]
  public class GenericView<T> : Avalonia.Controls.Control {}
""";

    var diagnostic = Assert.Single(await AnalyzeAsync(page));
    Assert.Equal("NSTR1004", diagnostic.Id);
    Assert.Contains("GenericView", diagnostic.GetMessage());
    Assert.Contains("it is generic", diagnostic.GetMessage());
  }

  [Fact]
  public async Task StaticViewForView_ReportsNstr1004()
  {
    var page = """
  [ViewFor<ViewModel>]
  public static class StaticView {}
""";

    var diagnostic = Assert.Single(await AnalyzeAsync(page));
    Assert.Equal("NSTR1004", diagnostic.Id);
    Assert.Contains("it is static", diagnostic.GetMessage());
  }

  [Fact]
  public async Task ViewNestedInGenericType_ReportsNstr1004()
  {
    var page = """
  public class Host<T>
  {
    [ViewFor<ViewModel>]
    public class InnerView : Avalonia.Controls.Control {}
  }
""";

    var diagnostic = Assert.Single(await AnalyzeAsync(page));
    Assert.Equal("NSTR1004", diagnostic.Id);
    Assert.Contains("nested in a generic type", diagnostic.GetMessage());
  }

  [Fact]
  public async Task PrivateNestedView_ReportsNstr1004()
  {
    var page = """
  public class Host
  {
    [ViewFor<ViewModel>]
    private class InnerView : Avalonia.Controls.Control {}
  }
""";

    var diagnostic = Assert.Single(await AnalyzeAsync(page));
    Assert.Equal("NSTR1004", diagnostic.Id);
    Assert.Contains("not accessible", diagnostic.GetMessage());
  }

  [Fact]
  public async Task NestedViewInNonGenericType_IsClean()
  {
    var page = """
  public class Host
  {
    [ViewFor<ViewModel>]
    public class InnerView : Avalonia.Controls.Control {}
  }
""";

    var diagnostics = await AnalyzeAsync(page);

    Assert.Empty(diagnostics);
  }
}
