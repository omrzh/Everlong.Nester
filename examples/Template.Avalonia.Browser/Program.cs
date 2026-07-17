using Avalonia;
using Avalonia.Browser;
using NesterApp;

internal partial class Program
{
  private static Task Main(string[] args)
  {
    return BuildAvaloniaApp()
      .ConfigureFonts(fontManager => fontManager.AddFontCollection(new CjkFontCollection()))
      .WithInterFont()
      .StartBrowserAppAsync("out");
  }

  public static AppBuilder BuildAvaloniaApp() =>
    AppBuilder.Configure<App>();
}
