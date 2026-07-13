using Everlong.Nester.Hosting;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using Terminal.Gui.App;

namespace NesterApp;

/// <summary>
///   The Terminal.Gui launch: one full-screen session — the Nester shell is
///   the surface the app loop runs.
/// </summary>
public static class Program
{
  public static async Task<int> Main(string[] args)
  {
    using IApplication app = Application.Create().Init();

    try
    {
      // ① The process-level container first (auth + process services); the
      //    shell bridges from it.
      var process = App.BuildAppServices();

      // ② The app lifetime — the terminal is single-view: the stage is the
      //    whole surface.
      AppLifetimeBuilder.Build(app, new AppLifetimeOptions
      {
        ErrorHandler = new App(),
        Services = process
      });

      // ③ Shell assembly — the user-owned shell: Start runs the ceremony
      //    (container → Director → stage → surface) and launches the
      //    startup dispatch; the loop below settles it.
      var shell = new UiShell { DirectorType = typeof(MainViewModel) };
      shell.Start();
      AppLifetime.SetMainShell(shell);

      // i18n — runs synchronously; views resolve Lang after this line.
      Lang.Initialize(AppSettings.Default.Language);

      // ④ The main loop — runs the shell's surface until a close intent
      //    requests stop.
      app.Run(shell.Surface);

      return 0;
    }
    catch (Exception ex)
    {
      Console.Error.WriteLine(ex);
      return 1;
    }
  }
}
