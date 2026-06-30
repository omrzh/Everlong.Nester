using NesterApp.Properties;

namespace NesterApp.Services;

/// <summary>Applies a theme choice to the running application.</summary>
public interface IThemeService
{
  /// <summary>Applies <paramref name="theme" /> immediately.</summary>
  void Apply(AppTheme theme);
}
