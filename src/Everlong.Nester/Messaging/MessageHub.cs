namespace Everlong.Nester.Messaging;

/// <summary>
///   A thread-safe <see cref="IMessageHub" />.
/// </summary>
public sealed class MessageHub : IMessageHub
{
  private readonly object _gate = new();
  private IMessageRecipient[] _recipients = [];

  /// <inheritdoc />
  public bool HasRecipients
  {
    get
    {
      lock (_gate)
        return _recipients.Length > 0;
    }
  }

  /// <inheritdoc />
  public IDisposable Register(IMessageRecipient recipient)
  {
    ArgumentNullException.ThrowIfNull(recipient);

    lock (_gate)
    {
      if (IndexOf(recipient) >= 0)
        throw new InvalidOperationException($"{recipient.GetType()} is already registered.");

      IMessageRecipient[] next = new IMessageRecipient[_recipients.Length + 1];
      Array.Copy(_recipients, next, _recipients.Length);
      next[^1] = recipient;
      _recipients = next;
    }

    return new Registration(this, recipient);
  }

  /// <inheritdoc />
  public void Unregister(IMessageRecipient recipient)
  {
    ArgumentNullException.ThrowIfNull(recipient);

    lock (_gate)
    {
      int index = IndexOf(recipient);
      if (index < 0)
        return;

      IMessageRecipient[] next = new IMessageRecipient[_recipients.Length - 1];
      Array.Copy(_recipients, next, index);
      Array.Copy(_recipients, index + 1, next, index, _recipients.Length - index - 1);
      _recipients = next;
    }
  }

  /// <inheritdoc />
  public void Publish(IMessage message)
  {
    ArgumentNullException.ThrowIfNull(message);

    IMessageRecipient[] snapshot;
    lock (_gate)
      snapshot = _recipients;

    List<Exception>? failures = null;
    foreach (IMessageRecipient recipient in snapshot)
    {
      if (!recipient.CanReceive(message))
        continue;

      try
      {
        recipient.Receive(message);
      }
      catch (Exception exception)
      {
        (failures ??= []).Add(exception);
      }
    }

    if (failures is not null)
      throw new MessageReceiveException(failures);
  }

  private int IndexOf(IMessageRecipient recipient)
  {
    for (int i = 0; i < _recipients.Length; i++)
    {
      if (ReferenceEquals(_recipients[i], recipient))
        return i;
    }

    return -1;
  }

  private sealed class Registration(MessageHub hub, IMessageRecipient recipient) : IDisposable
  {
    private MessageHub? _hub = hub;

    public void Dispose()
    {
      Interlocked.Exchange(ref _hub, null)?.Unregister(recipient);
    }
  }
}
