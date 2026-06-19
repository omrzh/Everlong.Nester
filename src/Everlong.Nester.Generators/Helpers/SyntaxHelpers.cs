using Everlong.Nester.Generators.Constants;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Everlong.Nester.Generators.Helpers;

public static class SyntaxHelpers
{
  // Parse simple statements
  public static StatementSyntax Statement(string code) => ParseStatement(code);

  /// <summary>
  ///   A <c>new T()</c> expression — the argument list is spelled out, so the printed form is valid
  ///   without a post-processing pass.
  /// </summary>
  internal static ObjectCreationExpressionSyntax NewObject(string type)
    => ObjectCreationExpression(ParseTypeName(type)).WithArgumentList(ArgumentList());

  /// <summary>
  ///   An <c>if (subject == typeof(TModel)) return new TView();</c> dispatch branch.  Every name
  ///   enters as a syntax node — never as text interpolated into a parsed snippet.
  /// </summary>
  internal static StatementSyntax TypeDispatch(string subject, string model, string view)
    => IfStatement(
         BinaryExpression(
           SyntaxKind.EqualsExpression,
           IdentifierName(subject),
           TypeOfExpression(ParseTypeName(model))),
         ReturnStatement(NewObject(view)));

  /// <summary>A string literal — a name or a user value arrives as a value, never as quoted text.</summary>
  internal static LiteralExpressionSyntax StringLiteral(string value)
    => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

  /// <summary>
  ///   The attribute lists a generated member carries: the generator's identity and version, and the
  ///   coverage exclusion.
  /// </summary>
  internal static SyntaxList<AttributeListSyntax> GeneratedMemberAttributes()
    => List(new AttributeListSyntax[]
       {
         GeneratedCodeAttribute(),
         AttributeList(SingletonSeparatedList(
           Attribute(ParseName("global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage"))))
       });

  /// <summary>
  ///   The attribute lists a generated field carries.  <c>ExcludeFromCodeCoverage</c> is not valid on a
  ///   field, and a field holds nothing to cover anyway, so a generated field says only that it is
  ///   generated.
  /// </summary>
  internal static SyntaxList<AttributeListSyntax> GeneratedFieldAttributes()
    => List(new AttributeListSyntax[] { GeneratedCodeAttribute() });

  /// <summary>The <c>[GeneratedCode]</c> attribute naming this generator and its version.</summary>
  private static AttributeListSyntax GeneratedCodeAttribute()
    => AttributeList(SingletonSeparatedList(
         Attribute(ParseName("global::System.CodeDom.Compiler.GeneratedCode"))
           .WithArgumentList(AttributeArgumentList(SeparatedList(new[]
           {
             AttributeArgument(StringLiteral(Conventions.GeneratorToolName)),
             AttributeArgument(StringLiteral(Conventions.GeneratorVersion))
           })))));

  // Parse with placeholder replacement (safe interpolation)
  public static StatementSyntax Statement(string template, params (string placeholder, string value)[] replacements)
  {
    var syntax = ParseStatement(template);
    foreach (var (placeholder, value) in replacements)
    {
      while (true)
      {
        var token = syntax.DescendantTokens()
          .FirstOrDefault(t => t.Text == placeholder);

        if (token == default)
          break;

        syntax = syntax.ReplaceToken(token, SyntaxFactory.Identifier(value));
      }
    }

    return syntax;
  }

  // Parse member declarations (fields, properties)
  public static MemberDeclarationSyntax Member(string code) => ParseMemberDeclaration(code)!;

  // Parse member with placeholder replacement
  public static MemberDeclarationSyntax Member(string template, params (string placeholder, string value)[] replacements)
  {
    var syntax = SyntaxFactory.ParseMemberDeclaration(template)!;
    foreach (var (placeholder, value) in replacements)
    {
      while (true)
      {
        var token = syntax.DescendantTokens()
          .FirstOrDefault(t => t.Text == placeholder);

        if (token == default)
          break;

        syntax = syntax.ReplaceToken(token, SyntaxFactory.Identifier(value));
      }
    }
    return syntax;
  }

  // Parse multiple statements
  public static IEnumerable<StatementSyntax> Statements(string code) =>
    ParseCompilationUnit(code)
      .DescendantNodes()
      .OfType<StatementSyntax>();
}
