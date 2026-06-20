using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using Everlong.Nester.Generators.Views;

namespace Everlong.Nester.Tests.Views;

/// <summary>
///   The candidate predicate: only <c>[ViewFor&lt;TViewModel&gt;]</c> marks a view candidate.
///   Nothing else — no <c>[Routable]</c>, no <c>[Layout&lt;T&gt;]</c>, no dialog base class —
///   is collected or inferred.
/// </summary>
public class CurrentAssemblyCandidateFactoryTests
{
  [Theory]
  [InlineData("[ViewFor<MainViewModel>]", "public class MainViewModel {}")]
  [InlineData("[ViewForAttribute<MainViewModel>]", "public class MainViewModel {}")]
  public void ReturnsTrue_WhenTypeHasViewForAttribute(string attributeUsage, string supportTypes)
  {
    var source = $$"""
using System;
{{supportTypes}}

namespace TestApp;

{{attributeUsage}}
public class Candidate {}
""";

    var node = GetFirstClassDeclaration(source);

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.True(result);
  }

  [Theory]
  [InlineData("[MyNs.ViewFor<MainViewModel>]")]
  [InlineData("[MyNs.ViewForAttribute<MainViewModel>]")]
  public void ReturnsTrue_WhenViewForAttributeIsQualified(string attributeUsage)
  {
    var source = $$"""
using System;

namespace MyNs
{
    public class ViewForAttribute<T> : System.Attribute {}
}

namespace TestApp;

{{attributeUsage}}
public class Candidate {}
""";

    var node = GetFirstClassDeclaration(source);

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.True(result);
  }

  [Theory]
  [InlineData("[Layout<MainViewModel>]", "public class MainViewModel {}")]
  [InlineData("[LayoutAttribute<MainViewModel>]", "public class MainViewModel {}")]
  [InlineData("[Routable<MainViewModel>]", "public class MainViewModel {}")]
  [InlineData("[RoutableAttribute<MainViewModel>]", "public class MainViewModel {}")]
  public void ReturnsFalse_WhenTypeOnlyHasARoutingAttribute(string attributeUsage, string supportTypes)
  {
    var source = $$"""
using System;
{{supportTypes}}

namespace TestApp;

{{attributeUsage}}
public class Candidate {}
""";

    var node = GetFirstClassDeclaration(source);

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Theory]
  [InlineData("MyDialogSessionBase")]
  [InlineData("DialogSessionBase<bool>")]
  [InlineData("global::Everlong.Nester.Dialog.DialogSessionBase")]
  public void ReturnsFalse_WhenTypeOnlyHasACandidateBaseType(string baseTypeName)
  {
    var source = $$"""
namespace TestApp;

public class Candidate : {{baseTypeName}} {}
""";

    var node = GetFirstClassDeclaration(source, "Candidate");

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Theory]
  [InlineData("HomeViewModel")]
  [InlineData("MainViewModel")]
  public void ReturnsFalse_WhenTypeNameOnlyLooksLikeViewModel(string typeName)
  {
    var source = $$"""
namespace TestApp;

public class {{typeName}} {}
""";

    var node = GetFirstClassDeclaration(source, typeName);

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Fact]
  public void ReturnsFalse_WhenTypeOnlyHasUserControlBase()
  {
    var source = """
namespace TestApp;

public class Candidate : UserControl {}
""";

    var node = GetFirstClassDeclaration(source, "Candidate");

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Fact]
  public void ReturnsFalse_WhenTypeHasNoViewForAttribute()
  {
    var source = """
namespace TestApp;

[Obsolete]
public class Candidate {}
""";

    var node = GetFirstClassDeclaration(source, "Candidate");

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Fact]
  public void ReturnsFalse_WhenNodeIsNotTypeDeclaration()
  {
    var syntaxTree = CSharpSyntaxTree.ParseText("namespace TestApp;", cancellationToken: TestContext.Current.CancellationToken);
    var node = syntaxTree.GetRoot(TestContext.Current.CancellationToken);

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Fact]
  public void ReturnsFalse_WhenClassIsAbstract()
  {
    var source = """
namespace TestApp;

[ViewFor<Candidate>]
public abstract class Candidate {}
""";

    var node = GetFirstClassDeclaration(source, "Candidate");

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.False(result);
  }

  [Fact]
  public void ReturnsTrue_WhenNodeIsRecordDeclaration()
  {
    var source = """
namespace TestApp;

[ViewFor<Candidate>]
public record Candidate;
""";

    var syntaxTree = CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken);
    var root = syntaxTree.GetRoot(TestContext.Current.CancellationToken);
    var node = root.DescendantNodes().OfType<RecordDeclarationSyntax>().First();

    var result = CandidateFactory.IsCandidateSyntax(node);

    Assert.True(result);
  }

  private static ClassDeclarationSyntax GetFirstClassDeclaration(string source, string typeName = "Candidate")
  {
    var root = CSharpSyntaxTree.ParseText(source).GetRoot();
    return root.DescendantNodes()
      .OfType<ClassDeclarationSyntax>()
      .First(type => type.Identifier.Text == typeName);
  }
}
