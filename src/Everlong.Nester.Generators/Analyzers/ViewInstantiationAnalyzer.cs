using System.Collections.Immutable;
using Everlong.Nester.Generators.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Analyzers;

/// <summary>
///   The [ViewFor&lt;T&gt;]-anchored instantiation rules: the generated locator only ever writes
///   <c>new View()</c>, so a view must be a class the locator can name — non-generic, non-static and
///   reachable from the assembly (NSTR1004) — with a parameterless constructor it can call (NSTR1003).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ViewInstantiationAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Descriptors.ViewNotInstantiable, Descriptors.ViewNeedsParameterlessConstructor);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
  }

  private static void AnalyzeType(SymbolAnalysisContext context)
  {
    var type = (INamedTypeSymbol)context.Symbol;
    if (type.TypeKind is not TypeKind.Class)
      return;
    if (type.IsAbstract)
      return;
    if (!type.Locations.Any(static location => location.IsInSource))
      return;
    if (!ViewGeneratorSymbolHelper.HasViewFor(type))
      return;

    var location = type.Locations.First(static l => l.IsInSource);

    if (ViewGeneratorSymbolHelper.CannotInstantiateReason(type) is { } reason)
    {
      context.ReportDiagnostic(Diagnostic.Create(Descriptors.ViewNotInstantiable,
                                                 location,
                                                 type.Name,
                                                 reason));
      return;
    }

    bool hasAccessibleParameterless = type.InstanceConstructors.Any(static c =>
      c.Parameters.Length == 0 &&
      c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal);

    if (hasAccessibleParameterless)
      return;

    // An implicit default constructor is public and appears in
    // InstanceConstructors — only a type that declares no accessible
    // parameterless constructor lands here.
    context.ReportDiagnostic(Diagnostic.Create(Descriptors.ViewNeedsParameterlessConstructor,
                                               location,
                                               type.Name));
  }
}
