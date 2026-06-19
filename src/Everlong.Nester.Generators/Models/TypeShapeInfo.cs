using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Everlong.Nester.Generators.Models;

/// <summary>
/// The declaration shape a generated half restates for a declared type: name, kind, record and
/// static-ness, and type parameter names. Accessibility is not part of the shape.
/// </summary>
internal sealed record TypeShapeInfo(
  string Name,
  TypeKind Kind,
  bool IsRecord,
  bool IsStatic,
  EquatableArray<string> TypeParameters)
{
  /// <summary>
  ///   The declaration the generated half restates: the kind keyword, <c>partial</c> and the type
  ///   parameter names.  Accessibility is never restated — the user's half owns it.
  /// </summary>
  public TypeDeclarationSyntax GetDeclaration()
  {
    TypeDeclarationSyntax declaration = this switch
    {
      // A record's braces are spelled out: the members of a record declaration without them are not
      // formatted where they belong.
      { IsRecord: true, Kind: TypeKind.Struct } => RecordDeclaration(Token(SyntaxKind.RecordKeyword), Name)
        .WithClassOrStructKeyword(Token(SyntaxKind.StructKeyword))
        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
      { IsRecord: true } => RecordDeclaration(Token(SyntaxKind.RecordKeyword), Name)
        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken))
        .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)),
      { Kind: TypeKind.Struct } => StructDeclaration(Name),
      { Kind: TypeKind.Interface } => InterfaceDeclaration(Name),
      _ => ClassDeclaration(Name)
    };

    var modifiers = new List<SyntaxToken>();
    if (IsStatic)
      modifiers.Add(Token(SyntaxKind.StaticKeyword));
    modifiers.Add(Token(SyntaxKind.PartialKeyword));

    declaration = declaration.WithModifiers(TokenList(modifiers));

    // The user's half documents the type; the generated half points at it instead of repeating it.
    declaration = declaration.WithLeadingTrivia(ParseLeadingTrivia("/// <inheritdoc/>\n"));

    if (TypeParameters.Length > 0)
    {
      // Omitting the type parameters does not fail the build: `Outer<T>` and `Outer` are two different
      // types, so the generated half would silently declare the second one and put the members there.
      declaration = declaration.WithTypeParameterList(TypeParameterList(SeparatedList(
        TypeParameters.Select(static name => TypeParameter(name)))));
    }

    return declaration;
  }
}
