using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Dialog;
using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   Cross-layer transitions (headless): a derived overlay's director is told
///   the transition crosses layers and is handed the source layer's lease,
///   whose content carries the interaction origin it held when it lost the
///   foreground — until it takes the foreground back.
/// </summary>
[Collection("RealShell")]
public sealed class CrossLayerTransitionTests
{
  private sealed class HostWindow : Window, IAvaloniaShellHost
  {
    public void HostShell(IAvaloniaShell shell, Control stage) => Content = stage;
  }

  private sealed class GroundVm
  {
  }

  private sealed class GroundPage : StackPanel
  {
    internal Button First { get; } = new() { Content = "Ground 1" };

    public GroundPage() => Children.Add(First);
  }

  public sealed class AnchorProbeDialog : DialogSessionBase<object?>
  {
  }

  private sealed class AnchorProbeView : ContentControl, ISceneTransition
  {
    internal TransitionContext? EnterContext { get; private set; }

    public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    {
      EnterContext = context;
      return Task.CompletedTask;
    }

    public Task AnimateExitAsync(TransitionContext context, CancellationToken token) => Task.CompletedTask;
  }

  private sealed class ProbeTemplate(GroundPage ground) : IDataTemplate
  {
    public Control? Build(object? param) => param switch
    {
      GroundVm => ground,
      AnchorProbeDialog => new AnchorProbeView(),
      _ => null,
    };

    public bool Match(object? data) => data is GroundVm or AnchorProbeDialog;
  }

  [Fact]
  public void InLayer_Transition_HasNoCounterpart()
  {
    var context = new TransitionContext(null, TransitionKind.Enter);

    Assert.Equal(TransitionScope.InLayer, context.Scope);
    Assert.Null(context.Counterpart);
  }

  [AvaloniaFact]
  public async Task OverlayDirector_SeesTheCrossLayerOriginAndItsSource()
  {
    var ground = new GroundPage();
    var window = new HostWindow { Width = 900, Height = 600 };
    var shell = RealShell.Create<RealShell.RealTestContext>(
      template: new ProbeTemplate(ground), rootView: window);
    var router = shell.Services.GetRequiredService<IRouter>();

    try
    {
      await router.RouteAsync(new Locator([Target.Of(typeof(GroundVm), instance: new GroundVm())]));
      await WaitUntilAsync(() => ground.First.IsAttachedToVisualTree());

      // The user interacts with the ground — element focus rests on its button.
      Assert.True(ground.First.Focus());

      var session = new AnchorProbeDialog();
      Task show = router.ShowAsync(session);

      AnchorProbeView? probe = null;
      await WaitUntilAsync(() =>
      {
        probe = window.GetVisualDescendants().OfType<AnchorProbeView>().FirstOrDefault();
        return probe?.EnterContext is not null;
      });

      TransitionContext enter = probe!.EnterContext!;
      Assert.Equal(TransitionScope.CrossLayer, enter.Scope);
      ILayerLease source = Assert.IsAssignableFrom<ILayerLease>(enter.Counterpart);
      Assert.True(source.IsLive);

      // The source layer's own anchor is the element the foreground last rested on.
      var origin = Assert.IsAssignableFrom<IFocusAnchor>(source.Content);
      Assert.Same(ground.First, origin.Anchor);

      // Closing the overlay returns the foreground and spends the origin.
      session.Close();
      await show.WaitAsync(TimeSpan.FromSeconds(5));
      Assert.Null(origin.Anchor);
    }
    finally
    {
      window.Close();
    }
  }
}
