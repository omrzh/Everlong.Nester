using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
namespace Everlong.Nester.Generators.Views.Executors;

/// <summary>
///   The Avalonia view provider — the generated half of the <c>[ViewLocator]</c> trigger
///   implements <c>IViewLocator</c>: it claims exactly the view models it can present, and
///   builds their declared views.
/// </summary>
internal sealed class AvaloniaViewProviderBranch : IExecutionBranch
{
  public BranchResult Execute(EquatableArray<PairedInfo> pairs, RegistrationModel registration)
  {
    var members = new List<MemberDeclarationSyntax>
    {
      ((FieldDeclarationSyntax)ParseMemberDeclaration("private readonly FrozenSet<Type> _supportedTypes;")!)
        .WithAttributeLists(SyntaxHelpers.GeneratedFieldAttributes())
    };

    members.Add(GenerateConstructor(pairs, registration));
    members.Add(GenerateMatchMethod());
    members.AddRange(GenerateBuildMethod(pairs));

    return new BranchResult(
      ImmutableArray.CreateRange(members),
      ImmutableArray.Create<BaseTypeSyntax>(SimpleBaseType(ParseTypeName("IViewLocator"))),
      ImmutableArray.Create(Ns.AvaloniaControls, Ns.NesterPresentation,
                            "System", "System.Collections.Frozen", "System.Linq"));
  }

  private static ConstructorDeclarationSyntax GenerateConstructor(
    EquatableArray<PairedInfo> pairs,
    RegistrationModel registration)
  {
    StatementSyntax supportedStmt;
    if (pairs.IsEmpty)
    {
      supportedStmt = ParseStatement("_supportedTypes = Array.Empty<Type>().ToFrozenSet();");
    }
    else
    {
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

      supportedStmt = ExpressionStatement(
        AssignmentExpression(
          SyntaxKind.SimpleAssignmentExpression,
          IdentifierName("_supportedTypes"),
          frozenSet));
    }

    return ConstructorDeclaration(registration.Hierarchy.Self.Name)
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList())
      .WithBody(Block(supportedStmt));
  }

  private static MethodDeclarationSyntax GenerateMatchMethod()
  {
    return MethodDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)), "Match")
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList(SingletonSeparatedList(
                                         Parameter(Identifier("data")).WithType(ParseTypeName("object?")))))
      .WithBody(Block(
                  ParseStatement("if (data is null) return false;"),
                  ParseStatement("return _supportedTypes.Contains(data.GetType());")
                ));
  }

  private static ImmutableArray<MemberDeclarationSyntax> GenerateBuildMethod(EquatableArray<PairedInfo> pairs)
  {
    // IDataTemplate.Build(object? data) — one view per call: the provider is a builder, not a recycling
    // template, so nothing Avalonia built earlier is ever handed back.
    var body = new List<StatementSyntax>
    {
      ParseStatement("if (data is null) return null;"),
    };

    if (!pairs.IsEmpty)
    {
      body.Add(ParseStatement("var type = data.GetType();"));
      foreach (var pair in pairs)
      {
        body.Add(SyntaxHelpers.TypeDispatch("type", pair.ViewModel.FullyQualified, pair.View.FullyQualified));
      }
    }

    body.Add(ParseStatement("return null;"));

    var build = MethodDeclaration(ParseTypeName("Control?"), "Build")
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList(SingletonSeparatedList(
                                         Parameter(Identifier("data")).WithType(ParseTypeName("object?")))))
      .WithBody(Block(body));

    return ImmutableArray.Create<MemberDeclarationSyntax>(build);
  }
}
