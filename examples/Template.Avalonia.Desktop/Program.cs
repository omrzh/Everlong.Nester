using Avalonia;
using Avalonia.Media;

namespace NesterApp;

internal class Program
{
  [STAThread]
  public static void Main(string[] args)
  {
    try
    {
      BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    catch (Exception e)
    {
      Console.WriteLine(e.StackTrace);
    }
  }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>()
      .UsePlatformDetect()
      .WithInterFont()
      // Avalonia's automatic fallback does not prefer color fonts: the first window that
      // renders a symbol in the same Unicode bucket (e.g. the login window's ✕) can cache
      // a monochrome face, and every later emoji reuses it.  An explicit color-emoji
      // fallback is consulted before the bucket cache, so the result is deterministic.
      .With(new FontManagerOptions
      {
        FontFallbacks =
        [
          new FontFallback
          {
            FontFamily = new FontFamily("Segoe UI Emoji, Apple Color Emoji, Noto Color Emoji"),
            UnicodeRange = UnicodeRange.Default,
          },
        ],
      })
      .LogToTrace();
}
