using Everlong.Nester.Generators.Models;
namespace Everlong.Nester.Generators.Views;

/// <summary>
///   Resolves the assembly's declared bindings: the <c>[ViewFor&lt;TViewModel&gt;]</c> views,
///   overridden per view model by the trigger's <c>[Mapping&lt;TViewModel, TView&gt;]</c>.
///   A view model without a declaration yields no binding.
/// </summary>
internal static class ViewCollectionHelper
{
  internal static EquatableArray<PairedInfo> Match(
    EquatableArray<ViewCandidate> views,
    EquatableArray<MappingModel> attachedMappings)
  {
    // Pairing: the [ViewFor] declarations first — one view per view model, the first declared view
    // outside the framework's own namespace winning — then the trigger's [Mapping] entries, which
    // replace whatever was declared for their view model and are the whole declaration for a view
    // model that carries no [ViewFor].  A view model with neither yields no binding and no diagnostic,
    // and the result is ordered by view-model name so the incremental output is stable.
    // One view per view model: among several [ViewFor] declarations the first outside the
    // framework's own namespace wins; candidates arrive sorted, so ties are stable.
    var bindings = new Dictionary<string, PairedInfo>(StringComparer.Ordinal);
    foreach (var view in views.OrderBy(static v => v.Type.FullyQualified, StringComparer.Ordinal))
    {
      foreach (var viewModel in view.ViewForViewModels)
      {
        if (bindings.TryGetValue(viewModel.FullyQualified, out var existing)
            && !Prefer(view.Type, existing.View))
        {
          continue;
        }

        bindings[viewModel.FullyQualified] = new PairedInfo(viewModel, view.Type);
      }
    }

    // The trigger's explicit mapping replaces the declared view for its view model, and is
    // itself the whole declaration for a view model that carries no [ViewFor].
    foreach (var mapping in attachedMappings)
    {
      bindings[mapping.ViewModel.FullyQualified] = new PairedInfo(mapping.ViewModel, mapping.View);
    }

    return bindings.Values
      .OrderBy(static pair => pair.ViewModel.FullyQualified, StringComparer.Ordinal)
      .ToEquatableArray();
  }

  /// <summary>A view outside the framework's own namespace beats one inside it.</summary>
  private static bool Prefer(TypeName candidate, TypeName current)
    => IsFrameworkNamespace(current.Namespace) && !IsFrameworkNamespace(candidate.Namespace);

  private static bool IsFrameworkNamespace(string ns)
    => ns.StartsWith("Everlong.Nester.", StringComparison.Ordinal);
}
