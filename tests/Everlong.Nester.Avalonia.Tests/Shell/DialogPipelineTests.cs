using Everlong.Nester.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   The dialog domain over the routing pipeline: a session
///   presents as a close-stack overlay — the chain [dimmer, session] built
///   by the ShowAsync extension itself, routed through the shell's router.
///   The lifecycle callbacks come
///   from the Routing contracts (<see cref="IArriving" /> /
///   <see cref="IArrived" /> / <see cref="IDeparted" /> /
///   <see cref="IReleasable" />) — the parameter delivery is
///   <see cref="IParameterized" />, driven by the pipeline itself.
/// </summary>
[Collection("RealShell")]
public class DialogPipelineTests
{
  // ── Session types ─────────────────────────────────────────

  public sealed class SimpleDialog : DialogSessionBase
  {
    public void Close(string? result = null) => Completion?.Complete(result);
  }

  public sealed class AwareDialog : DialogSessionBase, IArriving, IDeparted, IReleasable
  {
    public int Arrived { get; private set; }
    public int Departed { get; private set; }
    public int Released { get; private set; }
    public Task OnArrivingAsync(IRoutingContext context) => Task.CompletedTask;
    public override Task OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return base.OnArrivedAsync(context);
    }
    public void OnDeparted(IRoutingContext context) { Departed++; }
    public void Release() { Released++; }
    public void Close(string? result = null) => Completion?.Complete(result);
  }

  /// <summary>A session that records the participant-entry protocol.</summary>
  public sealed class ArgsReceivingDialog : DialogSessionBase, IParameterized
  {
    public IArgs? Received { get; private set; }
    public int Arrived { get; private set; }
    public IArgs? EngagedArgs => Received;
    public void DeliverArgs(IArgs? args) => Received = args;
    public override Task OnArrivedAsync(IRoutingContext context)
    {
      Arrived++;
      return base.OnArrivedAsync(context);
    }
    public void Close(string? result = null) => Completion?.Complete(result);
  }

  private sealed class ViewLocator : IDataTemplate
  {
    public bool Match(object? data)
      => data is SimpleDialog or AwareDialog or ArgsReceivingDialog;

    public Control? Build(object? data) => new ContentControl();
  }

  // ── Harness ──────────────────────────────────────────────

  private static (RealShell Shell, StagePanel Panel, IServiceProvider Sp) CreateShell()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>(template: new ViewLocator());
    return (shell, shell.Panel, shell.Services);
  }

  // ── ShowAsync builds its own chain — [dimmer, session] comes from the extension; the engine takes no part ──

  [AvaloniaFact]
  public async Task ShowAsync_PresentsSessionUnderDefaultDimmer()
  {
    var (shell, panel, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new SimpleDialog();

    var showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    // ShowAsync builds the chain [DefaultDimmerModel, session] itself — the
    // overlay hosts the framework dimmer around the session content.
    var host = Assert.Single(panel.DerivedHosts());
    Assert.IsType<DimmerLayout>(Assert.Single(host.Children));

    session.Close("ok");
    await showTask;
    Assert.Empty(panel.DerivedHosts());
  }

  // ── the dialog's own callbacks — the Routing contract's arrival / release (a terminal close runs no departure hook) ──

  [AvaloniaFact]
  public async Task DialogContent_ReceivesArrivalAndRelease_CloseRunsNoDeparture()
  {
    var (shell, panel, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new AwareDialog();

    var showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
    await WaitUntilAsync(() => session.Arrived == 1);

    session.Close("ok");
    await showTask;

    // A close is a terminal ceremony — no departure pair fires (the
    // session implements IDeparted and must not see it at close); the
    // release drain closes it out.
    Assert.Equal(1, session.Arrived);
    Assert.Equal(0, session.Departed);
    Assert.Equal(1, session.Released);
    Assert.Empty(panel.DerivedHosts());
  }

  // ── participant-entry protocol — IParameterized delivery before arrival ──

  [AvaloniaFact]
  public async Task Session_ReceivesDeliver_BeforeArrival()
  {
    var (shell, panel, sp) = CreateShell();
    var dialogs = sp.GetRequiredService<IRouter>();
    var session = new ArgsReceivingDialog();

    var showTask = dialogs.ShowAsync(session);
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
    await WaitUntilAsync(() => session.Arrived == 1);

    // The pipeline drove the delivery before arrival: an empty hand-over
    // (a dialog session is its own parameter — no strong args, no payload).
    Assert.Null(session.Received);

    session.Close("ok");
    await showTask;
    Assert.Empty(panel.DerivedHosts());
  }
}
