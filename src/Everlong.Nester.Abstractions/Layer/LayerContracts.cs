using Everlong.Nester.Intent;

namespace Everlong.Nester.Layer;

/// <summary>A closed z interval.</summary>
public readonly record struct LayerBand(int Floor, int Ceiling)
{
  /// <summary>Whether <paramref name="z" /> lies in the band.</summary>
  public bool Contains(int z) => z >= Floor && z <= Ceiling;

  /// <summary>The number of z values the band spans.</summary>
  public int Height => Ceiling - Floor + 1;

  /// <summary>The band holding the single z <paramref name="z" />.</summary>
  public static LayerBand At(int z) => new(z, z);
}

/// <summary>The reserved bands.</summary>
public static class KnownLayers
{
  /// <summary>The neutral band.</summary>
  public static readonly LayerBand Neutral = LayerBand.At(0);

  /// <summary>The backdrop band.</summary>
  public static readonly LayerBand Backdrop = new(100, 299);

  /// <summary>The navigation band.</summary>
  public static readonly LayerBand Navigation = new(1000, 1999);

  /// <summary>The floating tools band.</summary>
  public static readonly LayerBand Floating = new(2000, 2999);

  /// <summary>The dialog band.</summary>
  public static readonly LayerBand Dialog = new(3000, 3999);

  /// <summary>The toast / snackbar / notification band.</summary>
  public static readonly LayerBand Notice = new(4000, 4999);

  /// <summary>The dev tools band.</summary>
  public static readonly LayerBand DevTool = new(9000, 9999);

  /// <summary>The transition surface band.</summary>
  public static readonly LayerBand Flying = LayerBand.At(int.MaxValue);
}

/// <summary>Where inside a band a lease lands.</summary>
public enum LayerPolicy
{
  /// <summary>The band's floor.</summary>
  Floor,

  /// <summary>The band's ceiling.</summary>
  Ceiling,

  /// <summary>One above the band's highest live lease.</summary>
  AboveHighest,
}

/// <summary>A granted stacking position and the content it displays.</summary>
/// <remarks>
///   Single-use: a released or evicted lease never becomes active again.
///   <see cref="Z" /> is fixed at the grant; the content, the visibility
///   and the intent handler remain writable.
/// </remarks>
public interface ILayerLease
{
  /// <summary>
  ///   <see langword="true" /> while the lease is recorded;
  ///   <see langword="false" /> permanently after <see cref="Release" /> or
  ///   an eviction.
  /// </summary>
  bool IsActive { get; }

  /// <summary>The granted z.</summary>
  int Z { get; }

  /// <summary>The displayed content; <see langword="null" /> clears the slot without ending the lease.</summary>
  object? Content { get; set; }

  /// <summary>Whether the slot is displayed.</summary>
  bool IsVisible { get; set; }

  /// <summary>
  ///   The handler this lease presents to an intent dispatch — a
  ///   pass-through handler until one is assigned.
  /// </summary>
  IIntentHandler IntentHandler { get; set; }

  /// <summary>Ends the lease and unmounts the slot.</summary>
  /// <remarks>Idempotent; no eviction notice is triggered.</remarks>
  void Release();
}

/// <summary>Receives notice when a held lease is reclaimed.</summary>
public interface ILayerTenant
{
  /// <summary>Called once per reclaimed lease, after the lease is dead.</summary>
  /// <remarks>
  ///   The lease is already inactive — <see cref="ILayerLease.IsActive" />
  ///   is <see langword="false" /> and
  ///   <see cref="ILayerLease.Release" /> is a no-op.  A throw does not
  ///   interrupt the eviction of the remaining leases.
  /// </remarks>
  ValueTask OnEvictedAsync(ILayerLease lease);
}

/// <summary>Grants leases.</summary>
/// <remarks>Single-threaded: grants, releases and eviction notices all run on the broker's thread.</remarks>
public interface ILayerBroker
{
  /// <summary>Grants a lease at the closest feasible position inside <paramref name="band" /> under <paramref name="policy" />.</summary>
  /// <remarks>
  ///   A grant never leaves the band and never fails:
  ///   <see cref="LayerPolicy.AboveHighest" /> yields the band's highest
  ///   live z plus one, clamped to the ceiling (the floor when the band
  ///   holds none), so a band with no free z left above its highest live
  ///   lease degrades by sharing the ceiling.  At equal z the later
  ///   acquisition sits above.
  /// </remarks>
  /// <exception cref="InvalidOperationException">The broker no longer grants leases.</exception>
  ILayerLease Acquire(ILayerTenant tenant, object content, LayerBand band, LayerPolicy policy);

  /// <summary>Grants a lease at the exact z <paramref name="z" />.</summary>
  /// <remarks>Occupied z values are not rejected: same-z co-tenants share the band.</remarks>
  /// <exception cref="InvalidOperationException">The broker no longer grants leases.</exception>
  ILayerLease Acquire(ILayerTenant tenant, object content, int z);
}
