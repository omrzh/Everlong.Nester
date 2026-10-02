// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>The layer's keyboard-focus participation.</summary>
internal enum FocusPolicy
{
  /// <summary>The layer takes part in Tab navigation.</summary>
  Reachable,

  /// <summary>The layer traps Tab and excludes every layer beneath it from Tab navigation.</summary>
  Trapped,
}
