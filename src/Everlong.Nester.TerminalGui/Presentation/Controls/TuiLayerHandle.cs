using Everlong.Nester.Layer;
using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The platform handle — <see cref="LayerHandleBase" /> over the concrete
///   <see cref="TuiLayer" /> surface.
/// </summary>
internal sealed class TuiLayerHandle : LayerHandleBase
{
  internal TuiLayerHandle(TuiLayer surface, ILayerLedger ledger, LayerPlane plane, int z)
    : base(ledger, plane, z)
  {
    Surface = surface;
  }

  /// <summary>The mounted surface.</summary>
  internal TuiLayer Surface { get; }

  /// <inheritdoc />
  protected override object? Content
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
  protected override bool IsVisible { get => Surface.Visible; set => Surface.Visible = value; }
}
