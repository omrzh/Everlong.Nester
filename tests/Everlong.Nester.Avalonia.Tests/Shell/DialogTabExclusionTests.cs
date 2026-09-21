using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Everlong.Nester.Controls;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

using static Everlong.Nester.Tests.AsyncTestHelpers;

/// <summary>
///   The stage's stacked Tab exclusion: while a dialog overlay is mounted, the
///   routing layer beneath it (the ground) is excluded from Tab navigation —
///   the first Tab jumps straight into the overlay and stays there.  The
///   dialog takes no focus at reveal (no focus ring), and closing it restores
///   the ground's reachability.
/// </summary>
[Collection("RealShell")]
public sealed class DialogTabExclusionTests
{
  /// <summary>A real window host — the desktop protocol's stage mounts into it.</summary>
  private sealed class HostWindow : Window, IAvaloniaShellHost
  {
    public void HostShell(IAvaloniaShell shell, Control stage) => Content = stage;
  }

  private sealed class GroundVm
  {
  }

  /// <summary>The ground page — two focusable buttons behind the dialog.</summary>
  private sealed class GroundPage : StackPanel
  {
    internal Button First { get; } = new() { Content = "Ground 1" };
    internal Button Second { get; } = new() { Content = "Ground 2" };

    public GroundPage()
    {
      Children.Add(First);
      Children.Add(Second);
    }
  }

  private sealed class GroundTemplate : IDataTemplate
  {
    public Control? Build(object? param) => param is GroundVm ? new GroundPage() : null;

    public bool Match(object? data) => data is GroundVm;
  }

  private static async Task<(HostWindow Window, StagePanel Panel, IServiceProvider Sp)> CreateRig()
  {
    var window = new HostWindow { Width = 900, Height = 600 };
    RealShell real = RealShell.Create<RealShell.RealTestContext>(
      template: new GroundTemplate(), rootView: window);
    return (window, real.Panel, real.Services);
  }

  private static RoutingView? TryGroundHost(StagePanel panel)
    => panel.Children.OfType<ContentLayer>()
        .Select(cc => cc.Content)
        .OfType<RoutingView>()
        .FirstOrDefault(h => h.Location?.Instance is GroundVm);

  private static async Task<Button> WaitForButtonAsync(Control root, string content)
  {
    await WaitUntilAsync(() =>
    {
      Button? found = root.GetVisualDescendants().OfType<Button>()
        .FirstOrDefault(b => b.Content?.ToString() == content);
      return found is not null && found.IsAttachedToVisualTree();
    });
    return root.GetVisualDescendants().OfType<Button>().First(b => b.Content?.ToString() == content);
  }

  private static bool IsWithin(Visual? descendant, Visual ancestor)
    => descendant is not null && (ReferenceEquals(descendant, ancestor) || ancestor.IsVisualAncestorOf(descendant));

  // ── opening a dialog does not steal focus; the ground is excluded, so the first Tab enters the overlay and stays ──

  [AvaloniaFact]
  public async Task DialogOpen_GroundExcluded_FirstTabEntersTheCapsule()
  {
    var (window, panel, sp) = await CreateRig();
    var router = sp.GetRequiredService<IRouter>();

    try
    {
      // The ground page is routed first — two focusable buttons behind the dialog.
      await router.RouteAsync(new Locator([Target.Of(typeof(GroundVm), instance: new GroundVm())]));
      await WaitUntilAsync(() => TryGroundHost(panel) is not null);
      Button ground1 = await WaitForButtonAsync(window, "Ground 1");

      // The user was interacting with the ground page — focus is on its button.
      ground1.Focus();
      Assert.Same(ground1, window.FocusManager?.GetFocusedElement());

      // A real alert overlay reveals above the ground (its default view carries an OK button).
      Task<bool> alert = router.AlertAsync("message", "title", "OK");
      await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
      RoutingView overlay = panel.DerivedHosts().Single();
      await WaitForButtonAsync(window, "OK");

      // The dialog takes no focus at reveal — no focus ring on its button.
      Assert.Same(ground1, window.FocusManager?.GetFocusedElement());

      // The covered ground is excluded from Tab: the first Tab lands in the overlay.
      window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
      Control? focused = window.FocusManager?.GetFocusedElement() as Control;
      Assert.True(IsWithin(focused, overlay),
        $"第一次 Tab 应直接进入胶囊,实际: {focused?.GetType().Name}");

      // Once inside, Tab stays inside the overlay.
      for (int i = 0; i < 6; i++)
      {
        window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        focused = window.FocusManager?.GetFocusedElement() as Control;
        Assert.True(IsWithin(focused, overlay), $"Tab #{i + 2} must stay inside the overlay");
      }

      // Close the overlay (Enter on the focused OK) — teardown hygiene.
      window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
      await alert.WaitAsync(TimeSpan.FromSeconds(5));
      await WaitUntilAsync(() => panel.DerivedHosts().Count() == 0);
    }
    finally
    {
      window.Close();
    }
  }

  // ── with the overlay closed, the ground regains Tab reachability ──

  [AvaloniaFact]
  public async Task DialogClosed_GroundTabbableAgain()
  {
    var (window, panel, sp) = await CreateRig();
    var router = sp.GetRequiredService<IRouter>();

    try
    {
      await router.RouteAsync(new Locator([Target.Of(typeof(GroundVm), instance: new GroundVm())]));
      await WaitUntilAsync(() => TryGroundHost(panel) is not null);
      RoutingView ground = TryGroundHost(panel)!;
      Button ground1 = await WaitForButtonAsync(window, "Ground 1");
      Button ground2 = await WaitForButtonAsync(window, "Ground 2");

      Task<bool> alert = router.AlertAsync("message", "title", "OK");
      await WaitUntilAsync(() => panel.DerivedHosts().Count() == 1);
      RoutingView overlay = panel.DerivedHosts().Single();
      await WaitForButtonAsync(window, "OK");

      // Enter the overlay (first Tab), then close it with Enter on the OK.
      window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
      window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
      await alert.WaitAsync(TimeSpan.FromSeconds(5));
      await WaitUntilAsync(() => panel.DerivedHosts().Count() == 0);

      // The ground layer is top again — its content is tab-reachable.
      ground1.Focus();
      Assert.Same(ground1, window.FocusManager?.GetFocusedElement());
      window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
      Assert.Same(ground2, window.FocusManager?.GetFocusedElement());
      Assert.True(ground.IsVisualAncestorOf(ground2));
    }
    finally
    {
      window.Close();
    }
  }
}
