using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

/// <summary>
///   Discovers the current assembly's view declarations.  A class or record carrying
///   <c>[ViewFor&lt;TViewModel&gt;]</c> is a candidate; nothing else is collected.
/// </summary>
internal static class CandidateFactory
{
  private static readonly HashSet<string> Markers =
  [
    "ViewFor", // one generic type argument
    "ViewForAttribute", // one generic type argument; the suffix may be written out
  ];

  internal static bool IsCandidateSyntax(SyntaxNode node)
  {
    // 1. Must be a class/record type, and not abstract
    if (node is not TypeDeclarationSyntax typeDecl
      || typeDecl is InterfaceDeclarationSyntax or StructDeclarationSyntax
      || typeDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.AbstractKeyword)))
      return false;

    // 2. cover most cases
    if (typeDecl.AttributeLists is { Count: > 0 } list)
    {
      var hasMarker = list.SelectMany(it => it.Attributes)
          .Any(it => Markers.Contains(GetAttributeName(it)));

      if (hasMarker)
      {
        return true;
      }
    }

    return false;
  }

  internal static ViewCandidate? Create(GeneratorSyntaxContext context, CancellationToken token)
  {
    if (context.Node is not TypeDeclarationSyntax typeDecl
        || context.SemanticModel.GetDeclaredSymbol(typeDecl, token) is not { TypeKind: TypeKind.Class } symbol
        || symbol.IsAbstract)
    {
      return null;
    }

    // The locator names the view it instantiates, so a view it cannot reach is not a candidate —
    // ViewInstantiationAnalyzer reports which one and why (NSTR1004).  An abstract view is dropped
    // silently: neither collected nor reported, the NSTR1003-era convention.
    if (ViewGeneratorSymbolHelper.CannotInstantiateReason(symbol) is not null)
    {
      return null;
    }

    var viewForVms = new List<TypeName>();
    var viewForKeys = new HashSet<string>(StringComparer.Ordinal);

    foreach (var attr in symbol.GetAttributes())
    {
      token.ThrowIfCancellationRequested();
      if (attr.AttributeClass is not { } attrClass)
      {
        continue;
      }

      if (GetAttributeMetadataName(attrClass) != Attributes.ViewForFull
          || attrClass.TypeArguments.FirstOrDefault() is not INamedTypeSymbol viewModelSymbol)
      {
        continue;
      }

      var viewModel = TypeName.FromNamedTypeSymbol(viewModelSymbol);
      if (viewForKeys.Add(viewModel.FullyQualified))
      {
        viewForVms.Add(viewModel);
      }
    }

    if (viewForVms.Count == 0)
    {
      return null;
    }

    return new ViewCandidate(
      TypeName.FromNamedTypeSymbol(symbol),
      viewForVms.ToEquatableArray());
  }

  /// <summary>The attribute name as written at the syntax level, without the framework's namespace.</summary>
  private static string GetAttributeName(AttributeSyntax attribute)
    => attribute.Name switch
    {
      IdentifierNameSyntax ins => ins.Identifier.Text, // [Some] or [SomeAttribute]
      GenericNameSyntax g => g.Identifier.Text, // [Some<T>] or [SomeAttribute<T>]
      QualifiedNameSyntax q when q.Right is GenericNameSyntax gr => gr.Identifier.Text, // [MyNs.Some<T>]
      QualifiedNameSyntax qns => qns.Right.Identifier.Text, // [MyNs.Some]
      _ => string.Empty
    };

  private static string GetAttributeMetadataName(INamedTypeSymbol attributeClass)
  {
    var original = attributeClass.OriginalDefinition;
    var ns = original.ContainingNamespace.ToDisplayString();
    return $"{ns}.{original.MetadataName}";
  }
}
