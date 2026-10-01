using System.ComponentModel;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Layer;

/// <summary>
///   The handle skeleton: the slot members are abstract; the read-only view,
///   liveness and the exit path are shared.
/// </summary>
/// <remarks>Creates the handle over its ledger channel at the granted plane and z.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class LayerHandleBase : ILayerHandle
{
  private readonly ILayerLedger _ledger;
  private readonly LeaseView _lease;

  /// <summary>Creates the handle over its ledger channel at the granted plane and z.</summary>
  protected LayerHandleBase(ILayerLedger ledger, LayerPlane plane, int z)
  {
    _ledger = ledger;
    _lease = new LeaseView(this, ledger, plane, z);
  }

  /// <inheritdoc />
  public ILayerLease Lease => _lease;

  /// <summary>The handler this lease presents to an intent dispatch.</summary>
  protected IIntentHandler Handler { get; set; } = NoOpIntentHandler.Instance;

  /// <summary>The displayed content.</summary>
  protected abstract object? Content { get; set; }

  /// <summary>Whether the slot is displayed.</summary>
  protected abstract bool IsVisible { get; set; }

  /// <inheritdoc />
  public void SetContent(object? content) => Content = content;

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
    public object? Content => owner.Content;

    /// <inheritdoc />
    public bool IsVisible => owner.IsVisible;

    /// <inheritdoc />
    public IIntentHandler IntentHandler => owner.Handler;
  }
}
