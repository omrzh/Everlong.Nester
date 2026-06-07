namespace Everlong.Nester.Theming;

/// <summary>Selects the core chrome palette at runtime.</summary>
public static class NesterTheme
{
  private const string DarkDictionaryUri
    = "pack://application:,,,/Everlong.Nester.Wpf;component/Themes/Colors.Dark.xaml";

  /// <summary>Uses the dark palette when <paramref name="dark" /> is true, the light palette otherwise.</summary>
  public static void UseDark(bool dark) => ThemeDictionarySwap.Apply(DarkDictionaryUri, dark);
}
