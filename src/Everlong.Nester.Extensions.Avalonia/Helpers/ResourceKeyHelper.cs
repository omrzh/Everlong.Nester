using Everlong.Nester.Notice;
// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Helpers;

internal static class ResourceKeyHelper
{
  public static string? GetIconKey(object? value)
  {
    return value switch
    {
      ToastLevel level => $"Nester.Icon.{level}",
      NotificationLevel level => $"Nester.Icon.{level}",
      _ => null
    };
  }

  public static string? GetBrushKey(object? value)
  {
    return value switch
    {
      ToastLevel level => $"Nester.{level}",
      NotificationLevel level => $"Nester.{level}",
      _ => null
    };
  }
}
