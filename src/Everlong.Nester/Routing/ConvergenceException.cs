namespace Everlong.Nester.Routing;

/// <summary>
///   The aggregate report of lifecycle failures collected within one
///   convergence.
/// </summary>
/// <remarks>
///   Reported in place of the individual failures when more than one
///   participant failed in the same phase.  The failures are exposed in
///   execution order through <see cref="AggregateException.InnerExceptions" />.
/// </remarks>
public sealed class ConvergenceException : AggregateException
{
  internal ConvergenceException(IReadOnlyList<Exception> failures)
    : base($"{failures.Count} routing lifecycle hooks failed.", failures)
  {
  }
}
