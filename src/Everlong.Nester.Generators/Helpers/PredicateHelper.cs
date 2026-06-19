using Everlong.Nester.Generators.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Generators.Helpers;

public static class PredicateHelper
{

  /// <summary>
  ///  Predicate to check if a syntax node is a partial class declaration (excluding interfaces, including records).
  /// </summary>
  public static bool IsPartialClassDecl(SyntaxNode node, CancellationToken token = default)
  {
    return node is TypeDeclarationSyntax typeDecl and not InterfaceDeclarationSyntax
           && typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
           && !token.IsCancellationRequested;
  }

  public static bool IsPartialRecursively(INamedTypeSymbol? symbol)
  {
    if (symbol == null)
      return false;

    // Check the symbol itself
    if (!symbol.IsPartial())
      return false;

    // Check containing types
    var current = symbol.ContainingType;
    while (current != null)
    {
      if (!current.IsPartial())
        return false;
      current = current.ContainingType;
    }

    return true;
  }

  public static bool IsClass(SyntaxNode node, CancellationToken token = default)
    => node is ClassDeclarationSyntax;

  public static bool IsRecord(SyntaxNode node, CancellationToken token = default)
    => node is RecordDeclarationSyntax;

}
