namespace Everlong.Nester.Threading;

/// <summary>
///   Priority of a dispatch operation on the main thread.
/// </summary>
public enum DispatchPriority
{
  /// <summary>Low priority — callback executes after normal-priority work completes.</summary>
  Low = 0,

  /// <summary>Normal priority — the default.</summary>
  Normal = 1,

  /// <summary>High priority — callback executes before normal-priority work.</summary>
  High = 2,

  /// <summary>
  ///   Send — the callback executes synchronously and immediately,
  ///   bypassing the queue.
  /// </summary>
  /// <remarks>
  ///   Re-entrancy and deadlock are possible.
  /// </remarks>
  Send = 3
}
