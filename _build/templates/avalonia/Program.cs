using Avalonia;

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
      .LogToTrace();
}
