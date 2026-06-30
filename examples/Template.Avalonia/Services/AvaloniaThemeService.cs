using Avalonia;
using Avalonia.Styling;
using Everlong.DI;
using NesterApp.Properties;

namespace NesterApp.Services;

/// <summary>Applies the theme choice to the Avalonia application.</summary>
[Singleton<IThemeService>]
public sealed class AvaloniaThemeService : IThemeService
{
  /// <inheritdoc />
  public void Apply(AppTheme theme)
  {
    if (Application.Current is { } app)
    {
      app.RequestedThemeVariant = theme switch
      {
        AppTheme.Light => ThemeVariant.Light,
        AppTheme.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default,
      };
    }
  }
}
