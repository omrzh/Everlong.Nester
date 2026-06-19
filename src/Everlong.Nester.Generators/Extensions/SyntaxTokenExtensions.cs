using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Extensions;


/// <summary>
/// Extension methods for the <see cref="SyntaxToken"/> type.
/// </summary>
internal static class SyntaxTokenExtensions
{
  /// <summary>
  /// Deconstructs a <see cref="SyntaxToken"/> into its <see cref="SyntaxKind"/> value.
  /// </summary>
  /// <param name="syntaxToken">The input <see cref="SyntaxToken"/> value.</param>
  /// <param name="syntaxKind">The resulting <see cref="SyntaxKind"/> value for <paramref name="syntaxToken"/>.</param>
  public static void Deconstruct(this SyntaxToken syntaxToken, out SyntaxKind syntaxKind)
  {
    syntaxKind = syntaxToken.Kind();
  }
}
