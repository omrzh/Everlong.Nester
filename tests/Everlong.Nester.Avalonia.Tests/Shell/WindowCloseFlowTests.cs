using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Hosting;
using Xunit;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The window-owned close chain: the host window's <c>OnClosing</c>
///   override translates the close into a <see cref="TryCloseIntent" /> — the
///   framework never hooks <c>Window.Closing</c>.  Unlike
///   <see cref="DisposalFlowTests" /> (which calls the translator directly),
///   these tests drive a REAL window: <c>Close()</c> → <c>OnClosing</c> →
///   translator → intent chain → shell fallback (<c>DisposeAsync</c> +
///   re-entrant <c>Close()</c>, short-circuited by <c>Lifecycle</c>).
/// </summary>
[Collection("RealShell")]
public class WindowCloseFlowTests
{
  /// <summary>A host window that owns its close — the template pattern: base first, then translate.</summary>
  private sealed class TestShellWindow : Window, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    /// <inheritdoc />
    public void HostShell(IAvaloniaShell shell, PlatformControl stage)
    {
      _contentLayer.Content = stage;
      _shell ??= shell;
    }

    internal IAvaloniaShell? Shell
    {
      get => _shell;   // wired at HostShell — the window sits above the stage, it cannot crawl to it
      init => _shell = value;
    }
    private IAvaloniaShell? _shell;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
      base.OnClosing(e);
      if (Shell is { } shell)   // before mount there is no shell — the close falls through untranslated
        shell.WindowClosingToTryCloseIntent(e);
    }
  }

  /// <summary>Director that vetoes TryClose (the user's close guard).</summary>
  private sealed class VetoingDirector : IShellDirector
  {

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      if (context.Intent is TryCloseIntent)
      {
        context.Veto();
        return ValueTask.CompletedTask;
      }
      return next(context);
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Assembles a window-shell over a real window host and starts it (the window is shown at mount).</summary>
  private static (AvaloniaShell Shell, TestShellWindow Window) CreateWindowShell<TDirector>()
    where TDirector : class, IShellDirector, new()
  {
    var window = new TestShellWindow();
    var shell = TestHost.CreateShell<TDirector>(null, out _, rootView: window);
    shell.Start();
    return (shell, window);
  }

  /// <summary>Flushes posted continuations — the translator awaits the dispatch, so assertions must run after any posted hop.</summary>
  private static Task FlushUiThreadAsync()
    => Dispatcher.UIThread.InvokeAsync(() => { }).GetTask();

  // ── The window-owned close chain ──────────────

  [AvaloniaFact]
  public async Task WindowClose_TranslatesThroughChain_DisposesShell_ClosesWindow()
  {
    var (shell, window) = CreateWindowShell<RealShell.RealTestContext>();

    Assert.True(window.IsVisible, "The host window presents itself at mount (OnAssembled).");

    window.Close();
    await FlushUiThreadAsync();

    Assert.True(shell.Lifetime.Lifecycle == ShellLifecycle.Disposed, "The close must reach the shell fallback and dispose the shell.");
    Assert.False(window.IsVisible, "The re-entrant Close must complete after disposal.");
  }

  [AvaloniaFact]
  public async Task WindowClose_VetoingDirector_KeepsWindowOpen_ShellAlive()
  {
    var (shell, window) = CreateWindowShell<VetoingDirector>();

    window.Close();
    await FlushUiThreadAsync();

    Assert.False(shell.Lifetime.Lifecycle == ShellLifecycle.Disposed, "A vetoed close must leave the shell alive.");
    Assert.True(window.IsVisible, "A vetoed close must keep the window open.");

    await shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task WindowClose_AfterDisposal_SecondCloseFallsThrough_NoThrow()
  {
    var (shell, window) = CreateWindowShell<RealShell.RealTestContext>();
    window.Close();
    await FlushUiThreadAsync();
    Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);

    // Re-entry after disposal: the translator short-circuits (Lifecycle) —
    // the second close falls through untranslated, no arbitration, no throw.
    window.Close();
    await FlushUiThreadAsync();

    Assert.False(window.IsVisible);
  }

  [AvaloniaFact]
  public void WindowClose_BeforeMount_FallsThrough_NoShell()
  {
    var window = new TestShellWindow();

    // No shell attached yet — the close falls through untranslated (the
    // Shell resolves null; the translator is never called).
    window.Close();

    Assert.False(window.IsVisible);
  }

  // ── App-exit backstop (the DisposeOnExit cascade) ──

  [AvaloniaFact]
  public async Task AppExitCascade_DisposesTrackedShells_Idempotent()
  {
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      var (shell, window) = CreateWindowShell<RealShell.RealTestContext>();
      impl.Track(shell);

      // The window chain already disposed the shell — the exit cascade must
      // no-op on it (per-shell DisposeAsync is idempotent).
      window.Close();
      await FlushUiThreadAsync();
      Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);

      await impl.DisposeAsync();   // must not throw on already-disposed shells

      Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);
    }
    finally
    {
      MainDispatcher.ResetForTesting();
    }
  }

  /// <summary>Observable <see cref="AppLifetimeBase" /> — exposes the tracked set for registry assertions.</summary>
  private sealed class ObservableLifetime : AppLifetimeBase
  {
    public override bool IsSingleView => false;
    protected override void ReplaceSingleViewShell(IShell shellContext) { }
    protected override void ShutdownCore(int exitCode) { }
    public bool Tracks(IShell shell) => Shells.Contains(shell);
  }

  [AvaloniaFact]
  public async Task ShellDisposeAsync_UntracksItself_FromCascade()
  {
    AppLifetime.BindImpl(new ObservableLifetime());
    try
    {
      var lifetime = (ObservableLifetime)AppLifetime.Current!;
      var (shell, window) = CreateWindowShell<RealShell.RealTestContext>();

      // Start() tracked the shell through the bound facade (mount point).
      Assert.True(lifetime.Tracks(shell));

      window.Close();
      await FlushUiThreadAsync();

      // The shell's own teardown deregistered it — the app-exit cascade
      // will never re-dispose it.
      Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);
      Assert.False(lifetime.Tracks(shell));
    }
    finally
    {
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();
    }
  }

  [AvaloniaFact]
  public async Task UntrackAsync_IsIdempotent_CascadeSkipsUntracked()
  {
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      var (shell, _) = CreateWindowShell<RealShell.RealTestContext>();

      await impl.UntrackAsync(shell);  // never tracked — no-op
      impl.Track(shell);
      await impl.UntrackAsync(shell);  // removes
      await impl.UntrackAsync(shell);  // already removed — no-op

      await impl.DisposeAsync();       // the cascade sweeps an empty registry

      Assert.Equal(ShellLifecycle.Started, shell.Lifetime.Lifecycle); // untouched by the cascade
      await shell.DisposeAsync();      // cleanup
    }
    finally
    {
      MainDispatcher.ResetForTesting();
    }
  }

  [AvaloniaFact]
  public async Task Track_AfterLifetimeDisposed_Throws()
  {
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      var (shell, _) = CreateWindowShell<RealShell.RealTestContext>();
      impl.Track(shell);

      await impl.DisposeAsync(); // the cascade disposes the tracked shell

      Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);
      Assert.Throws<InvalidOperationException>(() => impl.Track(shell));
    }
    finally
    {
      MainDispatcher.ResetForTesting();
    }
  }
}
