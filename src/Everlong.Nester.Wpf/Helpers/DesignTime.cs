namespace Everlong.Nester.Helpers;

/// <summary>
/// Helper utilities for detecting design-time (XAML designer) environment.
/// </summary>
public static class DesignTime
{
  static DesignTime()
  {
    DesignTimeHelper.Impl = new Helper();
  }

  /// <summary>
  /// Gets a value indicating whether the code is running in a design-time environment
  /// (designer for Avalonia or WPF).
  /// </summary>
  public static bool IsTrue
    => System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());

  /// <summary>
  /// Gets a value indicating whether the code is not running in a design-time environment.
  /// </summary>
  public static bool IsFalse => !IsTrue;

  /// <summary>
  /// Design-time helper that implements <see cref="IDesignTime"/> and forwards calls to <see cref="DesignTime"/>.
  /// </summary>
  private class Helper : IDesignTime
  {
    bool IDesignTime.IsTrue => DesignTime.IsTrue;
  }
}
