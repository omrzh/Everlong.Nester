using Avalonia.Headless.XUnit;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Tests.Routing;

/// <summary>A no-op director for real-shell assembly.</summary>
internal sealed class NoopDirector : IShellDirector
{
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);

  public void OnAssembled(IShell shell) { }
  public bool HandleError(Exception exception) => true;
}

/// <summary>
///   Routes on a REAL <see cref="AvaloniaShell"/> (headless): the router rents
///   a band from the shell's broker, and a <see cref="BackIntent"/> dispatched
///   through the shell reaches the presented overlay and closes it.
/// </summary>
[Collection("RealShell")]
public class RouterOnRealShellTests
{
  [AvaloniaFact]
  public async Task Router_RoutesAndPresents_OnRealShell_AndBackCloses()
  {
    var shell = TestHost.CreateShell<NoopDirector>(s =>
    {
      s.AddSingleton<TestContent>(_ => new TestContent());
    });
    shell.Start();

    var router = new Router(shell.Services);
    await router.RouteAsync(new Request(typeof(TestContent), null));

    object ground = router.Model!.Current!;
    Assert.Same(ground, router.View.Location!.Instance);

    IRouter present = router.Derive();
    await present.RouteAsync(new Request(typeof(TestContent), null));

    IntentResult handled = await shell.DispatchIntent(null, new BackIntent("ok"));
    Assert.Equal(IntentResult.Handled, handled);
    Assert.Equal("ok", await present.Completion!);
  }
}
