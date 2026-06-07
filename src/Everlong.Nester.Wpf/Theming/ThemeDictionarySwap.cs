using System.Windows;

namespace Everlong.Nester.Theming;

/// <summary>Merges and removes a theme color dictionary on the application's resources.</summary>
internal static class ThemeDictionarySwap
{
  /// <summary>Merges the dictionary at <paramref name="uri" /> when <paramref name="dark" /> is true, removes it otherwise.</summary>
  public static void Apply(string uri, bool dark)
  {
    if (Application.Current is not { } app)
      return;

    var merged = app.Resources.MergedDictionaries;
    ResourceDictionary? existing = merged.FirstOrDefault(d => IsMatch(d, uri));

    if (dark)
    {
      if (existing is null)
        merged.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Absolute) });
    }
    else if (existing is not null)
    {
      merged.Remove(existing);
    }
  }

  private static bool IsMatch(ResourceDictionary dictionary, string uri)
    => string.Equals(dictionary.Source?.ToString(), uri, StringComparison.OrdinalIgnoreCase);
}
