namespace Everlong.Nester.Hosting;

/// <summary>
///   The host's teardown signals.
/// </summary>
/// <remarks>
///   <see cref="Stopping" /> cancels when teardown begins, <see cref="Stopped" />
///   when the teardown cascade completes.  Both are live from the host's
///   construction and stay readable after its disposal.
/// </remarks>
public interface IHostLifetime
{
  /// <summary>Signals that the host has begun its teardown.</summary>
  CancellationToken Stopping { get; }

  /// <summary>Signals that the host's teardown cascade has completed.</summary>
  CancellationToken Stopped { get; }
}
