using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Layer;

/// <summary>A minimal <see cref="LayerLeaseBase"/> backed by plain fields.</summary>
internal sealed class TestLease : LayerLeaseBase
{
  private object? _content;
  private bool _visible = true;   // a mounted surface is visible by default

  internal TestLease(ILayerLedger ledger, LayerPlane plane, int z)
    : base(ledger, plane, z)
  {
  }

  public override object? Content { get => _content; set => _content = value; }

  public override bool IsVisible { get => _visible; set => _visible = value; }
}
