using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>A minimal <see cref="LayerHandleBase"/> backed by a plain field.</summary>
internal sealed class TestHandle : LayerHandleBase
{
  private bool _visible = true;   // a mounted surface is visible by default

  internal TestHandle(ILayerLedger ledger, object content, LayerPlane plane, int z)
    : base(ledger, content, plane, z)
  {
  }

  protected override bool IsVisible { get => _visible; set => _visible = value; }
}
