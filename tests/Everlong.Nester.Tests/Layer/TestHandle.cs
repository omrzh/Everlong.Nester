using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>A minimal <see cref="LayerHandleBase"/> backed by plain fields.</summary>
internal sealed class TestHandle : LayerHandleBase
{
  private object? _content;
  private bool _visible = true;   // a mounted surface is visible by default

  internal TestHandle(ILayerLedger ledger, LayerPlane plane, int z)
    : base(ledger, plane, z)
  {
  }

  protected override object? Content { get => _content; set => _content = value; }

  protected override bool IsVisible { get => _visible; set => _visible = value; }
}
