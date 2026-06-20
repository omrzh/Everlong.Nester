using Everlong.Nester.Generators.Models;
namespace Everlong.Nester.Generators.Views;

/// <summary>
///   Raw scan output: one view type and the view models it declares itself for via
///   <c>[ViewFor&lt;TViewModel&gt;]</c>.
/// </summary>
internal sealed record ViewCandidate(
  TypeName Type,
  EquatableArray<TypeName> ViewForViewModels);
