using System.ComponentModel;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Layer;

/// <summary>
///   The lease skeleton: the slot members are abstract; liveness and the
///   exit path are shared.
/// </summary>
/// <remarks>Creates the lease over its ledger channel at the granted z.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class LayerLeaseBase(ILayerLedger ledger, int z) : ILayerLease
{
  /// <inheritdoc />
  public bool IsActive => ledger.IsLive(this);

  /// <inheritdoc />
  public int Z { get; } = z;

  /// <inheritdoc />
  public IIntentHandler IntentHandler { get; set; } = NoOpIntentHandler.Instance;

  /// <inheritdoc />
  public abstract object? Content { get; set; }

  /// <inheritdoc />
  public abstract bool IsVisible { get; set; }

  /// <inheritdoc />
  public void Release() => ledger.Drop(this);
}
