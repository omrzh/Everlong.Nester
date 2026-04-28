namespace Everlong.Nester.Messaging;

/// <summary>
///   The aggregate report of recipient failures collected within one broadcast.
/// </summary>
/// <remarks>
///   Exposes the failures in execution order through
///   <see cref="AggregateException.InnerExceptions" />.
/// </remarks>
/// <param name="failures">The failures, in execution order.</param>
public sealed class MessageReceiveException(IReadOnlyList<Exception> failures)
  : AggregateException($"{failures.Count} message recipients failed.", failures);
