using System.Windows;
using System.Windows.Media;
using Everlong.DI;
using Everlong.Nester.Theming;
using NesterApp.Properties;

namespace NesterApp.Services;

/// <summary>Applies the theme choice to the WPF application.</summary>
[Singleton<IThemeService>]
public sealed class WpfThemeService : IThemeService
{
  private const string DarkPaletteUri = "Assets/AppResources.Dark.xaml";

  /// <inheritdoc />
  public void Apply(AppTheme theme)
  {
    if (Application.Current is not { } app)
      return;

#pragma warning disable WPF0001 // Application.ThemeMode is experimental.
    app.ThemeMode = theme switch
    {
      AppTheme.Light => ThemeMode.Light,
      AppTheme.Dark => ThemeMode.Dark,
      _ => ThemeMode.System,
    };
#pragma warning restore WPF0001

    // WPF has no ThemeDictionaries: Fluent swaps itself, the app tokens and
    // Nester's palettes swap explicitly against the effective theme.
    bool dark = IsDark(app);
    SwapAppPalette(app, dark);
    NesterTheme.UseDark(dark);
    NesterExtendedTheme.UseDark(dark);
  }

  /// <summary>Merges or removes the template's dark App.* dictionary.</summary>
  private static void SwapAppPalette(Application app, bool dark)
  {
    var merged = app.Resources.MergedDictionaries;
    ResourceDictionary? existing = merged.FirstOrDefault(IsDarkPalette);

    if (dark)
    {
      if (existing is null)
        merged.Add(new ResourceDictionary { Source = new Uri(DarkPaletteUri, UriKind.Relative) });
    }
    else if (existing is not null)
    {
      merged.Remove(existing);
    }
  }

  private static bool IsDarkPalette(ResourceDictionary dictionary)
  {
    string? source = dictionary.Source?.OriginalString;
    return source is not null
           && (source.Equals(DarkPaletteUri, StringComparison.OrdinalIgnoreCase)
               || source.EndsWith("/" + DarkPaletteUri, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>Reads the effective theme from Fluent's primary text brush.</summary>
  private static bool IsDark(Application app)
    => app.TryFindResource("TextFillColorPrimaryBrush") is SolidColorBrush brush
       && (brush.Color.R + brush.Color.G + brush.Color.B) / 3 > 160;
}
