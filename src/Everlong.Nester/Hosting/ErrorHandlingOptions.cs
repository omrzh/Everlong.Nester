namespace Everlong.Nester.Hosting;

/// <summary>
///   Configures host-level error handling behaviour.
/// </summary>
public sealed class ErrorHandlingOptions
{
  /// <summary>
  ///   Gets or sets a value indicating whether unhandled host errors are rethrown on the UI thread.
  /// </summary>
  public bool RethrowUnhandledExceptions { get; init; }

  /// <summary>
  ///   Gets or sets a value indicating whether unobserved task exceptions are marked as observed.
  /// </summary>
  public bool MarkTaskSchedulerExceptionsAsObserved { get; init; } = true;
}
