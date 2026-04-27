namespace Everlong.Nester.Helpers;

/// <summary>
///   Indicates whether the application is running in design-time.
/// </summary>
public interface IDesignTime
{
  /// <summary>
  ///   Gets a value indicating whether the current execution context is design time.
  /// </summary>
  bool IsTrue { get; }
}
