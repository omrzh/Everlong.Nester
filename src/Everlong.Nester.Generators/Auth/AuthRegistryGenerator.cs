using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Extensions;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Everlong.Nester.Generators.Auth;

/// <summary>
///   Generates the assembly's authorization registry — a partial class
///   filling the <c>[AuthRegistry]</c> trigger with the assembly's
///   <c>[Authorize]</c> declarations, merged with every
///   <c>[Concat&lt;TRegistry&gt;]</c> registry's table.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class AuthRegistryGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // The trigger — the [AuthRegistry]-marked partial class the generated
    // implementation fills; a trigger that is not partial is left to
    // PartialKeywordAnalyzer (NSTR0001/NSTR0008), so no code is generated for it.
    // The snapshot carries the hierarchy, the concat registries and the location — never the symbol:
    // the input node then compares by value, so the output node caches.
    IncrementalValuesProvider<AuthTriggerInfo> triggers = context.SyntaxProvider
      .ForAttributeWithMetadataName<AuthTriggerInfo?>(
        Attributes.AuthRegistryFull,
        predicate: PredicateHelper.IsPartialClassDecl,
        transform: static (ctx, _) => SnapshotTrigger(ctx))
      .Where(static trigger => trigger is not null)
      .Select(static (trigger, _) => trigger!);

    // The declarations — every [Authorize]-carrying type of the current
    // assembly, with its requirement arguments.
    IncrementalValuesProvider<AuthEntryInfo> entries = context.SyntaxProvider
      .ForAttributeWithMetadataName<AuthEntryInfo?>(
        Attributes.AuthorizeFull,
        predicate: static (node, _) => node is TypeDeclarationSyntax,
        transform: static (ctx, _) => TransformEntry(ctx))
      .Where(static entry => entry is not null)
      .Select(static (entry, _) => entry!);

    IncrementalValueProvider<(EquatableArray<AuthTriggerInfo> Triggers, ImmutableArray<AuthEntryInfo> Entries)> input
      = triggers.Collect().Select(static (arr, _) => arr.ToEquatableArray()).Combine(entries.Collect());

    context.RegisterSourceOutput(input, static (spc, tuple) =>
      ExecuteHelper.Execute(spc, tuple, Execute));
  }

  /// <summary>Reads one [Authorize] declaration — the requirement arguments, or nulls for a bare declaration.</summary>
  private static AuthEntryInfo? TransformEntry(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol type)
      return null;

    string? roles = null;
    string? policy = null;
    foreach (AttributeData attribute in ctx.Attributes)
    {
      foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
      {
        switch (named)
        {
          case { Key: "Roles", Value.Value: string rolesValue }:
            roles = rolesValue;
            break;
          case { Key: "Policy", Value.Value: string policyValue }:
            policy = policyValue;
            break;
        }
      }
    }

    return new AuthEntryInfo(
      type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      roles,
      policy);
  }

  /// <summary>Snapshots one [AuthRegistry] trigger — the hierarchy its half restates, its concat registries and its location.</summary>
  private static AuthTriggerInfo? SnapshotTrigger(GeneratorAttributeSyntaxContext ctx)
  {
    if (ctx.TargetSymbol is not INamedTypeSymbol symbol)
      return null;

    using ImmutableArrayBuilder<string> concatRegistries = ImmutableArrayBuilder<string>.Rent();

    foreach (AttributeData attribute in symbol.GetAttributes())
    {
      if (attribute.AttributeClass is not { } attributeClass
          || attributeClass.GetFullyQualifiedMetadataName() != Attributes.ConcatFull
          || attributeClass.TypeArguments.FirstOrDefault() is not INamedTypeSymbol registryType)
      {
        continue;
      }

      concatRegistries.Add(registryType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    return new AuthTriggerInfo(
      TypeHierarchy.From(symbol),
      concatRegistries.ToImmutable(),
      LocationInfo.CreateFrom(symbol.Locations.FirstOrDefault()));
  }

  private static void Execute(
    SourceProductionContext context,
    (EquatableArray<AuthTriggerInfo> Triggers, ImmutableArray<AuthEntryInfo> Entries) input)
  {
    if (input.Triggers.IsEmpty)
      return;

    if (input.Triggers.Length > 1)
    {
      foreach (AuthTriggerInfo rejected in input.Triggers)
      {
        context.ReportDiagnostic(
          DiagnosticInfo.Create(Descriptors.MultipleAuthRegistries, rejected.Location).ToDiagnostic());
      }
      return;
    }

    AuthTriggerInfo trigger = input.Triggers[0];
    AuthEntryInfo[] sortedEntries = input.Entries
      .OrderBy(static e => e.TypeDisplay, StringComparer.Ordinal)
      .ToArray();

    context.AddSource(
      $"{trigger.Hierarchy.FilenameHint}.AuthRegistry{Conventions.GeneratorSuffix}",
      BuildRegistrySource(trigger.Hierarchy, sortedEntries, trigger.ConcatRegistries));
  }

  /// <summary>Builds the registry partial — the descriptor map, the concat parts and the lookup.</summary>
  private static CompilationUnitSyntax BuildRegistrySource(
    TypeHierarchy hierarchy,
    IReadOnlyList<AuthEntryInfo> entries,
    EquatableArray<string> concatRegistries)
  {
    var members = new List<MemberDeclarationSyntax>
    {
      DescriptorMap(entries)
    };

    if (!concatRegistries.IsEmpty)
      members.Add(ConcatParts(concatRegistries));

    members.Add(DescriptorLookup(!concatRegistries.IsEmpty));

    return hierarchy.GetPartialHalf(
      usings: ImmutableArray<string>.Empty,
      members: members.ToImmutableArray(),
      baseTypes: ImmutableArray.Create<BaseTypeSyntax>(SimpleBaseType(ParseTypeName(AuthRegistryType))));
  }

  private static readonly SyntaxTokenList FieldModifiers =
    TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));

  private const string AuthDescriptorType = "global::Everlong.Nester.Auth.AuthDescriptor";

  private const string AuthDescriptorNullableType = AuthDescriptorType + "?";

  private const string AuthRegistryType = "global::Everlong.Nester.Auth.IAuthRegistry";

  private const string SystemTypeName = "global::System.Type";

  private const string DescriptorMapType =
    "global::System.Collections.Generic.Dictionary<" +
    "global::System.Type, global::Everlong.Nester.Auth.AuthDescriptor>";

  /// <summary>The descriptor map — one entry per [Authorize] declaration, keyed by its type.</summary>
  private static FieldDeclarationSyntax DescriptorMap(IReadOnlyList<AuthEntryInfo> entries)
  {
    ExpressionSyntax Entry(AuthEntryInfo entry) =>
      AssignmentExpression(
        SyntaxKind.SimpleAssignmentExpression,
        ImplicitElementAccess(BracketedArgumentList(SingletonSeparatedList(
          Argument(TypeOfExpression(ParseTypeName(entry.TypeDisplay)))))),
        ObjectCreationExpression(ParseTypeName(AuthDescriptorType))
          .WithArgumentList(ArgumentList(SeparatedList(RequirementArguments(entry)))));

    ExpressionSyntax[] initializers = entries.Select(Entry).ToArray();

    return FieldDeclaration(
        VariableDeclaration(ParseTypeName(DescriptorMapType))
          .WithVariables(SingletonSeparatedList(
            VariableDeclarator(Identifier("_map"))
              .WithInitializer(EqualsValueClause(
                ObjectCreationExpression(ParseTypeName(DescriptorMapType))
                  .WithInitializer(InitializerExpression(
                    SyntaxKind.CollectionInitializerExpression,
                    SeparatedList(initializers, TrailingSeparators(initializers.Length)))))))))
      .WithAttributeLists(SyntaxHelpers.GeneratedFieldAttributes())
      .WithModifiers(FieldModifiers);
  }

  /// <summary>The concat parts — instantiated once and consulted after the own map.</summary>
  private static FieldDeclarationSyntax ConcatParts(EquatableArray<string> concatRegistries)
  {
    ExpressionSyntax[] parts = concatRegistries
      .Select(static registry =>
        (ExpressionSyntax)ObjectCreationExpression(ParseTypeName(registry)).WithArgumentList(ArgumentList()))
      .ToArray();

    return FieldDeclaration(
        VariableDeclaration(ArrayType(ParseTypeName(AuthRegistryType), SingletonList(ArrayRankSpecifier())))
          .WithVariables(SingletonSeparatedList(
            VariableDeclarator(Identifier("_parts"))
              .WithInitializer(EqualsValueClause(
                InitializerExpression(
                  SyntaxKind.ArrayInitializerExpression,
                  SeparatedList(parts, TrailingSeparators(parts.Length))))))))
      .WithAttributeLists(SyntaxHelpers.GeneratedFieldAttributes())
      .WithModifiers(FieldModifiers);
  }

  /// <summary>One comma per element — the emitted list keeps its trailing separator.</summary>
  private static IEnumerable<SyntaxToken> TrailingSeparators(int count)
    => Enumerable.Repeat(Token(SyntaxKind.CommaToken), count);

  /// <summary>The lookup — the own map first, then every concat part in declaration order.</summary>
  private static MethodDeclarationSyntax DescriptorLookup(bool hasParts)
  {
    var body = new List<StatementSyntax>
    {
      IfStatement(
        InvocationExpression(
            MemberAccessExpression(
              SyntaxKind.SimpleMemberAccessExpression,
              IdentifierName("_map"),
              IdentifierName("TryGetValue")))
          .WithArgumentList(ArgumentList(SeparatedList<ArgumentSyntax>(new[]
          {
            Argument(IdentifierName("type")),
            Argument(DeclarationExpression(
                       ParseTypeName(AuthDescriptorType),
                       SingleVariableDesignation(Identifier("descriptor"))))
              .WithRefKindKeyword(Token(SyntaxKind.OutKeyword))
          }))),
        ReturnStatement(IdentifierName("descriptor")))
    };

    if (hasParts)
    {
      body.Add(ForEachStatement(
        ParseTypeName(AuthRegistryType),
        Identifier("part"),
        IdentifierName("_parts"),
        Block(
          LocalDeclarationStatement(
            VariableDeclaration(ParseTypeName(AuthDescriptorNullableType))
              .WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier("found"))
                  .WithInitializer(EqualsValueClause(
                    InvocationExpression(
                        MemberAccessExpression(
                          SyntaxKind.SimpleMemberAccessExpression,
                          IdentifierName("part"),
                          IdentifierName("GetDescriptor")))
                      .WithArgumentList(ArgumentList(SingletonSeparatedList(
                        Argument(IdentifierName("type")))))))))),
          IfStatement(
            IsPatternExpression(
              IdentifierName("found"),
              UnaryPattern(
                Token(SyntaxKind.NotKeyword),
                ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression)))),
            ReturnStatement(IdentifierName("found"))))));
    }

    body.Add(ReturnStatement(LiteralExpression(SyntaxKind.NullLiteralExpression)));

    return MethodDeclaration(ParseTypeName(AuthDescriptorNullableType), "GetDescriptor")
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList(SingletonSeparatedList(
        Parameter(Identifier("type")).WithType(ParseTypeName(SystemTypeName)))))
      .WithBody(Block(body))
      .WithLeadingTrivia(ParseLeadingTrivia("/// <inheritdoc />\n"));
  }

  /// <summary>The requirement's constructor arguments — named, so a bare declaration emits an empty list.</summary>
  private static IEnumerable<ArgumentSyntax> RequirementArguments(AuthEntryInfo entry)
  {
    if (entry.Roles is not null)
      yield return Argument(NameColon(IdentifierName("Roles")), Token(SyntaxKind.None), SyntaxHelpers.StringLiteral(entry.Roles));

    if (entry.Policy is not null)
      yield return Argument(NameColon(IdentifierName("Policy")), Token(SyntaxKind.None), SyntaxHelpers.StringLiteral(entry.Policy));
  }
}

/// <summary>One [AuthRegistry] trigger, snapshotted: the hierarchy its half restates, its concat registries and its location.</summary>
internal sealed record AuthTriggerInfo(
  TypeHierarchy Hierarchy,
  EquatableArray<string> ConcatRegistries,
  LocationInfo? Location);

/// <summary>One [Authorize] declaration — the carrying type and its requirement arguments.</summary>
internal sealed record AuthEntryInfo(string TypeDisplay, string? Roles, string? Policy);
