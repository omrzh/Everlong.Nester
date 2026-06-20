using Everlong.Nester.Generators.Models;
namespace Everlong.Nester.Generators.Views;

/// <summary>
///   The result of the unified collection phase — the trigger's registration state and the
///   bindings the provider branches render.
/// </summary>
internal sealed record UnifiedCollectionResult(
  UiFramework UiFramework,
  RegistrationModel Registration,
  EquatableArray<PairedInfo> Pairs,
  EquatableArray<DiagnosticInfo> Diagnostics
);
