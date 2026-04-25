namespace Everlong.Nester.Activation;

/// <summary>
///   The process's activation agent: converts external input into
///   activation intents and delivers them through the bound host channel.
/// </summary>
/// <remarks>
///   Constructed before negotiation and bound via <see cref="Bind" />.
///   Intents arriving while no channel is bound are parked and drained by
///   <see cref="FlushAsync" />.  Disposal releases the resources the agent
///   holds.
/// </remarks>
public interface IActivationAgent : IAsyncDisposable
{
  /// <summary>
  ///   Converts command-line arguments into activation intents.
  /// </summary>
  /// <param name="args">The raw command-line arguments (no executable path).</param>
  /// <returns>The recognized activation intents; unrecognized args are not represented.</returns>
  IReadOnlyList<IActivationIntent> Convert(IReadOnlyList<string> args);

  /// <summary>
  ///   Binds the host channel, replacing any previous binding; a bound
  ///   agent may serve activation.
  /// </summary>
  /// <param name="channel">The host channel.</param>
  void Bind(IActivationChannel channel);

  /// <summary>
  ///   Releases the host channel.
  /// </summary>
  /// <param name="channel">The channel to release.</param>
  /// <returns>
  ///   <see langword="true" /> when <paramref name="channel" /> was the bound
  ///   channel and the binding is released; <see langword="false" /> when
  ///   another channel has taken over — the binding is left untouched.
  /// </returns>
  ValueTask<bool> UnbindAsync(IActivationChannel channel);

  /// <summary>
  ///   Drains the startup input: dispatches the once-converted command-line
  ///   intents followed by the parked intents, through the bound channel.
  ///   Returns whether any intent terminated the consultation.
  /// </summary>
  ValueTask<bool> FlushAsync();

  /// <summary>
  ///   Takes and drains the parked intents (intents that arrived while no
  ///   channel was bound).
  /// </summary>
  IReadOnlyList<IActivationIntent> TakePendingIntents();
}
