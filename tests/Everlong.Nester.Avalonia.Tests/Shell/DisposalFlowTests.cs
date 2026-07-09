using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The unified disposal flow (F1 closure): <see cref="IShell.DisposeAsync" />
///   is the single teardown entry — stops the shell token, releases layers
///   and the container; intents are short-circuited after disposal; the
///   window-close translation (the host window's <c>OnClosing</c>) arbitrates
///   through the intent chain with veto support.
/// </summary>
[Collection("RealShell")]
public class DisposalFlowTests
{
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


  /// <summary>The internal ctor needs reflection — the translator only reads <see cref="WindowClosingEventArgs.Cancel" />.</summary>
  private static WindowClosingEventArgs NewClosingArgs()
    => (WindowClosingEventArgs)System.Activator.CreateInstance(
      typeof(WindowClosingEventArgs),
      System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
      null,
      [Avalonia.Controls.WindowCloseReason.WindowClosing, false],
      null)!;

  // ── Lifetime signals ─────────────────────────────

  [AvaloniaFact]
  public async Task DisposeAsync_CancelsStoppingSignal()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    var token = shell.Shell.Lifetime.Stopping;

    Assert.False(token.IsCancellationRequested);
    await shell.Shell.DisposeAsync();

    Assert.True(token.IsCancellationRequested, "Disposing the shell must stop its token.");
  }

  [AvaloniaFact]
  public async Task DisposeAsync_FiresStoppedSignal_AfterTeardownCompletes()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    var lifetime = shell.Services.GetRequiredService<IShellLifetime>();

    Assert.False(lifetime.Stopped.IsCancellationRequested);
    await shell.Shell.DisposeAsync();

    Assert.True(lifetime.Stopping.IsCancellationRequested);
    Assert.True(lifetime.Stopped.IsCancellationRequested, "The Stopped signal fires when the teardown cascade completes.");
  }

  [AvaloniaFact]
  public async Task DisposeAsync_FaultsUnsettledStartup()
  {
    // A shell disposed before its startup flow settled: the Startup signal
    // faults — an unstarted-then-disposed shell's startup is terminal and
    // waiters must never hang.
    var shell = TestHost.CreateShell<RealShell.RealTestContext>();
    await shell.DisposeAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => shell.Lifetime.Startup);
  }

  // ── Intent short-circuit after disposal ─────────

  [AvaloniaFact]
  public async Task DisposeAsync_AfterDisposal_IntentsAreShortCircuited()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    await shell.Shell.DisposeAsync();

    // A destroyed shell must not answer intents — the window-close re-entry
    // path depends on this (no arbitration, no exceptions).
    Assert.Equal(IntentResult.Pass, await shell.Shell.DispatchIntent(null, new CloseIntent()));
    Assert.Equal(IntentResult.Pass, await shell.Shell.DispatchIntent(null, new TryCloseIntent()));
  }

  // ── Window-close translation (the host window's OnClosing) ──

  [AvaloniaFact]
  public async Task WindowClosing_Translation_HoldsClose_AndArbitratesThroughChain()
  {
    // A vetoing Director proves the TryCloseIntent really walked the chain:
    // were it not delivered, nothing could veto it.
    var shell = RealShell.Create<VetoingDirector>();
    var e = NewClosingArgs();

    shell.Shell.WindowClosingToTryCloseIntent(e);

    Assert.True(e.Cancel, "The translator must hold the close while the chain arbitrates.");
    Assert.False(shell.Shell.Lifetime.Lifecycle == ShellLifecycle.Disposed, "A vetoed close must leave the shell alive.");
    await shell.Shell.DisposeAsync();
  }

  [AvaloniaFact]
  public async Task WindowClosing_AfterDisposal_FallsThrough()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    await shell.Shell.DisposeAsync();

    // Re-entry (Close → DisposeAsync → window.Close → Closing): the
    // destroyed shell must let the close fall through — no arbitration.
    var e = NewClosingArgs();
    shell.Shell.WindowClosingToTryCloseIntent(e);

    Assert.False(e.Cancel, "A disposed shell must not hold the close.");
  }
}
