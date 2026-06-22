using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Analyzers;

/// <summary>
///   Reports generation targets and enclosing types that are not declared <c>partial</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialKeywordAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Descriptors.TargetPartial, Descriptors.EnclosingTypePartial);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSyntaxNodeAction(AnalyzeNode,
      SyntaxKind.ClassDeclaration,
      SyntaxKind.RecordDeclaration);
  }

  private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
  {
    if (context.Node is not TypeDeclarationSyntax typeDecl)
      return;

    if (context.SemanticModel.GetDeclaredSymbol(typeDecl) is not { } symbol)
      return;

    // Both diagnostics report on the analyzed declaration: a diagnostic reported on another node is
    // classified as compilation-level by Roslyn, and the IDE offers no code fix for those.
    if (typeDecl.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
      return;

    if (RequiresPartial(symbol))
    {
      context.ReportDiagnostic(Diagnostic.Create(Descriptors.TargetPartial,
                                                 typeDecl.Identifier.GetLocation(),
                                                 symbol.Name));
    }

    if (FindNestedTarget(symbol) is { } nestedTarget)
    {
      context.ReportDiagnostic(Diagnostic.Create(Descriptors.EnclosingTypePartial,
                                                 typeDecl.Identifier.GetLocation(),
                                                 symbol.Name,
                                                 nestedTarget.Name));
    }
  }

  /// <summary>
  /// The nearest type inside <paramref name="symbol" /> that is generated into — at any depth, since
  /// every type on the way to it needs the keyword as well.
  /// </summary>
  private static INamedTypeSymbol? FindNestedTarget(INamedTypeSymbol symbol)
  {
    // Known residual: once the trigger is `partial` but an enclosing type is not, the non-partial
    // declaration's symbol carries no nested members — this walk finds nothing and the user sees the
    // compiler's CS0260 instead of NSTR0008.
    foreach (var nested in symbol.GetTypeMembers())
    {
      if (RequiresPartial(nested))
        return nested;

      if (FindNestedTarget(nested) is { } deeper)
        return deeper;
    }

    return null;
  }

  private static bool RequiresPartial(INamedTypeSymbol symbol)
  {
    foreach (var attr in symbol.GetAttributes())
    {
      if (attr.AttributeClass == null)
        continue;

      var attrName = attr.AttributeClass.Name;
      var attrNs = attr.AttributeClass.ContainingNamespace?.ToDisplayString();

      if (string.IsNullOrEmpty(attrNs))
        continue;

      if (attrNs == Ns.NesterRouting)
      {
        if (attrName == "RoutableAttribute")
          return true;
      }
      else if (attrNs == Ns.NesterPresentation)
      {
        if (attrName is "ViewLocatorAttribute" or "WpfViewLocatorAttribute")
          return true;
      }
      else if (attrNs == Ns.NesterDi)
      {
        if (attrName is "InjectAttribute" or "ServiceRegistrarAttribute")
          return true;
      }
    }

    foreach (var member in symbol.GetMembers())
    {
      if (member is not (IPropertySymbol or IFieldSymbol))
        continue;

      foreach (var attr in member.GetAttributes())
      {
        if (attr.AttributeClass?.Name == "InjectAttribute" &&
            attr.AttributeClass.ContainingNamespace?.ToDisplayString() == Ns.NesterDi)
        {
          return true;
        }
      }
    }

    return false;
  }
}
