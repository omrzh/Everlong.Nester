using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   Represents an intent related to the shell, which is intended to be handled by the shell itself or its owner.
/// </summary>
public interface IShellIntent : IIntent;

/// <summary>
///   Represents an intent to show the shell.
///   <seealso cref="IShellIntent"/>
/// </summary>
public sealed record ShowIntent : IShellIntent;

/// <summary>
///   Represents an intent to hide the shell.
/// </summary>
public sealed record HideIntent : IShellIntent;

/// <summary>
///   Represents an intent to close the shell and can be canceled.
/// </summary>
public sealed record TryCloseIntent : IShellIntent;

/// <summary>
///   Represents an intent to close the shell, semantically saying avoid cancellation.
/// </summary>
public sealed record CloseIntent : IShellIntent;

/// <summary>
///   Represents an intent to alter the shell state.
/// </summary>
public sealed record MutateShellStateIntent(HostState TargetState) : IShellIntent;

/// <summary>
///   Represents an intent to restore the shell state from platform defaults.
/// </summary>
public sealed record RestoreShellStateIntent : IShellIntent;

/// <summary>
///   Represents an intent to toggle whether the shell is topmost.
/// </summary>
/// <param name="IsTopmost"><see langword="true" /> to make the shell topmost; otherwise <see langword="false" />.</param>
public sealed record TopmostIntent(bool IsTopmost = true) : IShellIntent;

/// <summary>
///   Represents an intent to center the shell on the screen.
/// </summary>
public sealed record CenterOnScreenIntent : IShellIntent;

/// <summary>
///   Represents an intent to center the shell on its owner.
/// </summary>
public sealed record CenterOnOwnerIntent : IShellIntent;
