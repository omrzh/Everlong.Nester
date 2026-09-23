using Everlong.Nester.Layer;
using Terminal.Gui.Views;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The stage — the root surface that hosts every lease's
///   <see cref="TuiLayer" />, stacked by z (the highest z draws on top).
/// </summary>
internal sealed class TuiStage : Runnable, ILayerStage
{
  private readonly List<TuiLayerLease> _leases = [];

  internal TuiStage()
  {
    Title = "Nester";
  }

  /// <summary>Mounts a tenant's lease surface onto the stage.</summary>
  public void MountLease(ILayerLease lease)
  {
    if (lease is not TuiLayerLease layerLease)
      throw new InvalidOperationException("The stage only mounts Terminal.Gui leases.");
    _leases.Add(layerLease);
    Rebuild();
  }

  /// <summary>Detaches a tenant's lease surface from the stage.</summary>
  public void UnmountLease(ILayerLease lease)
  {
    if (lease is not TuiLayerLease layerLease)
      return;
    _leases.Remove(layerLease);
    Rebuild();
  }

  /// <summary>
  ///   Rebuilds the child order — z ascending, the highest z last (later
  ///   SubViews draw on top in v2.4.17's renderer).
  /// </summary>
  private void Rebuild()
  {
    var ordered = _leases
      .OrderBy(l => l.Z)
      .ThenBy(l => _leases.IndexOf(l))
      .Select(l => l.Surface)
      .ToList();

    RemoveAll();
    foreach (var surface in ordered)
      Add(surface);
  }
}
