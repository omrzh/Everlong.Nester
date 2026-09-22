using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

/// <summary>
///   The flying plane's carrier slot: a control anywhere under the stage
///   parks a value on the window's plane, and the surface drops both the
///   value and the children when its lease is reclaimed.
/// </summary>
[Collection("RealShell")]
public class FlyingCanvasTests
{
  [AvaloniaFact]
  public async Task Anchor_IsReachableFromAnyControlUnderTheStage()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var probe = new Border();
      shell.Panel.Children.Add(probe);

      probe.GetFlyingCanvas()!.Anchor = "the view layer's hand-off";

      Assert.Equal("the view layer's hand-off", shell.Panel.FlyingCanvas!.Anchor);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public async Task LeaseReclaim_ClearsTheAnchorAndTheChildren()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    var flying = shell.Services.GetRequiredService<IFlyingLayer>();
    flying.Canvas.Anchor = new object();
    flying.Canvas.Children.Add(new Border());

    await shell.Shell.DisposeAsync();

    Assert.Null(flying.Canvas.Anchor);
    Assert.Empty(flying.Canvas.Children);
  }

  [AvaloniaFact]
  public void Clear_DropsTheAnchorAndTheChildren()
  {
    var canvas = new FlyingCanvas();
    canvas.Anchor = new object();
    canvas.Children.Add(new Border());

    canvas.Clear();

    Assert.Null(canvas.Anchor);
    Assert.Empty(canvas.Children);
  }
}
