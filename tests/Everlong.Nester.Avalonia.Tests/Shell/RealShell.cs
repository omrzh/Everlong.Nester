using Avalonia;
using Xunit;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Everlong.DI;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Intent;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp;

using Everlong.Nester.Tests.Hosting;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Serializes every RealShell user: Application.Current.DataTemplates is a
///   process-wide collection — parallel classes would race on template order.
/// </summary>
[CollectionDefinition("RealShell")]
public sealed class RealShellCollection;

/// <summary>
///   The REAL shell harness — every shell-level test gets a true base
///   environment: the real Build&lt;TDirector&gt; flow (BuildProvider →
///   ResolveDirector → BuildStage) over the real registration surface
///   (AddNester + TestAppServices.AddWindowAuth + the [ServiceRegistrar]
///   AppServices).  Only the visual stack is fake (one DataTemplate →
///   tagged ContentControl).  This replaces the hand-assembled
///   TestShellFactory path test by test.
/// </summary>
internal sealed record RealShell(
  AvaloniaShell Broker,
  StagePanel Panel,
  IServiceProvider Services,
  IServiceProvider Root,
  AvaloniaShell Shell)
{
  /// <summary>Lightweight decision maker for tests that don't need the real Director.</summary>
  internal sealed class RealTestContext : IShellDirector
  {

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Fake visual stack: every view model maps to a host view (the desktop protocol: the Director view must BE the host).</summary>
  private sealed class FakeTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new TestHostView { Tag = param.GetType() };

    // Every test view model maps to a host view — the harness serves
    // custom page VMs (PageVm & co.), not just the real app types.
    public bool Match(object? data) => true;
  }

  public static RealShell Create<TDirector>(Action<IServiceCollection>? register = null,
                                           IDataTemplate? template = null,
                                           ContentControl? rootView = null)
    where TDirector : class, IShellDirector, new()
  {
    // Rebuild the chain deterministically (serialized by the RealShell
    // collection).  The DataTemplates collection is read FORWARDS (first
    // wins), so the catch-all fake must be LAST: framework views → test template →
    // fake, i.e. index 0 is checked first.
    // The extension framework views — notices, dialog sessions and the
    // dimmer chrome — all live in the Extensions platform packages; their
    // default locator sits at the same precedence the platform-core
    // built-in locator holds.
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
    if (template is not null)
      Application.Current!.DataTemplates.Add(template);
    Application.Current!.DataTemplates.Add(new FakeTemplate());

    AvaloniaShell shell = TestHost.CreateShell<TDirector>(s =>
    {
      TestAppServices.AddWindowAuth(s);
      s.AddServices(new AppServices());
      register?.Invoke(s);
    }, out var root, rootView: rootView);
    shell.Start();   // the harness keeps the real window semantics — shown, never promoted (fire-and-forget: the harness drives its own lifecycle)
    IServiceProvider services = ((IShell)shell).Services!;
    return new RealShell(shell,
                         shell.StagePanel!, services, root, shell);
  }

  /// <summary>The resolved Director (the shell's decision maker).</summary>
  public object Director => Shell.Director!;

  /// <summary>Live leases in intent-dispatch order (test/observer channel, shell level).</summary>
  public IEnumerable<ILayerLease> LeaseOrder()
    => Broker.LeaseOrder();

  /// <summary>The MS.DI root behind the shell scope (test/observer channel).</summary>
  public IServiceProvider RootProvider => Root;
}
