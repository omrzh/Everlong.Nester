using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
namespace Everlong.Nester.Tests.Routing;

/// <summary>
///   The Routing domain on the real shell with real views — the platform
///   assembly fills the nodes' views and bodies, and the reveal mounts and
///   switches the chain per body.
/// </summary>
[Collection("RealShell")]
public class RouterPlatformTests
{
  private sealed class LayoutView : ContentControl, IBodyHolder
  {
    private readonly BodyPanel _body = new();

    internal LayoutView()
    {
      Content = _body;
      Width = 100;
      Height = 100;
    }

    public IBodyPanel GetBodyPanel() => _body;
  }

  private sealed class PageView : ContentControl { }

  private sealed class ViewTemplate : IDataTemplate
  {
    private readonly Type _type;
    private readonly Func<Control> _factory;

    internal ViewTemplate(Type type, Func<Control> factory)
    {
      _type = type;
      _factory = factory;
    }

    public bool Match(object? data) => data?.GetType() == _type;

    public Control? Build(object? param) => _factory();
  }

  private static (AvaloniaShell shell, Router router) Create()
  {
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(LayoutAlpha), () => new LayoutView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(PageAlpha), () => new PageView()));
    Application.Current!.DataTemplates.Add(new ViewTemplate(typeof(PageBeta), () => new PageView()));

    var shell = TestHost.CreateShell<NoopDirector>(s =>
    {
      s.AddSingleton<LayoutAlpha>(_ => new LayoutAlpha());
      s.AddSingleton<PageAlpha>(_ => new PageAlpha());
      s.AddSingleton<PageBeta>(_ => new PageBeta());
    });
    shell.Start();
    return (shell, new Router(shell.Services));
  }

  private static Request Chain(Type layout, Type page)
    => new(page, null, [Target.Of(layout), Target.Of(page)]);

  [AvaloniaFact]
  public async Task RouteAsync_AssemblesAndRevealsTheChain_InRealViews()
  {
    (AvaloniaShell shell, Router router) = Create();
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    var hostBody = (IBodyPanel<Control>)router.View;
    var layoutNode = Assert.Single(hostBody.Children);
    var layoutView = Assert.IsType<LayoutView>(layoutNode.View);
    var bodyPanel = (IBodyPanel<Control>)layoutView.GetBodyPanel();
    var pageNode = Assert.Single(bodyPanel.Children);
    Assert.IsType<PageView>(pageNode.View);

    Assert.Same(layoutNode, hostBody.ActiveChild);
    Assert.Same(pageNode, bodyPanel.ActiveChild);
    Assert.True(layoutView.IsVisible);
    Assert.True(((PageView)pageNode.View!).IsVisible);
  }

  [AvaloniaFact]
  public async Task RouteAsync_GroundLayerMountsIntoTheStagePanel()
  {
    (AvaloniaShell shell, Router router) = Create();
    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));

    // The router's overlay ledger connects to the shell's stage: the ground
    // layer must be physically mounted into the stage panel — not just held
    // in the router's model (a missing ledger→stage connection leaves the
    // presented chain invisible).
    var panel = Assert.IsType<StagePanel>(shell.StagePanel);
    var groundLayer = Assert.Single(
      panel.Children.OfType<ContentControl>(),
      c => ReferenceEquals(c.Content, router.View));
    Assert.Equal(KnownLayers.Navigation.Floor, groundLayer.ZIndex);
  }

  [AvaloniaFact]
  public void Derive_LeasesTheBandItsLifetimeImplies()
  {
    (AvaloniaShell shell, Router router) = Create();
    var panel = Assert.IsType<StagePanel>(shell.StagePanel);

    // A full navigator — a derived router that pushes and traverses — shares
    // the ground router's navigation band, one slot above its floor.
    var navigator = (Router)router.Derive();
    var navigatorLayer = Assert.Single(panel.Children.OfType<ContentControl>(),
      c => ReferenceEquals(c.Content, navigator.View));
    Assert.True(navigatorLayer.ZIndex > KnownLayers.Navigation.Floor);
    Assert.True(KnownLayers.Navigation.Contains(navigatorLayer.ZIndex));

    // A one-shot dialog rents the dialog band, above every navigator.
    var oneShot = (Router)router.Derive(isEphemeral: true);
    var dialogLayer = Assert.Single(panel.Children.OfType<ContentControl>(),
      c => ReferenceEquals(c.Content, oneShot.View));
    Assert.True(KnownLayers.Dialog.Contains(dialogLayer.ZIndex));
  }

  [AvaloniaFact]
  public async Task Back_RestoresTheHiddenChain_AndHidesTheCurrent()
  {
    (AvaloniaShell shell, Router router) = Create();

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageAlpha)));
    Assert.Equal(1, router.Stack.Count);
    var hostBody = (IBodyPanel<Control>)router.View;
    var bodyPanel = (IBodyPanel<Control>)((LayoutView)hostBody.Children[0].View!).GetBodyPanel();
    var pageA = (PageView)bodyPanel.Children[0].View!;

    await router.RouteAsync(Chain(typeof(LayoutAlpha), typeof(PageBeta)));
    var pageB = Assert.Single(bodyPanel.Children, n => !ReferenceEquals(n.View, pageA)).View!;
    Assert.False(pageA.IsVisible);
    Assert.True(((PageView)pageB).IsVisible);

    Assert.Equal(2, router.Stack.Count);
    Assert.True(router.Model.CanGoBack, "stack should hold the first visit");
    Assert.Equal(IntentResult.Handled, await shell.DispatchIntent(null, new BackIntent()));
    Assert.True(pageA.IsVisible);
    Assert.False(((PageView)pageB).IsVisible);
    Assert.Same(bodyPanel.Children.First(n => ReferenceEquals(n.View, pageA)), bodyPanel.ActiveChild);
  }
}
