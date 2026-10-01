using Everlong.Nester.Intent;

namespace Everlong.Nester.Layer;

/// <summary>The reserved stacking planes a lease can be granted in.</summary>
/// <remarks>
///   A plane is a closed z slice with a fixed stacking position and no
///   knowledge of who occupies it; the plane's names state intent, not
///   occupancy.  <see cref="Overlay" /> is the ephemeral interaction chain —
///   its leases stack in grant order.
/// </remarks>
public enum LayerPlane
{
  /// <summary>Below the base surface — background services.</summary>
  Ground = 0,

  /// <summary>The base surface — the persistent navigation stack.</summary>
  Base = 1,

  /// <summary>Above the base surface — panels that coexist with it.</summary>
  Dock = 2,

  /// <summary>The ephemeral interaction chain — dialogs, palettes and popups.</summary>
  Overlay = 3,

  /// <summary>Above the interaction chain — transient feedback.</summary>
  Notice = 4,

  /// <summary>Above feedback — developer tools.</summary>
  Debug = 5,

  /// <summary>The frontmost plane — transition ghosts.</summary>
  Ghost = 6,
}

/// <summary>Why a layer-focus transfer is running.</summary>
public enum LayerFocusCause
{
  /// <summary>A candidate appeared on the stack.</summary>
  Granted = 0,

  /// <summary>The holder left the stack.</summary>
  Departed = 1,

  /// <summary>A layer asked for layer focus.</summary>
  Requested = 2,
}

/// <summary>One layer-focus consultation or transfer.</summary>
/// <remarks>Carries the cause of the transfer; the lease under consideration is the receiver.</remarks>
public readonly record struct LayerFocusContext(LayerFocusCause Cause);

/// <summary>The read-only identity and observed state of a granted lease.</summary>
/// <remarks>
///   Single-use: a released or evicted lease never becomes live again.
///   <see cref="Z" /> and <see cref="Plane" /> are fixed at the grant.  A
///   lease exposes no writable member — the holder shapes the slot through
///   the <see cref="ILayerHandle" /> the grant returned.
/// </remarks>
public interface ILayerLease
{
  /// <summary>
  ///   <see langword="true" /> while the lease is recorded;
  ///   <see langword="false" /> permanently after an eviction or the
  ///   holder's release.
  /// </summary>
  bool IsLive { get; }

  /// <summary>The granted z.</summary>
  int Z { get; }

  /// <summary>The plane the lease was granted in.</summary>
  LayerPlane Plane { get; }

  /// <summary>The surface the lease presents — fixed at the grant.</summary>
  object Content { get; }

  /// <summary>Whether the slot is displayed.</summary>
  bool IsVisible { get; }

  /// <summary>
  ///   The handler this lease presents to an intent dispatch — a
  ///   pass-through handler until one is assigned.
  /// </summary>
  IIntentHandler IntentHandler { get; }
}

/// <summary>The holder's writable handle over a granted lease.</summary>
/// <remarks>
///   The grant's only writable channel: <see cref="Lease" /> is the
///   read-only identity, and the visibility and the intent handler stay
///   mutable for the lease's life — the content is fixed at the grant.  A
///   handle is never handed to an observer — <see cref="ILayerBroker.Focused" />
///   and <see cref="ILayerTenant.OnEvictedAsync" /> expose the read-only
///   <see cref="ILayerLease" /> instead.
/// </remarks>
public interface ILayerHandle
{
  /// <summary>The read-only lease this handle controls.</summary>
  ILayerLease Lease { get; }

  /// <summary>Shows or hides the slot without ending the lease.</summary>
  void SetVisible(bool visible);

  /// <summary>Replaces the handler this lease presents to an intent dispatch.</summary>
  void SetIntentHandler(IIntentHandler handler);

  /// <summary>Ends the lease and unmounts the slot.</summary>
  /// <remarks>Idempotent; no eviction notice is triggered.</remarks>
  void Release();
}

/// <summary>Receives notice when a held lease is reclaimed.</summary>
public interface ILayerTenant
{
  /// <summary>Called once per reclaimed lease, after the lease is dead.</summary>
  /// <remarks>
  ///   The lease is already inactive — <see cref="ILayerLease.IsLive" />
  ///   is <see langword="false" /> and
  ///   its handle's <see cref="ILayerHandle.Release" /> is a no-op.  A throw does not
  ///   interrupt the eviction of the remaining leases.
  /// </remarks>
  ValueTask OnEvictedAsync(ILayerLease lease);
}

/// <summary>Competes for layer focus — implemented by a lease's content.</summary>
/// <remarks>
///   Layer focus is the single grant of foreground interaction: the focused
///   layer is consulted first by an intent dispatch and is the layer the
///   element focus is expected to follow.  A lease whose content does not
///   implement this contract is never granted layer focus — the participating
///   surface is the content, so the element-focus save and restore have the
///   visual tree they need.  All members are synchronous and run on the
///   broker's thread; a callback must not release the lease under
///   consultation.
/// </remarks>
public interface IFocusableContent
{
  /// <summary>Decides whether this layer takes layer focus now; <see langword="false" /> defers to the next candidate.</summary>
  bool TryFocus(LayerFocusContext context);

  /// <summary>Called before this layer takes layer focus, while it does not hold it.</summary>
  void OnFocusing(LayerFocusContext context);

  /// <summary>Called after this layer takes layer focus.</summary>
  void OnFocused(LayerFocusContext context);

  /// <summary>Called before this layer loses layer focus, while it still holds it.</summary>
  void OnUnfocusing(LayerFocusContext context);

  /// <summary>Called after this layer loses layer focus.</summary>
  void OnUnfocused(LayerFocusContext context);
}

/// <summary>Grants leases and owns layer focus.</summary>
/// <remarks>Single-threaded: grants, releases, focus transfers and eviction notices all run on the broker's thread.</remarks>
public interface ILayerBroker
{
  /// <summary>Grants a lease in <paramref name="plane" /> at the plane's stacking position.</summary>
  /// <remarks>
  ///   A grant never fails and never leaves the plane.  Within a stacking
  ///   plane the lease lands one above the plane's highest live z, clamped
  ///   to the plane's ceiling; a non-stacking plane lands all leases on its
  ///   floor, where the later acquisition sits above.  A grant re-runs the
  ///   layer-focus election.
  /// </remarks>
  /// <exception cref="InvalidOperationException">The broker no longer grants leases.</exception>
  /// <returns>The writable handle over the granted lease.</returns>
  ILayerHandle Acquire(ILayerTenant tenant, object content, LayerPlane plane);

  /// <summary>The lease that currently holds layer focus, or <see langword="null" /> when none does.</summary>
  ILayerLease? Focused { get; }

  /// <summary>Asks for layer focus on behalf of a live lease; an evicted lease or one whose content is not <see cref="IFocusableContent" /> is ignored.</summary>
  void RequestFocus(ILayerHandle handle);
}
