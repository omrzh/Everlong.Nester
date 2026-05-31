using Avalonia.Controls.ApplicationLifetimes;
using Everlong.Nester.Messaging;

namespace Everlong.Nester.Activation;

/// <summary>
///   Translates platform activation events into application lifecycle messages.
/// </summary>
internal static class AppLifecycleMessageFactory
{
  /// <summary>
  ///   Maps an activation event to its lifecycle message, or
  ///   <see langword="null" /> when the event carries no lifecycle fact.
  /// </summary>
  internal static IMessage? FromActivation(ActivatedEventArgs args) => args switch
  {
    { Kind: ActivationKind.Reopen } => new AppReopenMessage(),
    { Kind: ActivationKind.Background } => new AppResumedMessage(),
    _ => null,
  };

  /// <summary>Maps a deactivation event to the background-enter message.</summary>
  internal static IMessage FromDeactivation() => new AppBackgroundMessage();
}
