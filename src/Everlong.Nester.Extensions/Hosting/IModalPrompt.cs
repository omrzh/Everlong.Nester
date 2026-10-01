namespace Everlong.Nester.Hosting;

/// <summary>
///   Presents a modal prompt on the calling thread.
/// </summary>
/// <remarks>
///   Synchronous: a call blocks the calling thread until the user answers,
///   and may run a nested message loop while it waits.
/// </remarks>
public interface IModalPrompt
{
  /// <summary>
  ///   Presents a confirmation prompt and reports the user's answer.
  /// </summary>
  /// <param name="title">The prompt's title.</param>
  /// <param name="message">The prompt's message.</param>
  /// <returns>
  ///   <see langword="true" /> when the user confirms;
  ///   <see langword="false" /> when the user declines.
  /// </returns>
  bool Confirm(string title, string message);

  /// <summary>
  ///   Presents an alert and waits for the user to acknowledge it.
  /// </summary>
  /// <param name="title">The alert's title.</param>
  /// <param name="message">The alert's message.</param>
  void Alert(string title, string message);
}
