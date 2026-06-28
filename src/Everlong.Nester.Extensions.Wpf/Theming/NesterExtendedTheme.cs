namespace Everlong.Nester.Theming;

/// <summary>Selects the Extensions chrome palette at runtime.</summary>
public static class NesterExtendedTheme
{
  private const string DarkDictionaryUri
    = "pack://application:,,,/Everlong.Nester.Extensions.Wpf;component/Themes/NesterExtendedTheme.Dark.xaml";

  /// <summary>Uses the dark palette when <paramref name="dark" /> is true, the light palette otherwise.</summary>
  public static void UseDark(bool dark) => ThemeDictionarySwap.Apply(DarkDictionaryUri, dark);
}
