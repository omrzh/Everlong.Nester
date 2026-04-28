namespace Everlong.Nester.Messaging;

/// <summary>
///   A fact that has already happened.
/// </summary>
public interface IMessage;

/// <summary>
///   Broadcasts messages to the registered recipients.
/// </summary>
public interface IMessageHub
{
  /// <summary>
  ///   Registers a recipient.
  /// </summary>
  /// <remarks>
  ///   Recipients are identified by reference.  Thread-safe.
  /// </remarks>
  /// <param name="recipient">The recipient to register.</param>
  /// <returns>
  ///   A token that unregisters the recipient when disposed; disposal is
  ///   idempotent.
  /// </returns>
  /// <exception cref="ArgumentNullException"><paramref name="recipient" /> is <see langword="null" />.</exception>
  /// <exception cref="InvalidOperationException"><paramref name="recipient" /> is already registered.</exception>
  IDisposable Register(IMessageRecipient recipient);

  /// <summary>
  ///   Unregisters a recipient.
  /// </summary>
  /// <remarks>
  ///   Recipients are identified by reference; a recipient that is not
  ///   registered is a no-op.  Thread-safe.
  /// </remarks>
  /// <param name="recipient">The recipient to unregister.</param>
  /// <exception cref="ArgumentNullException"><paramref name="recipient" /> is <see langword="null" />.</exception>
  void Unregister(IMessageRecipient recipient);

  /// <summary>
  ///   Broadcasts <paramref name="message" /> to the recipients that accept
  ///   it, in registration order.
  /// </summary>
  /// <remarks>
  ///   Synchronous; runs on the caller's thread.  A nested broadcast settles
  ///   before the outer one resumes.  A throw from
  ///   <see cref="IMessageRecipient.CanReceive" /> aborts the broadcast; a
  ///   throw from <see cref="IMessageRecipient.Receive" /> is isolated per
  ///   recipient and reported after the broadcast completes.  Thread-safe.
  /// </remarks>
  /// <param name="message">The message to broadcast.</param>
  /// <exception cref="ArgumentNullException"><paramref name="message" /> is <see langword="null" />.</exception>
  /// <exception cref="MessageReceiveException">One or more recipients threw from <see cref="IMessageRecipient.Receive" />.</exception>
  void Publish(IMessage message);

  /// <summary>
  ///   Whether at least one recipient is registered.
  /// </summary>
  bool HasRecipients { get; }
}

/// <summary>
///   A recipient that declares whether it accepts a broadcast message.
/// </summary>
public interface IMessageRecipient
{
  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <remarks>
  ///   Pure — deterministic for a given message instance and free of side
  ///   effects.  A throw is a contract violation and aborts the broadcast.
  /// </remarks>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> when this recipient accepts the message.</returns>
  bool CanReceive(IMessage message);

  /// <summary>
  ///   Receives a message this recipient accepted.
  /// </summary>
  /// <remarks>
  ///   Runs on the publisher's thread.  The instance is shared by every
  ///   recipient of one broadcast and must not be mutated.  A throw is
  ///   isolated per recipient — the broadcast continues and the failures
  ///   surface as <see cref="MessageReceiveException" />.
  /// </remarks>
  /// <param name="message">The accepted message.</param>
  void Receive(IMessage message);
}
