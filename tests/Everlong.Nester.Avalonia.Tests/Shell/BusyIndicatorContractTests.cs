using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Everlong.Nester.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Helpers;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The busy-indicator contract, headless: the generated ViewLocator
///   resolves <see cref="BusyProbeViewModel"/> into <see cref="ProbeView"/>,
///   and <see cref="ContentLayer.Visible"/> reaches the view's ProgressBar.
/// </summary>
public class BusyIndicatorContractTests
{
  [AvaloniaFact]
  public async Task ViewLocator_ResolvesBusyIndicator_AndVisibilityFollowsLayer()
  {
    var locator = new TestViewLocator();
    var vm = new BusyProbeViewModel();

    // The generated mapping must resolve the VM into the probe view.
    Assert.True(locator.Match(vm));
    Assert.IsType<ProbeView>(locator.Build(vm));

    // The lease carrier: a real ContentLayer. Content = the busy VM, located
    // via the generated mapping hanging off the carrier's own DataTemplates.
    var layer = new ContentLayer { Content = vm };
    layer.DataTemplates.Add(locator);

    var window = new Window { Content = layer, Width = 800, Height = 600 };
    window.Show();

    try
    {
      await UIDispatcher.WaitForLoadedAsync();

      // The generated mapping must have kicked in: the visual tree carries a ProgressBar.
      var bar = window.GetVisualDescendants().OfType<ProgressBar>().FirstOrDefault()
                ?? throw new InvalidOperationException(
                  "No ProgressBar under the content slot — the ViewLocator mapping did not resolve.");
      Assert.True(bar.IsEffectivelyVisible);

      // The presentation lever on the carrier reaches the view.
      layer.IsVisible = false;
      Dispatcher.UIThread.RunJobs();
      Assert.False(bar.IsEffectivelyVisible);

      layer.IsVisible = true;
      Dispatcher.UIThread.RunJobs();
      Assert.True(bar.IsEffectivelyVisible);
    }
    finally
    {
      window.Close();
    }
  }
}

/// <summary>
///   The probe view for <see cref="BusyProbeViewModel"/> — a pure-C#
///   stand-in for a busy indicator (no XAML compilation).
/// </summary>
[ViewFor<BusyProbeViewModel>]
internal sealed class ProbeView : UserControl
{
  public ProbeView()
  {
    Content = new ProgressBar { IsIndeterminate = true, Height = 3 };
  }
}

/// <summary>The probe view model — a local stand-in for the template's busy indicator.</summary>
internal sealed class BusyProbeViewModel;
