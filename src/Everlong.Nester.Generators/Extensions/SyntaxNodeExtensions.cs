using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Extensions;


/// <summary>
/// Extension methods for the <see cref="SyntaxNode"/> type.
/// </summary>
internal static class SyntaxNodeExtensions
{
  /// <summary>
  /// Checks whether a given <see cref="SyntaxNode"/> represents the first (partial) declaration of a given symbol.
  /// </summary>
  /// <param name="syntaxNode">The input <see cref="SyntaxNode"/> instance.</param>
  /// <param name="symbol">The target <see cref="ISymbol"/> instance to check the syntax declaration for.</param>
  /// <returns>Whether <paramref name="syntaxNode"/> is the first (partial) declaration for <paramref name="symbol"/>.</returns>
  /// <remarks>
  /// Each syntax node is compared against the symbol's declaring syntax references: a node that
  /// matches the first reference is kept, any other is a duplicate.
  /// </remarks>
  public static bool IsFirstSyntaxDeclarationForSymbol(this SyntaxNode syntaxNode, ISymbol symbol)
  {
    var declaringSyntaxReferences = symbol.DeclaringSyntaxReferences;

    if (declaringSyntaxReferences.Length == 0)
    {
      return false;
    }

    var syntaxReference = declaringSyntaxReferences[0];

    return syntaxReference.SyntaxTree == syntaxNode.SyntaxTree
        && syntaxReference.Span == syntaxNode.Span;
  }

  /// <summary>
  /// Checks whether a given <see cref="SyntaxNode"/> is a given type declaration with or potentially with any base types, using only syntax.
  /// </summary>
  /// <typeparam name="T">The type of declaration to check for.</typeparam>
  /// <param name="node">The input <see cref="SyntaxNode"/> to check.</param>
  /// <returns>Whether <paramref name="node"/> is a given type declaration with or potentially with any base types.</returns>
  public static bool IsTypeDeclarationWithOrPotentiallyWithBaseTypes<T>(this SyntaxNode node)
      where T : TypeDeclarationSyntax
  {
    // Immediately bail if the node is not a type declaration of the specified type
    if (node is not T typeDeclaration)
    {
      return false;
    }

    // If the base types list is not empty, the type can definitely has implemented interfaces
    if (typeDeclaration.BaseList is { Types.Count: > 0 })
    {
      return true;
    }

    // If the base types list is empty, check if the type is partial. If it is, it means
    // that there could be another partial declaration with a non-empty base types list.
    return typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
  }
}
