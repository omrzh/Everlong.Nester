using System.Collections.Concurrent;
using System.Collections.Immutable;
using Everlong.Nester.Generators.Constants;
using Everlong.Nester.Generators.Helpers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Everlong.Nester.Generators.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MappingAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    ImmutableArray.Create(Descriptors.MappingConflict, Descriptors.MappingRedundant);

  public override void Initialize(AnalysisContext context)
  {
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterCompilationStartAction(compilationContext =>
    {
      // vmFQN -> (viewShortName, location) from [ViewFor<TVM>] on views
      var viewForMappings = new ConcurrentDictionary<string, (string ViewName, Location Location)>();

      // (vmFQN, viewName, location) from [Mapping<TModel, TView>] on ViewLocator classes
      var explicitMappings = new ConcurrentBag<(string VmFQN, string ViewName, Location Location)>();

      compilationContext.RegisterSymbolAction(symbolContext =>
      {
        var namedType = (INamedTypeSymbol)symbolContext.Symbol;

        // Check for [ViewFor<TVM>] on this type
        foreach (var attr in namedType.GetAttributes())
        {
          if (attr.AttributeClass is not { Name: "ViewForAttribute", TypeArguments.Length: 1 } attrClass)
            continue;
          if (attrClass.ContainingNamespace.ToDisplayString() != Ns.NesterPresentation)
            continue;
          if (attrClass.TypeArguments[0] is not INamedTypeSymbol vmType)
            continue;

          var vmFQN = vmType.ToDisplayString();
          var location = attr.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken).GetLocation()
                         ?? namedType.Locations.FirstOrDefault()
                         ?? Location.None;

          viewForMappings.TryAdd(vmFQN, (namedType.Name, location));
        }

        // Check for [ViewLocator] + [Mapping<TModel, TView>] on this type
        bool hasViewLocator = namedType.GetAttributes().Any(a =>
          a.AttributeClass?.Name == Attributes.ViewLocator &&
          a.AttributeClass.ContainingNamespace.ToDisplayString() == Ns.NesterPresentation);

        if (!hasViewLocator)
          return;

        foreach (var attr in namedType.GetAttributes())
        {
          if (attr.AttributeClass is not { Name: "MappingAttribute", TypeArguments.Length: 2 } attrClass)
            continue;
          if (!ViewGeneratorSymbolHelper.IsMappingNamespace(attrClass.ContainingNamespace))
            continue;
          if (attrClass.TypeArguments[0] is not INamedTypeSymbol modelType ||
              attrClass.TypeArguments[1] is not INamedTypeSymbol viewType)
            continue;

          var vmFQN = modelType.ToDisplayString();
          var location = attr.ApplicationSyntaxReference?.GetSyntax(symbolContext.CancellationToken).GetLocation()
                         ?? namedType.Locations.FirstOrDefault()
                         ?? Location.None;

          explicitMappings.Add((vmFQN, viewType.Name, location));
        }
      }, SymbolKind.NamedType);

      compilationContext.RegisterCompilationEndAction(endContext =>
      {
        foreach (var (vmFQN, dtViewName, dtLocation) in explicitMappings)
        {
          if (!viewForMappings.TryGetValue(vmFQN, out var vfEntry))
            continue;

          var (vfViewName, vfLocation) = vfEntry;

          if (vfViewName == dtViewName)
          {
            // Same view — Mapping is redundant (NSTR1002), report on Mapping attribute
            endContext.ReportDiagnostic(Diagnostic.Create(
              Descriptors.MappingRedundant,
              dtLocation,
              vmFQN,
              dtViewName));
          }
          else
          {
            // Different view — conflict (NSTR1001), report on ViewFor attribute (the overridden one)
            endContext.ReportDiagnostic(Diagnostic.Create(
              Descriptors.MappingConflict,
              vfLocation,
              vmFQN,
              dtViewName,
              vfViewName));
          }
        }
      });
    });
  }
}
