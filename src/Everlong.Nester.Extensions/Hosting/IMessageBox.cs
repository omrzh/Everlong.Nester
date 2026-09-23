namespace Everlong.Nester.Hosting;

/// <summary>
///   Shows a confirmation prompt — the surface the session-ending
///   orchestration puts its guards through.
/// </summary>
public interface IMessageBox
{
  /// <summary>
  ///   Shows a confirmation prompt and reports the user's answer.
  /// </summary>
  /// <param name="title">The prompt's title.</param>
  /// <param name="message">The prompt's message.</param>
  /// <returns><see langword="true" /> when the user confirms.</returns>
  bool Confirm(string title, string message);
}
