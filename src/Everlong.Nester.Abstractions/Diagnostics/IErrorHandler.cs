namespace Everlong.Nester.Diagnostics;

/// <summary>A contract for handling an exception.</summary>
public interface IErrorHandler
{
  /// <summary>Handles an exception.</summary>
  /// <param name="exception">The exception to handle.</param>
  /// <returns>
  ///   <see langword="true" /> if the exception was handled;
  ///   otherwise, <see langword="false" />.
  /// </returns>
  bool HandleError(Exception exception);
}

/// <summary>A contract for reporting an exception.</summary>
public interface IErrorReporter
{
  /// <summary>Reports an exception.</summary>
  /// <param name="exception">The exception to report.</param>
  void ReportError(Exception exception);
}
