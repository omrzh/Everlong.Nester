using Everlong.Nester.Generators.Helpers;
using Everlong.Nester.Generators.Models;
namespace Everlong.Nester.Generators.Views;

public abstract partial class DataTemplateGeneratorBase
{
  private UnifiedCollectionResult? BuildUnifiedResult(
    TriggerPlan triggerPlan,
    EquatableArray<ViewCandidate> views)
  {
    var trigger = triggerPlan.Trigger;
    if (trigger == null)
    {
      if (triggerPlan.Diagnostics.Length == 0)
      {
        return null;
      }

      return new UnifiedCollectionResult(
        UiFramework.Error,
        null!,
        EquatableArray<PairedInfo>.Empty,
        triggerPlan.Diagnostics);
    }

    using var diagnostics = ImmutableArrayBuilder<DiagnosticInfo>.Rent();
    diagnostics.AddRange(triggerPlan.Diagnostics.AsSpan());

    try
    {
      EquatableArray<PairedInfo> pairs =
        ViewCollectionHelper.Match(views, trigger.Registration.AttachedContracts);

      return new UnifiedCollectionResult(
        TargetFramework,
        trigger.Registration,
        pairs,
        diagnostics.ToImmutable());
    }
    catch (Exception ex)
    {
      diagnostics.Add(CreateDiagnostic(Descriptors.TransformError, trigger.Location, ex.Message));
      return new UnifiedCollectionResult(
        UiFramework.Error,
        null!,
        EquatableArray<PairedInfo>.Empty,
        diagnostics.ToImmutable());
    }
  }
}
