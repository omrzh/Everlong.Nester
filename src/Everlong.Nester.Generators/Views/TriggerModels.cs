using Everlong.Nester.Generators.Models;
namespace Everlong.Nester.Generators.Views;

/// <summary>
/// Fully resolved registration state captured from the trigger class (ViewLocator / WpfViewLocator),
/// including all explicitly declared mappings and the class identity.
/// </summary>
internal sealed record RegistrationModel(
  TypeHierarchy Hierarchy,
  EquatableArray<MappingModel> AttachedContracts
);

/// <summary>
/// Raw attribute snapshot collected from a single trigger class before validation.
/// </summary>
internal sealed record TriggerSnapshot(
  RegistrationModel Registration,
  LocationInfo? Location
);

/// <summary>Normalized trigger state after validation: 0 or 1 active trigger, plus any validation diagnostics.</summary>
internal sealed record TriggerPlan(
  TriggerSnapshot? Trigger,
  EquatableArray<DiagnosticInfo> Diagnostics);
