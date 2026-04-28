namespace Everlong.Nester.Messaging;

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" />.
/// </summary>
/// <typeparam name="T1">The accepted message type.</typeparam>
/// <remarks>
///   A type that implements more than one arity interface must implement
///   <see cref="IMessageRecipient" /> explicitly — the inherited default
///   implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1> : IMessageRecipient
  where T1 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message is T1 typed && CanReceive(typed);

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message) => Receive((T1)message);

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);
}

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" /> or <typeparamref name="T2" />.
/// </summary>
/// <typeparam name="T1">An accepted message type.</typeparam>
/// <typeparam name="T2">An accepted message type.</typeparam>
/// <remarks>
///   Types are matched in declaration order.  A type that implements more than one
///   arity interface must implement <see cref="IMessageRecipient" /> explicitly —
///   the inherited default implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1, T2> : IMessageRecipient
  where T1 : class, IMessage
  where T2 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message switch
  {
    T1 typed => CanReceive(typed),
    T2 typed => CanReceive(typed),
    _ => false,
  };

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    switch (message)
    {
      case T1 typed:
        Receive(typed);
        break;
      case T2 typed:
        Receive(typed);
        break;
      default:
        throw new InvalidOperationException($"Unaccepted message type {message.GetType()}.");
    }
  }

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T2 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T2 message);
}

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" />, <typeparamref name="T2" /> or <typeparamref name="T3" />.
/// </summary>
/// <typeparam name="T1">An accepted message type.</typeparam>
/// <typeparam name="T2">An accepted message type.</typeparam>
/// <typeparam name="T3">An accepted message type.</typeparam>
/// <remarks>
///   Types are matched in declaration order.  A type that implements more than one
///   arity interface must implement <see cref="IMessageRecipient" /> explicitly —
///   the inherited default implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1, T2, T3> : IMessageRecipient
  where T1 : class, IMessage
  where T2 : class, IMessage
  where T3 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message switch
  {
    T1 typed => CanReceive(typed),
    T2 typed => CanReceive(typed),
    T3 typed => CanReceive(typed),
    _ => false,
  };

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    switch (message)
    {
      case T1 typed:
        Receive(typed);
        break;
      case T2 typed:
        Receive(typed);
        break;
      case T3 typed:
        Receive(typed);
        break;
      default:
        throw new InvalidOperationException($"Unaccepted message type {message.GetType()}.");
    }
  }

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T2 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T3 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T2 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T3 message);
}

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" /> or <typeparamref name="T4" />.
/// </summary>
/// <typeparam name="T1">An accepted message type.</typeparam>
/// <typeparam name="T2">An accepted message type.</typeparam>
/// <typeparam name="T3">An accepted message type.</typeparam>
/// <typeparam name="T4">An accepted message type.</typeparam>
/// <remarks>
///   Types are matched in declaration order.  A type that implements more than one
///   arity interface must implement <see cref="IMessageRecipient" /> explicitly —
///   the inherited default implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1, T2, T3, T4> : IMessageRecipient
  where T1 : class, IMessage
  where T2 : class, IMessage
  where T3 : class, IMessage
  where T4 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message switch
  {
    T1 typed => CanReceive(typed),
    T2 typed => CanReceive(typed),
    T3 typed => CanReceive(typed),
    T4 typed => CanReceive(typed),
    _ => false,
  };

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    switch (message)
    {
      case T1 typed:
        Receive(typed);
        break;
      case T2 typed:
        Receive(typed);
        break;
      case T3 typed:
        Receive(typed);
        break;
      case T4 typed:
        Receive(typed);
        break;
      default:
        throw new InvalidOperationException($"Unaccepted message type {message.GetType()}.");
    }
  }

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T2 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T3 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T4 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T2 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T3 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T4 message);
}

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />, <typeparamref name="T4" /> or <typeparamref name="T5" />.
/// </summary>
/// <typeparam name="T1">An accepted message type.</typeparam>
/// <typeparam name="T2">An accepted message type.</typeparam>
/// <typeparam name="T3">An accepted message type.</typeparam>
/// <typeparam name="T4">An accepted message type.</typeparam>
/// <typeparam name="T5">An accepted message type.</typeparam>
/// <remarks>
///   Types are matched in declaration order.  A type that implements more than one
///   arity interface must implement <see cref="IMessageRecipient" /> explicitly —
///   the inherited default implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1, T2, T3, T4, T5> : IMessageRecipient
  where T1 : class, IMessage
  where T2 : class, IMessage
  where T3 : class, IMessage
  where T4 : class, IMessage
  where T5 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message switch
  {
    T1 typed => CanReceive(typed),
    T2 typed => CanReceive(typed),
    T3 typed => CanReceive(typed),
    T4 typed => CanReceive(typed),
    T5 typed => CanReceive(typed),
    _ => false,
  };

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    switch (message)
    {
      case T1 typed:
        Receive(typed);
        break;
      case T2 typed:
        Receive(typed);
        break;
      case T3 typed:
        Receive(typed);
        break;
      case T4 typed:
        Receive(typed);
        break;
      case T5 typed:
        Receive(typed);
        break;
      default:
        throw new InvalidOperationException($"Unaccepted message type {message.GetType()}.");
    }
  }

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T2 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T3 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T4 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T5 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T2 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T3 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T4 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T5 message);
}

/// <summary>
///   A recipient that accepts messages of type <typeparamref name="T1" />, <typeparamref name="T2" />, <typeparamref name="T3" />, <typeparamref name="T4" />, <typeparamref name="T5" /> or <typeparamref name="T6" />.
/// </summary>
/// <typeparam name="T1">An accepted message type.</typeparam>
/// <typeparam name="T2">An accepted message type.</typeparam>
/// <typeparam name="T3">An accepted message type.</typeparam>
/// <typeparam name="T4">An accepted message type.</typeparam>
/// <typeparam name="T5">An accepted message type.</typeparam>
/// <typeparam name="T6">An accepted message type.</typeparam>
/// <remarks>
///   Types are matched in declaration order.  A type that implements more than one
///   arity interface must implement <see cref="IMessageRecipient" /> explicitly —
///   the inherited default implementations have no most-specific one.
/// </remarks>
public interface IMessageRecipient<T1, T2, T3, T4, T5, T6> : IMessageRecipient
  where T1 : class, IMessage
  where T2 : class, IMessage
  where T3 : class, IMessage
  where T4 : class, IMessage
  where T5 : class, IMessage
  where T6 : class, IMessage
{
  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message switch
  {
    T1 typed => CanReceive(typed),
    T2 typed => CanReceive(typed),
    T3 typed => CanReceive(typed),
    T4 typed => CanReceive(typed),
    T5 typed => CanReceive(typed),
    T6 typed => CanReceive(typed),
    _ => false,
  };

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    switch (message)
    {
      case T1 typed:
        Receive(typed);
        break;
      case T2 typed:
        Receive(typed);
        break;
      case T3 typed:
        Receive(typed);
        break;
      case T4 typed:
        Receive(typed);
        break;
      case T5 typed:
        Receive(typed);
        break;
      case T6 typed:
        Receive(typed);
        break;
      default:
        throw new InvalidOperationException($"Unaccepted message type {message.GetType()}.");
    }
  }

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T1 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T2 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T3 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T4 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T5 message) => true;

  /// <summary>
  ///   Reports whether this recipient accepts <paramref name="message" />.
  /// </summary>
  /// <param name="message">The message under consideration.</param>
  /// <returns><see langword="true" /> unless overridden.</returns>
  bool CanReceive(T6 message) => true;

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T1 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T2 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T3 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T4 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T5 message);

  /// <summary>
  ///   Receives an accepted message.
  /// </summary>
  /// <param name="message">The accepted message.</param>
  void Receive(T6 message);
}
