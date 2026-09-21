using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Everlong.Nester.Controls;
using Everlong.Nester.Helpers;
using Everlong.Nester.Presentation;
using Everlong.Nester.Layer;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Everlong.Nester.Tests.Layer;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The stage's lease materialization (layer protocol v3): every lease's
///   content slot mounts directly on the root grid with its own ZIndex (the
///   lease's z); same-z slots stack by insertion order; the flying canvas
///   sits at int.MaxValue so transition ghosts render above everything.
/// </summary>
public class LayerStageTests
{
  private static StagePanel NewPanel() => new();

  /// <summary>A live platform lease over the given slot, positioned at <paramref name="z"/>.</summary>
  private static ContentLayerLease Lease(ContentLayer slot, int z)
    => new(slot, StubLedger.Instance, z);

  /// <summary>A detached ledger channel: every lease stays live, no ledger actions.</summary>
  private sealed class StubLedger : ILayerLedger
  {
    public static readonly StubLedger Instance = new();

    public bool IsLive(ILayerLease lease) => true;

    public void Drop(ILayerLease lease) { }
  }

  [AvaloniaFact]
  public void MountSlot_AddsContentControl_WithItsZIndex()
  {
    var panel = NewPanel();
    var slot = new ContentLayer();
    var lease = Lease(slot, 100);

    panel.MountLease(lease);

    Assert.Same(slot, panel.Children.OfType<ContentControl>().Single());
    Assert.Equal(100, slot.ZIndex);
  }

  [AvaloniaFact]
  public void MountSlot_ArbitraryZ_NoFloorPanels()
  {
    var panel = NewPanel();
    var ground = new ContentLayer();
    var dialog = new ContentLayer();
    var groundLease = Lease(ground, 0);
    var dialogLease = Lease(dialog, 100);

    panel.MountLease(groundLease);
    panel.MountLease(dialogLease);

    // Slots mount directly on the root grid — no intermediate floor panels.
    Assert.Equal(new[] { ground, dialog }, panel.Children.OfType<ContentControl>());
    Assert.Equal(0, ground.ZIndex);
    Assert.Equal(100, dialog.ZIndex);
  }

  [AvaloniaFact]
  public void MountSlot_SameZ_SlotsStackInCutOrder()
  {
    var panel = NewPanel();
    var first = new ContentLayer();
    var second = new ContentLayer();
    var firstLease = Lease(first, 100);
    var secondLease = Lease(second, 100);

    panel.MountLease(firstLease);
    panel.MountLease(secondLease);

    Assert.Equal(new[] { first, second }, panel.Children.OfType<ContentControl>());
  }

  [AvaloniaFact]
  public void UnmountSlot_RemovesFromStage()
  {
    var panel = NewPanel();
    var slot = new ContentLayer();
    var lease = Lease(slot, 100);
    panel.MountLease(lease);

    panel.UnmountLease(lease);

    Assert.Empty(panel.Children.OfType<ContentControl>());
  }

  private static (BareShell Shell, StagePanel Panel) ShellWithPanel()
  {
    var panel = new StagePanel();
    var shell = new BareShell();
    shell.ConnectStage(panel);
    return (shell, panel);
  }

  [AvaloniaFact]
  public void Acquire_MountsTheContent()
  {
    var (shell, panel) = ShellWithPanel();
    var tenant = new LayerTestTenant();
    var content = new object();

    var lease = shell.Acquire(tenant, content, 100);

    var slot = panel.Children.OfType<ContentControl>().Single();
    Assert.Same(content, slot.Content);
    Assert.Equal(100, lease.Z);
    Assert.Equal(100, slot.ZIndex);
  }

  [AvaloniaFact]
  public void Acquire_TopPlacement_ProjectsTheGrantedZIndex()
  {
    var (shell, panel) = ShellWithPanel();
    shell.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    var lease = shell.Acquire(new LayerTestTenant(), new object(), KnownLayers.Dialog, LayerPolicy.AboveHighest);

    var slot = panel.Children.OfType<ContentControl>().Last();
    Assert.Equal(KnownLayers.Dialog.Floor + 1, lease.Z);
    Assert.Equal(KnownLayers.Dialog.Floor + 1, slot.ZIndex);
  }

  [AvaloniaFact]
  public async Task Acquire_ViewModel_ResolvesDataTemplate()
  {
    var (shell, panel) = ShellWithPanel();
    var content = new object();
    shell.Acquire(new LayerTestTenant(), content, 100);

    var slot = panel.Children.OfType<ContentControl>().Single();
    slot.DataTemplates.Add(new FuncDataTemplate<object>((_, _) => new TextBlock { Text = "mapped" }));

    var window = new Window { Content = panel, Width = 200, Height = 200 };
    window.Show();
    await UIDispatcher.WaitForLoadedAsync();

    try
    {
      var text = slot.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault();
      Assert.NotNull(text);
      Assert.Equal("mapped", text!.Text);
    }
    finally
    {
      window.Close();
    }
  }
}

/// <summary>
///   The flying layer contract (2026-09): the window-scoped tenant rents the
///   top band (<see cref="KnownLayers.Flying" />) from the shell; the same
///   plane figure is the stage's <see cref="StagePanel.FlyingCanvas" /> and
///   the container's <see cref="IFlyingLayer" />.
/// </summary>
[Collection("RealShell")]
public class FlyingLayerServiceTests
{
  [AvaloniaFact]
  public async Task ContainerResolvesTheStagePlane()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var flying = shell.Services.GetRequiredService<IFlyingLayer>();
      Assert.NotNull(flying.Canvas);

      // The stage and the container hand out the same plane.
      Assert.Same(flying.Canvas, shell.Panel.FlyingCanvas);

      // It is pulled into the top-band slot mounted on the shell stage.
      var slot = shell.Panel.Children.OfType<ContentControl>()
        .Single(c => KnownLayers.Flying.Contains(ZOrder.Get(c)));
      Assert.Same(flying.Canvas, slot.Content);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public void UnassembledStage_BindsNoPlane()
  {
    // A shell that never assembled has no container — the bind degrades to
    // no plane instead of reading the shell's throwing Services face.
    var shell = new BareShell();
    var panel = new StagePanel();

    shell.ConnectStage(panel);

    Assert.Null(panel.FlyingCanvas);
  }
}
