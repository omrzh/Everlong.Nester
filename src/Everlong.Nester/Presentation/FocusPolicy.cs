namespace Everlong.Nester.Presentation;

/// <summary>The layer content's declared keyboard-focus participation.</summary>
public enum FocusPolicy
{
  /// <summary>The content takes part in Tab navigation.</summary>
  Reachable,

  /// <summary>The content traps Tab and excludes every layer beneath it from Tab navigation.</summary>
  Trapped,
}

/// <summary>
///   The Tab-navigation intent of a layer's content.  A surface whose
///   content declares no intent is excluded from Tab navigation.
/// </summary>
public interface IFocusPolicySurface
{
  /// <summary>The content's Tab-navigation intent.</summary>
  FocusPolicy FocusPolicy { get; set; }

  /// <summary>Raised when <see cref="FocusPolicy" /> changes.</summary>
  event Action? FocusPolicyChanged;
}
