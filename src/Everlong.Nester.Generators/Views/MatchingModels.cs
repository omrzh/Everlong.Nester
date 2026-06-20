using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

/// <summary>
///   A declared view-model to view binding — the trigger's <c>[Mapping&lt;TViewModel, TView&gt;]</c>.
/// </summary>
internal sealed record MappingModel(
  TypeName ViewModel,
  TypeName View)
{
  internal static MappingModel From(INamedTypeSymbol vmSymbol, INamedTypeSymbol viewSymbol)
    => new(TypeName.FromNamedTypeSymbol(vmSymbol), TypeName.FromNamedTypeSymbol(viewSymbol));
}

/// <summary>A resolved view-model to view binding.</summary>
internal sealed record PairedInfo(
  TypeName ViewModel,
  TypeName View);
