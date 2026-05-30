// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   The direction of a view transition.
/// </summary>
public enum TransitionKind
{
  /// <summary>A view arrives.</summary>
  Enter = 0,

  /// <summary>The current view departs.</summary>
  Exit = 1,

  /// <summary>The current view is re-presented in place.</summary>
  Refresh = 2,

  /// <summary>The current view departs with no arriving view.</summary>
  Dismiss = 3
}
