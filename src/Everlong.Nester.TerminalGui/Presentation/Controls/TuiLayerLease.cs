using Everlong.Nester.Layer;
using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The platform lease — <see cref="LayerLeaseBase" /> over the concrete
///   <see cref="TuiLayer" /> surface.
/// </summary>
internal sealed class TuiLayerLease : LayerLeaseBase
{
  internal TuiLayerLease(TuiLayer surface, ILayerLedger ledger, int z)
    : base(ledger, z)
  {
    Surface = surface;
  }

  /// <summary>The mounted surface.</summary>
  internal TuiLayer Surface { get; }

  /// <inheritdoc />
  public override object? Content
  {
    get => Surface.Content;
    set => Surface.Content = value switch
    {
      null => null,
      View view => view,
      _ => throw new ArgumentException($"A Terminal.Gui layer renders a {nameof(View)}.", nameof(value)),
    };
  }

  /// <inheritdoc />
  public override bool IsVisible { get => Surface.Visible; set => Surface.Visible = value; }
}
