using Everlong.Nester.Messaging;

namespace Everlong.Nester.Hosting;

/// <summary>
///   The session-ending arbitration — collects the guards the participants
///   broadcast, puts each to the user, and reports whether the shutdown may
///   proceed.
/// </summary>
public static class SessionEndingArbitration
{
  /// <summary>
  ///   Runs the arbitration over <paramref name="hub" />.
  /// </summary>
  /// <remarks>
  ///   Synchronous, on the caller's thread.  A prompt may run a nested message
  ///   loop; a request that arrives inside one is answered as a refusal.
  ///   Every guard's action runs once all prompts were confirmed; a failing
  ///   action is isolated.
  /// </remarks>
  /// <param name="hub">The hub that broadcasts the request.</param>
  /// <param name="messageBox">The prompt surface.</param>
  /// <returns><see langword="true" /> when every guard was confirmed.</returns>
  public static bool Run(IMessageHub hub, IMessageBox messageBox)
  {
    ArgumentNullException.ThrowIfNull(hub);
    ArgumentNullException.ThrowIfNull(messageBox);

    if (_arbitrating)
      return false;

    _arbitrating = true;
    try
    {
      var message = new SessionEndingMessage();
      hub.Publish(message);

      foreach (SessionEndingGuard guard in message.Guards)
        if (!messageBox.Confirm(guard.Title, guard.Message))
          return false;

      foreach (SessionEndingGuard guard in message.Guards)
      {
        try
        {
          guard.Confirmed();
        }
        catch
        {
          // A failing action must not stop the others — the session is ending.
        }
      }

      return true;
    }
    finally
    {
      _arbitrating = false;
    }
  }

  private static bool _arbitrating;
}
