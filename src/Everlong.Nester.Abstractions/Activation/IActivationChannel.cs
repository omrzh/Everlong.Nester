using Everlong.Nester.Intent;

namespace Everlong.Nester.Activation;

/// <summary>
///   The host face that receives activation intents.
/// </summary>
public interface IActivationChannel
{
  /// <summary>The native handle of the host's main window; <c>0</c> when none.</summary>
  nint HostHandle { get; }

  /// <summary>Delivers one activation intent to the host's intent pipeline.</summary>
  /// <param name="intent">The activation intent.</param>
  /// <returns>The consultation outcome.</returns>
  /// <remarks>Invoked on the main thread.</remarks>
  ValueTask<IntentResult> DispatchAsync(IActivationIntent intent);
}
