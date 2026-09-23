using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   The marker for shell-lifecycle intents — the shell's own end.
/// </summary>
public interface IShellIntent : IIntent;

/// <summary>
///   The marker for window intents — the chrome and the placement of the
///   shell's host window.
/// </summary>
public interface IWindowIntent : IIntent;

/// <summary>Represents an intent to show the shell's window.</summary>
public sealed record ShowIntent : IWindowIntent;

/// <summary>Represents an intent to hide the shell's window.</summary>
public sealed record HideIntent : IWindowIntent;

/// <summary>Represents an intent to close the shell, open to a refusal.</summary>
public sealed record TryCloseIntent : IShellIntent;

/// <summary>Represents an intent to close the shell, dispatched where no refusal is sought.</summary>
public sealed record CloseIntent : IShellIntent;

/// <summary>Represents an intent to alter the shell's window state.</summary>
public sealed record MutateShellStateIntent(HostState TargetState) : IWindowIntent;

/// <summary>Represents an intent to restore the shell's window state from the platform defaults.</summary>
public sealed record RestoreShellStateIntent : IWindowIntent;

/// <summary>Represents an intent to toggle whether the shell's window is topmost.</summary>
/// <param name="IsTopmost"><see langword="true" /> to make the window topmost; otherwise <see langword="false" />.</param>
public sealed record TopmostIntent(bool IsTopmost = true) : IWindowIntent;

/// <summary>Represents an intent to center the shell's window on the screen.</summary>
public sealed record CenterOnScreenIntent : IWindowIntent;
