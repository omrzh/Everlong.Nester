using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Everlong.Nester.Generators.Views.Executors;

/// <summary>
///   The WPF view provider — the generated half of the <c>[WpfViewLocator]</c> trigger derives
///   from <c>ViewLocatorBase</c> and registers the resolved bindings as templates: it claims
///   exactly the view models it can present, and builds their declared views.
/// </summary>
internal sealed class WpfViewProviderBranch : IExecutionBranch
{
  public BranchResult Execute(EquatableArray<PairedInfo> pairs, RegistrationModel registration)
  {
    var members = new List<MemberDeclarationSyntax>
    {
      ((FieldDeclarationSyntax)ParseMemberDeclaration("private readonly FrozenSet<Type> _supportedTypes;")!)
        .WithAttributeLists(SyntaxHelpers.GeneratedFieldAttributes())
    };

    members.Add(GenerateConstructor(pairs, registration.Hierarchy.Self.Name));
    members.Add(GenerateMatchMethod());
    members.Add(GenerateBuildMethod(pairs));

    return new BranchResult(
      ImmutableArray.CreateRange(members),
      ImmutableArray.Create<BaseTypeSyntax>(SimpleBaseType(ParseTypeName("ViewLocatorBase"))),
      ImmutableArray.Create(Ns.NesterPresentation, "System.Windows", "System.Windows.Controls",
                            "System", "System.Collections.Frozen", "System.Linq"));
  }

  private static ConstructorDeclarationSyntax GenerateConstructor(
    EquatableArray<PairedInfo> pairs, string className)
  {
    var statements = new List<StatementSyntax>();

    foreach (var pair in pairs)
    {
      statements.Add(ExpressionStatement(
        InvocationExpression(
            GenericName(Identifier("AddTemplate"))
              .WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(new[]
              {
                ParseTypeName(pair.ViewModel.FullyQualified),
                ParseTypeName(pair.View.FullyQualified)
              }))))
          .WithArgumentList(ArgumentList())));
    }

    statements.Add(SupportedTypesAssignment(pairs));

    return ConstructorDeclaration(className)
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList())
      .WithBody(Block(statements));
  }

  private static StatementSyntax SupportedTypesAssignment(EquatableArray<PairedInfo> pairs)
  {
    if (pairs.IsEmpty)
      return ParseStatement("_supportedTypes = Array.Empty<Type>().ToFrozenSet();");

    var initializer = InitializerExpression(
      SyntaxKind.ArrayInitializerExpression,
      SeparatedList<ExpressionSyntax>(
        pairs.Select(static p => TypeOfExpression(ParseTypeName(p.ViewModel.FullyQualified)))));

    var frozenSet = InvocationExpression(
      MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        ArrayCreationExpression(
          ArrayType(ParseTypeName("Type"), SingletonList(ArrayRankSpecifier())),
          initializer),
        IdentifierName("ToFrozenSet")))
      .WithArgumentList(ArgumentList());

    return ExpressionStatement(
      AssignmentExpression(
        SyntaxKind.SimpleAssignmentExpression,
        IdentifierName("_supportedTypes"),
        frozenSet));
  }

  private static MethodDeclarationSyntax GenerateMatchMethod()
  {
    return MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), "Match")
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
      .WithParameterList(ParameterList(SingletonSeparatedList(
                                         Parameter(Identifier("data")).WithType(ParseTypeName("object?")))))
      .WithBody(Block(
                  ParseStatement("if (data is null) return false;"),
                  ParseStatement("return _supportedTypes.Contains(data.GetType());")
                ));
  }

  private static MethodDeclarationSyntax GenerateBuildMethod(EquatableArray<PairedInfo> pairs)
  {
    var statements = new List<StatementSyntax>
    {
      ParseStatement("if (data is null) return null;"),
    };

    if (!pairs.IsEmpty)
    {
      statements.Add(ParseStatement("var type = data.GetType();"));
      foreach (var pair in pairs)
      {
        statements.Add(SyntaxHelpers.TypeDispatch("type", pair.ViewModel.FullyQualified, pair.View.FullyQualified));
      }
    }

    statements.Add(ParseStatement("return null;"));

    return MethodDeclaration(ParseTypeName("FrameworkElement?"), "Build")
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword)))
      .WithParameterList(ParameterList(SingletonSeparatedList(
        Parameter(Identifier("data")).WithType(ParseTypeName("object?")))))
      .WithBody(Block(statements));
  }
}
