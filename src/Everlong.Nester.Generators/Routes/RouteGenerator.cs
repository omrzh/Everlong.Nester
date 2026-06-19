using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Extensions;
using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Everlong.Nester.Generators.Routes;

/// <summary>
///   Generates the strongly-typed locator class for ViewModels marked
///   <c>[Routable]</c> / <c>[Routable&lt;TArgs&gt;]</c> — a locator whose
///   chain is built from the <c>[Layout&lt;T&gt;]</c> declarations and whose
///   constructor projects the chain's argument records in order.
/// </summary>
[Generator]
public sealed class RouteGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Types referenced by any [Layout<T>] declaration — the layout role:
    // they contribute chain parameters but never generate a route class.
    // Snapshotted as names, so the node compares by value.
    IncrementalValueProvider<EquatableArray<TypeName>> layoutRefs = context.SyntaxProvider
      .ForAttributeWithMetadataName(
        Attributes.Layout1Full,
        predicate: static (node, _) => node is TypeDeclarationSyntax,
        transform: static (ctx, _) => TypeName.FromNamedTypeSymbol((INamedTypeSymbol)ctx.Attributes[0].AttributeClass!.TypeArguments[0]))
      .Collect()
      .Select(static (arr, _) => arr.ToEquatableArray());

    // The declarations — each [Routable] / [Routable<TArgs>] type snapshotted in the transform:
    // nothing downstream of the transform holds a symbol, so every node compares by value.
    IncrementalValuesProvider<Result<RouteCandidateSnapshot?>> nonGeneric = context.SyntaxProvider
      .ForAttributeWithMetadataName<Result<RouteCandidateSnapshot?>>(
        Attributes.RoutableFull,
        predicate: PredicateHelper.IsPartialClassDecl,
        transform: static (ctx, _) => Snapshot(ctx));

    IncrementalValuesProvider<Result<RouteCandidateSnapshot?>> generic = context.SyntaxProvider
      .ForAttributeWithMetadataName<Result<RouteCandidateSnapshot?>>(
        Attributes.RoutableGenericFull,
        predicate: PredicateHelper.IsPartialClassDecl,
        transform: static (ctx, _) => Snapshot(ctx));

    IncrementalValueProvider<(EquatableArray<Result<RouteCandidateSnapshot?>> Candidates, EquatableArray<TypeName> LayoutRefs)> input
      = nonGeneric.Collect()
        .Combine(generic.Collect())
        .Select(static (pair, _) => pair.Left.AddRange(pair.Right).ToEquatableArray())
        .Combine(layoutRefs);

    IncrementalValuesProvider<Result<RouteInfo?>> targets = input
      .SelectMany(static (pair, _) => pair.Candidates.Select(candidate => Plan(candidate, pair.LayoutRefs)));

    context.ReportDiagnostics(targets.SelectMany(static (item, _) => item.Errors));

    context.RegisterSourceOutput(
      targets.Where(static item => item.Value is not null)
             .Select(static (item, _) => item.Value!),
      WrappedExecute);
  }

  private static Result<RouteCandidateSnapshot?> Snapshot(GeneratorAttributeSyntaxContext context)
  {
    using ImmutableArrayBuilder<DiagnosticInfo> diagnostics = ImmutableArrayBuilder<DiagnosticInfo>.Rent();

    if (context.TargetSymbol is not INamedTypeSymbol symbol)
      return new Result<RouteCandidateSnapshot?>(null, diagnostics.ToImmutable());

    LocationInfo? location = LocationInfo.CreateFrom(symbol.Locations.FirstOrDefault());

    try
    {
      // The chain: the page plus its [Layout<...>] ancestors, outermost first.
      var chain = new List<INamedTypeSymbol> { symbol };
      var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { symbol };

      for (var current = symbol; ; current = chain[0])
      {
        AttributeData? layoutAttr = current.GetAttributes()
          .FirstOrDefault(a => IsAttribute(a, Attributes.Layout1Full));
        if (layoutAttr?.AttributeClass?.TypeArguments.FirstOrDefault() is not INamedTypeSymbol layout)
          break;

        if (!visited.Add(layout))
        {
          diagnostics.Add(DiagnosticInfo.Create(
            Descriptors.TransformError, location,
            $"Circular [Layout<{layout.Name}>] chain on {symbol.Name}."));
          return new Result<RouteCandidateSnapshot?>(null, diagnostics.ToImmutable());
        }
        chain.Insert(0, layout);
      }

      // Each node's argument type — the [Routable<TArgs>] generic argument,
      // or none when the node is unparameterized.
      var nodes = new List<RouteNodeInfo>(chain.Count);
      foreach (INamedTypeSymbol nodeType in chain)
      {
        string? argsTypeDisplay = null;
        AttributeData? routable = nodeType.GetAttributes()
          .FirstOrDefault(a => IsAttribute(a, Attributes.RoutableGenericFull));
        if (routable?.AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol argsType)
          argsTypeDisplay = argsType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        nodes.Add(new RouteNodeInfo(
          nodeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
          argsTypeDisplay));
      }

      return new Result<RouteCandidateSnapshot?>(
        new RouteCandidateSnapshot(
          Hierarchy: TypeHierarchy.From(symbol),
          Type: TypeName.FromNamedTypeSymbol(symbol),
          Location: location,
          Nodes: nodes.ToEquatableArray()),
        diagnostics.ToImmutable());
    }
    catch (Exception ex)
    {
      diagnostics.Add(DiagnosticInfo.Create(Descriptors.TransformError, location, ex.Message));
      return new Result<RouteCandidateSnapshot?>(null, diagnostics.ToImmutable());
    }
  }

  /// <summary>
  ///   Turns a snapshot into the emitted locator's model: a type that only plays the layout role
  ///   generates nothing, and the locator's name comes from the trigger's own name.  The snapshot's
  ///   diagnostics travel with it.
  /// </summary>
  private static Result<RouteInfo?> Plan(Result<RouteCandidateSnapshot?> candidate, EquatableArray<TypeName> layoutRefs)
  {
    if (candidate.Value is not { } snapshot)
      return new Result<RouteInfo?>(null, candidate.Errors);

    // The layout role: referenced as another type's [Layout<T>] — it only
    // declares chain parameters, never generates a route class of its own.
    if (layoutRefs.Any(layout => layout.Equals(snapshot.Type)))
      return new Result<RouteInfo?>(null, candidate.Errors);

    return new Result<RouteInfo?>(
      new RouteInfo(
        Hierarchy: snapshot.Hierarchy,
        LocatorName: BuildLocatorName(snapshot.Hierarchy.Self.Name),
        Nodes: snapshot.Nodes),
      candidate.Errors);
  }

  /// <summary>Checks an attribute's metadata identity.</summary>
  private static bool IsAttribute(AttributeData attribute, string fullyQualifiedMetadataName)
    => attribute.AttributeClass is { } ac
       && GetAttributeMetadataName(ac) == fullyQualifiedMetadataName;

  private static string GetAttributeMetadataName(INamedTypeSymbol attributeClass)
  {
    var original = attributeClass.OriginalDefinition;
    var ns = original.ContainingNamespace.ToDisplayString();
    return $"{ns}.{original.MetadataName}";
  }

  /// <summary>
  ///   Builds the generated locator class name — the page's name stripped of
  ///   its <c>PageModel</c> / <c>ViewModel</c> suffix, plus <c>Locator</c>.
  /// </summary>
  internal static string BuildLocatorName(string typeName)
  {
    const string pageModel = Conventions.PageSuffix + Conventions.ModelSuffix; // "PageModel"
    const string viewModel = Conventions.ViewSuffix + Conventions.ModelSuffix; // "ViewModel"

    string stem = typeName.EndsWith(pageModel, StringComparison.Ordinal)
      ? typeName[..^pageModel.Length]
      : typeName.EndsWith(viewModel, StringComparison.Ordinal)
        ? typeName[..^viewModel.Length]
        : typeName;

    return stem + Conventions.LocatorSuffix;
  }

  private static void WrappedExecute(SourceProductionContext context, RouteInfo info)
    => ExecuteHelper.Execute(context, info, Execute);

  private static void Execute(SourceProductionContext context, RouteInfo info)
  {
    // Constructor: one parameter per parameterized node, in chain order;
    // the base call materializes the chain.
    var parameters = new List<ParameterSyntax>();
    var nodeExpressions = new List<ExpressionSyntax>();
    int argIndex = 0;
    foreach (RouteNodeInfo node in info.Nodes)
    {
      string? argsName = null;
      if (node.ArgsTypeDisplay is { } argsType)
      {
        argsName = $"args{argIndex++}";
        parameters.Add(Parameter(Identifier(argsName)).WithType(ParseTypeName(argsType)));
      }
      nodeExpressions.Add(NodeInvocation(node.TypeDisplay, argsName));
    }

    var chainArray = ArrayCreationExpression(
      ArrayType(ParseTypeName("global::Everlong.Nester.Routing.ITarget"))
        .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())),
      InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList(nodeExpressions)));

    var constructor = ConstructorDeclaration(Identifier(info.LocatorName))
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
      .WithParameterList(ParameterList(SeparatedList(parameters)))
      .WithInitializer(ConstructorInitializer(
        SyntaxKind.BaseConstructorInitializer,
        ArgumentList(SingletonSeparatedList(Argument(chainArray)))))
      .WithBody(Block());

    var classDeclaration = ClassDeclaration(Identifier(info.LocatorName))
      .WithAttributeLists(SyntaxHelpers.GeneratedMemberAttributes())
      .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword)))
      .WithLeadingTrivia(ParseLeadingTrivia($"/// <summary>The generated locator for <c>{info.Hierarchy.FilenameHint}</c>.</summary>\n"))
      .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(
        SimpleBaseType(ParseTypeName("global::Everlong.Nester.Routing.Locator")))))
      .WithMembers(SingletonList<MemberDeclarationSyntax>(constructor));

    var compilationUnit = info.Hierarchy.GetCompilationUnit(
      ImmutableArray<string>.Empty,
      classDeclaration);

    context.AddSource($"{info.Hierarchy.FilenameHint}.Locator{Conventions.GeneratorSuffix}", compilationUnit);
  }

  /// <summary>Builds <c>Target.Of(typeof(T), args?)</c>.</summary>
  private static ExpressionSyntax NodeInvocation(string typeDisplay, string? argsName)
  {
    var arguments = argsName is null
      ? new[] { Argument(TypeOfExpression(ParseTypeName(typeDisplay))) }
      : new[]
      {
        Argument(TypeOfExpression(ParseTypeName(typeDisplay))),
        Argument(IdentifierName(argsName)),
      };

    return InvocationExpression(
      ParseExpression("global::Everlong.Nester.Routing.Target.Of"),
      ArgumentList(SeparatedList(arguments)));
  }
}

/// <summary>A chain node of a generated locator — the participant type and its optional argument type.</summary>
internal sealed record RouteNodeInfo(string TypeDisplay, string? ArgsTypeDisplay);

/// <summary>
///   One [Routable] declaration, snapshotted in the transform: the hierarchy its locator is emitted
///   for, its own name (the layout role is an assembly-wide question, answered later) and its
///   resolved chain.
/// </summary>
internal sealed record RouteCandidateSnapshot(
  TypeHierarchy Hierarchy,
  TypeName Type,
  LocationInfo? Location,
  EquatableArray<RouteNodeInfo> Nodes);

/// <summary>The model of a generated locator class.</summary>
internal sealed record RouteInfo(
  TypeHierarchy Hierarchy,
  string LocatorName,
  EquatableArray<RouteNodeInfo> Nodes);
