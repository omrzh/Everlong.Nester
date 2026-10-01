using System.ComponentModel;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Layer;

/// <summary>
///   The handle skeleton: the slot members are abstract; the read-only view,
///   content, liveness and the exit path are shared.
/// </summary>
/// <remarks>Creates the handle over its ledger channel at the granted plane and z, holding the granted content.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class LayerHandleBase : ILayerHandle
{
  private readonly ILayerLedger _ledger;
  private readonly object _content;
  private readonly LeaseView _lease;

  /// <summary>Creates the handle over its ledger channel at the granted plane and z, holding the granted content.</summary>
  protected LayerHandleBase(ILayerLedger ledger, object content, LayerPlane plane, int z)
  {
    _ledger = ledger;
    _content = content;
    _lease = new LeaseView(this, ledger, plane, z);
  }

  /// <inheritdoc />
  public ILayerLease Lease => _lease;

  /// <summary>The handler this lease presents to an intent dispatch.</summary>
  protected IIntentHandler Handler { get; set; } = NoOpIntentHandler.Instance;

  /// <summary>The content granted with the lease — fixed for its life.</summary>
  protected object Content => _content;

  /// <summary>Whether the slot is displayed.</summary>
  protected abstract bool IsVisible { get; set; }

  /// <inheritdoc />
  public void SetVisible(bool visible) => IsVisible = visible;

  /// <inheritdoc />
  public void SetIntentHandler(IIntentHandler handler) => Handler = handler;

  /// <inheritdoc />
  public void Release() => _ledger.Drop(_lease);

  /// <summary>The read-only projection an observer receives — it holds no writable member and no path back to the handle.</summary>
  private sealed class LeaseView(LayerHandleBase owner, ILayerLedger ledger, LayerPlane plane, int z) : ILayerLease
  {
    /// <inheritdoc />
    public bool IsLive => ledger.IsLive(this);

    /// <inheritdoc />
    public int Z => z;

    /// <inheritdoc />
    public LayerPlane Plane => plane;

    /// <inheritdoc />
    public object Content => owner.Content;

    /// <inheritdoc />
    public bool IsVisible => owner.IsVisible;

    /// <inheritdoc />
    public IIntentHandler IntentHandler => owner.Handler;
  }
}
