using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Generators.Extensions;


/// <summary>
/// Extension methods for the <see cref="SymbolInfo"/> type.
/// </summary>
internal static class SymbolInfoExtensions
{
  /// <summary>
  /// Tries to get the resolved attribute type symbol from a given <see cref="SymbolInfo"/> value.
  /// </summary>
  /// <param name="symbolInfo">The <see cref="SymbolInfo"/> value to check.</param>
  /// <param name="typeSymbol">The resulting attribute type symbol, if correctly resolved.</param>
  /// <returns>Whether <paramref name="symbolInfo"/> is resolved to a symbol.</returns>
  /// <remarks>
  /// An attribute on an invalid target is ignored by Roslyn without an error, so the generator
  /// validates the type symbol itself.
  /// </remarks>
  public static bool TryGetAttributeTypeSymbol(this SymbolInfo symbolInfo, [NotNullWhen(true)] out INamedTypeSymbol? typeSymbol)
  {
    ISymbol? attributeSymbol = symbolInfo.Symbol;

    // If no symbol is selected and there is a single candidate symbol, use that
    if (attributeSymbol is null
      && symbolInfo.CandidateSymbols is { Length: > 0 }
      && symbolInfo.CandidateSymbols[0] is ISymbol candidateSymbol)
    {
      attributeSymbol = candidateSymbol;
    }

    // Extract the symbol from either the current one or the containing type
    if ((attributeSymbol as INamedTypeSymbol ?? attributeSymbol?.ContainingType) is not INamedTypeSymbol resultingSymbol)
    {
      typeSymbol = null;

      return false;
    }

    typeSymbol = resultingSymbol;

    return true;
  }
}
