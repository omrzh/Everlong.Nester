using Everlong.Nester.ComponentModel;
using Everlong.Nester.Routing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Helpers;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Tests.Routing;
using Everlong.Nester.Tests.Hosting;

namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   The dimmer chrome: the framework locator translates it like any other
///   view, and clicking its backdrop dispatches the routing BackIntent —
///   the derived layer (leaf without a guard) closes itself.
/// </summary>
[Collection("RealShell")]
public class DimmerDefaultShellTests
{
  public sealed class TestDialogSession : DialogSessionBase<object?>
  {
  }

  private sealed class FakeViewLocator : IDataTemplate
  {
    public bool Match(object? data) => data is TestDialogSession;

    public Control? Build(object? data)
      => data is null ? null : new ContentControl();
  }

  // ── the Extensions locator translates the dimmer like any other view ──

  [Fact]
  public void ExtendedViewLocator_TranslatesDimmer()
  {
    var locator = new NesterExtendedViewLocator();

    Assert.True(locator.Match(new DefaultDimmerModel()));
    Assert.IsType<DimmerLayout>(locator.Build(new DefaultDimmerModel()));

    // User types are not the framework locators' business.
    Assert.False(locator.Match(new TestDialogSession()));
    Assert.Null(locator.Build(new object()));
  }

  // ── clicking the dimmer backdrop dispatches BackIntent ──

  [AvaloniaFact]
  public async Task ClickingDimmerBackdrop_DispatchesBackIntent()
  {
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
    Application.Current!.DataTemplates.Add(new FakeViewLocator());

    var shell = TestHost.CreateShell<NoopDirector>(s => s.AddTransient<TestDialogSession>());
    shell.Start();
    var panel = shell.StagePanel!;
    // The stage owns its shell from PrepareStage — the dimmer resolves it by
    // crawling the panel; no property broadcast is involved.
    Assert.Same(shell, ((IShellStage)panel).Shell);

    // Present a derived layer [dimmer, session] through the routing domain —
    // the session rides its chain node.
    var router = ((IShell)shell).Services!.GetRequiredService<IRouter>();
    var session = new TestDialogSession();
    IRouter present = router.Derive();
    await present.RouteAsync(new Locator(
      [
        Target.Of(typeof(DefaultDimmerModel)),
        Target.Of(typeof(TestDialogSession), instance: session)
      ]));
    await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);

    var host = Assert.Single(panel.DerivedHosts());
    var dimmer = Assert.IsType<DimmerLayout>(Assert.Single(host.Children));
    var window = new Window { Content = panel, Width = 800, Height = 600 };
    window.Show();
    await UIDispatcher.WaitForLoadedAsync();

    try
    {
      // Click anywhere on the backdrop (the dialog page is backgroundless, so
      // the hit lands on the dimmer border and raises Tapped → BackIntent).
      window.MouseDown(new Point(400, 300), MouseButton.Left);
      window.MouseUp(new Point(400, 300), MouseButton.Left);

      // The dimmer tap dispatches the routing BackIntent — the derived layer
      // (its leaf has no guard) closes itself; the presentation settles.
      await present.Completion!.Result.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.Empty(panel.DerivedHosts());
    }
    finally
    {
      window.Close();
    }
  }
}
