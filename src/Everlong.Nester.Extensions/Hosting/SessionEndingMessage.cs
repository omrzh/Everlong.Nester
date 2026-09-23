using Everlong.Nester.Messaging;

namespace Everlong.Nester.Hosting;

/// <summary>
///   A request to end the session — broadcast before the host shuts down so
///   participants may put a question to the user and flush their state.
/// </summary>
public sealed class SessionEndingMessage : IMessage
{
  private readonly List<SessionEndingGuard> _guards = [];

  /// <summary>Gets the guards registered by the recipients, in registration order.</summary>
  public IReadOnlyList<SessionEndingGuard> Guards => _guards;

  /// <summary>
  ///   Registers a guard — the question to put to the user and the action to
  ///   run once every guard is confirmed.
  /// </summary>
  /// <param name="title">The prompt's title.</param>
  /// <param name="message">The prompt's message.</param>
  /// <param name="confirmed">The action to run when every guard is confirmed.</param>
  public void Guard(string title, string message, Action confirmed)
    => _guards.Add(new SessionEndingGuard(title, message, confirmed));
}

/// <summary>
///   One participant's session-ending guard — a prompt and the action it
///   confirms.
/// </summary>
/// <param name="Title">The prompt's title.</param>
/// <param name="Message">The prompt's message.</param>
/// <param name="Confirmed">The action to run when every guard is confirmed.</param>
public sealed record SessionEndingGuard(string Title, string Message, Action Confirmed);
