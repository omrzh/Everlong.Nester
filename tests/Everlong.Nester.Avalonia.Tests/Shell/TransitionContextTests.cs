using Avalonia.Controls;
using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The director working surface — the moving heads and the head-only
///   visibility levers.
/// </summary>
public sealed class TransitionContextTests
{
  private sealed class PlainView : ContentControl;

  [Fact]
  public void ShowArriving_RevealsOnlyTheArrivingHead()
  {
    var arriving = new PlainView { Opacity = 0, IsHitTestVisible = false };
    var departing = new PlainView { Opacity = 0, IsHitTestVisible = false };
    var context = new TransitionContext(null, TransitionKind.Enter)
    {
      Arriving = arriving,
      Departing = departing,
    };

    context.ShowArriving();

    Assert.Equal(1, arriving.Opacity);
    Assert.True(arriving.IsHitTestVisible);
    Assert.Equal(0, departing.Opacity);          // the departing head is untouched
    Assert.False(departing.IsHitTestVisible);
  }

  [Fact]
  public void HideDeparting_HidesOnlyTheDepartingHead()
  {
    var arriving = new PlainView();
    var departing = new PlainView();
    var context = new TransitionContext(null, TransitionKind.Exit)
    {
      Arriving = arriving,
      Departing = departing,
    };

    context.HideDeparting();

    Assert.Equal(0, departing.Opacity);
    Assert.False(departing.IsHitTestVisible);
    Assert.Equal(1, arriving.Opacity);           // the arriving head is untouched
    Assert.True(arriving.IsHitTestVisible);
  }

  [Fact]
  public void ShowArriving_WithoutAnArrivingHead_DoesNothing()
  {
    var context = new TransitionContext(null, TransitionKind.Dismiss)
    {
      Departing = new PlainView { Opacity = 0 },
    };

    context.ShowArriving();

    Assert.Null(context.Arriving);
  }

  [Fact]
  public void HideDeparting_WithoutADepartingHead_DoesNothing()
  {
    var context = new TransitionContext(null, TransitionKind.Enter)
    {
      Arriving = new PlainView(),
    };

    context.HideDeparting();

    Assert.Null(context.Departing);
  }
}
