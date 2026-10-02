using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

/// <summary>
///   The flying plane's surface: the surface drops the children the view
///   layer put on it when its lease is reclaimed and when it is cleared.
/// </summary>
[Collection("RealShell")]
public class FlyingCanvasTests
{
  [AvaloniaFact]
  public async Task LeaseReclaim_ClearsTheChildren()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    var flying = shell.Services.GetRequiredService<IFlyingLayer>();
    flying.Canvas.Children.Add(new Border());

    await shell.Shell.DisposeAsync();

    Assert.Empty(flying.Canvas.Children);
  }

  [AvaloniaFact]
  public void Clear_DropsTheChildren()
  {
    var canvas = new FlyingCanvas();
    canvas.Children.Add(new Border());

    canvas.Clear();

    Assert.Empty(canvas.Children);
  }
}
