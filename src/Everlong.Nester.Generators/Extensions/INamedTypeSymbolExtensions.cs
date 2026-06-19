using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Extensions;

/// <summary>
/// Extension methods for the <see cref="INamedTypeSymbol"/> type.
/// </summary>
internal static class INamedTypeSymbolExtensions
{
  /// <summary>
  /// Checks whether a given <see cref="INamedTypeSymbol"/> is partial.
  /// </summary>
  /// <param name="typeSymbol">The input <see cref="INamedTypeSymbol"/> instance to check.</param>
  /// <returns>Whether <paramref name="typeSymbol"/> is partial.</returns>
  public static bool IsPartial(this INamedTypeSymbol typeSymbol)
  {
    foreach (SyntaxReference syntaxReference in typeSymbol.DeclaringSyntaxReferences)
    {
      if (syntaxReference.GetSyntax() is TypeDeclarationSyntax typeDeclaration &&
          typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Gets all member symbols from a given <see cref="INamedTypeSymbol"/> instance, including inherited ones.
  /// </summary>
  /// <param name="symbol">The input <see cref="INamedTypeSymbol"/> instance.</param>
  /// <returns>A sequence of all member symbols for <paramref name="symbol"/>.</returns>
  public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol)
  {
    for (INamedTypeSymbol? currentSymbol = symbol; currentSymbol is { SpecialType: not SpecialType.System_Object }; currentSymbol = currentSymbol.BaseType)
    {
      foreach (ISymbol memberSymbol in currentSymbol.GetMembers())
      {
        yield return memberSymbol;
      }
    }
  }

  /// <summary>
  /// Gets all member symbols from a given <see cref="INamedTypeSymbol"/> instance, including inherited ones, only if they are declared in source.
  /// </summary>
  /// <param name="symbol">The input <see cref="INamedTypeSymbol"/> instance.</param>
  /// <returns>A sequence of all member symbols for <paramref name="symbol"/>.</returns>
  public static IEnumerable<ISymbol> GetAllMembersFromSameAssembly(this INamedTypeSymbol symbol)
  {
    for (INamedTypeSymbol? currentSymbol = symbol; currentSymbol is { SpecialType: not SpecialType.System_Object }; currentSymbol = currentSymbol.BaseType)
    {
      // Stop early when we reach a base type from another assembly
      if (!SymbolEqualityComparer.Default.Equals(currentSymbol.ContainingAssembly, symbol.ContainingAssembly))
      {
        yield break;
      }

      foreach (ISymbol memberSymbol in currentSymbol.GetMembers())
      {
        yield return memberSymbol;
      }
    }
  }

  /// <summary>
  /// Gets all member symbols from a given <see cref="INamedTypeSymbol"/> instance, including inherited ones.
  /// </summary>
  /// <param name="symbol">The input <see cref="INamedTypeSymbol"/> instance.</param>
  /// <param name="name">The name of the members to look for.</param>
  /// <returns>A sequence of all member symbols for <paramref name="symbol"/>.</returns>
  public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol, string name)
  {
    for (INamedTypeSymbol? currentSymbol = symbol; currentSymbol is { SpecialType: not SpecialType.System_Object }; currentSymbol = currentSymbol.BaseType)
    {
      foreach (ISymbol memberSymbol in currentSymbol.GetMembers(name))
      {
        yield return memberSymbol;
      }
    }
  }
}
