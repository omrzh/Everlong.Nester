namespace Everlong.Nester.Helpers;

/// <summary>
///   Static access to the design-time context.
/// </summary>
public static class DesignTimeHelper
{
  /// <summary>
  ///   Gets or sets the design-time implementation.  When
  ///   <see langword="null" />, design-time context is assumed.
  /// </summary>
  public static IDesignTime? Impl { get; set; }

  /// <summary>
  ///   Gets a value indicating whether the current execution context is design time.
  /// </summary>
  public static bool IsTrue => Impl is null || Impl.IsTrue;

  /// <summary>
  ///   Gets a value indicating whether the current execution context is not design time.
  /// </summary>
  public static bool IsFalse => Impl is not null && !Impl.IsTrue;
}
