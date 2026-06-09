using System.Windows;
using System.Windows.Input;
using Everlong.Nester.Intent;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's routed commands and the static methods that post their
///   intents.
/// </summary>
public static class IntentCommands
{
  // ── Shell-state intents ────────────────────────────────────────
  /// <summary>The command that posts a <see cref="TryCloseIntent" />.</summary>
  public static readonly RoutedCommand CloseCommand = new(nameof(CloseCommand), typeof(IntentCommands));
  /// <summary>The command that posts a minimized <see cref="MutateShellStateIntent" />.</summary>
  public static readonly RoutedCommand MinimizeCommand = new(nameof(MinimizeCommand), typeof(IntentCommands));
  /// <summary>The command that posts a maximized <see cref="MutateShellStateIntent" />.</summary>
  public static readonly RoutedCommand MaximizeCommand = new(nameof(MaximizeCommand), typeof(IntentCommands));
  /// <summary>The command that posts a <see cref="RestoreShellStateIntent" />.</summary>
  public static readonly RoutedCommand RestoreCommand = new(nameof(RestoreCommand), typeof(IntentCommands));
  /// <summary>The command that posts a full-screen <see cref="MutateShellStateIntent" />.</summary>
  public static readonly RoutedCommand FullScreenCommand = new(nameof(FullScreenCommand), typeof(IntentCommands));

  // ── Generic intent ─────────────────────────────────────────────
  /// <summary>The generic intent command — the caller supplies the <see cref="IIntent" /> as the command parameter.</summary>
  public static readonly RoutedCommand IntentCommand = new(nameof(IntentCommand), typeof(IntentCommands));

  // ── Static execution methods ──────────────────────────────────
  /// <summary>Posts a <see cref="TryCloseIntent" /> from <paramref name="control" />.</summary>
  public static void Close(DependencyObject control)
    => control.PostIntent(new TryCloseIntent());

  /// <summary>Posts a minimized <see cref="MutateShellStateIntent" /> from <paramref name="control" />.</summary>
  public static void Minimize(DependencyObject control)
    => control.PostIntent(new MutateShellStateIntent(HostState.Minimized));

  /// <summary>Posts a maximized <see cref="MutateShellStateIntent" /> from <paramref name="control" />.</summary>
  public static void Maximize(DependencyObject control)
    => control.PostIntent(new MutateShellStateIntent(HostState.Maximized));

  /// <summary>Posts a <see cref="RestoreShellStateIntent" /> from <paramref name="control" />.</summary>
  public static void Restore(DependencyObject control)
    => control.PostIntent(new RestoreShellStateIntent());

  /// <summary>Posts a full-screen <see cref="MutateShellStateIntent" /> from <paramref name="control" />.</summary>
  public static void FullScreen(DependencyObject control)
    => control.PostIntent(new MutateShellStateIntent(HostState.FullScreen));


  /// <summary>Posts <paramref name="intent" /> from <paramref name="control" />.</summary>
  public static void PostIntent(DependencyObject control, IIntent intent)
    => control.PostIntent(intent);

  // ── CommandBinding registration ───────────────────────────────
  /// <summary>Binds the five shell-state commands to their handlers on <paramref name="target" />.</summary>
  public static void RegisterCommandBindings(UIElement target)
  {
    target.CommandBindings.Add(new CommandBinding(
                                 CloseCommand,
                                 (s, _) => Close((DependencyObject)s)));

    target.CommandBindings.Add(new CommandBinding(
                                 MinimizeCommand,
                                 (s, _) => Minimize((DependencyObject)s)));

    target.CommandBindings.Add(new CommandBinding(
                                 MaximizeCommand,
                                 (s, _) => Maximize((DependencyObject)s)));

    target.CommandBindings.Add(new CommandBinding(
                                 RestoreCommand,
                                 (s, _) => Restore((DependencyObject)s)));

    target.CommandBindings.Add(new CommandBinding(
                                 FullScreenCommand,
                                 (s, _) => FullScreen((DependencyObject)s)));
  }
}
