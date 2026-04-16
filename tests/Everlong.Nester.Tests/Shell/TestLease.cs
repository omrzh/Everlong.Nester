using Everlong.Nester.Layer;

namespace Everlong.Nester.Tests.Shell;

/// <summary>A minimal <see cref="LayerLeaseBase"/> backed by plain fields.</summary>
internal sealed class TestLease : LayerLeaseBase
{
  private object? _content;
  private bool _visible = true;   // a mounted surface is visible by default

  internal TestLease(ILayerLedger ledger, int z)
    : base(ledger, z)
  {
  }

  public override object? Content { get => _content; set => _content = value; }

  public override bool IsVisible { get => _visible; set => _visible = value; }
}
