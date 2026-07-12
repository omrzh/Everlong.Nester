using Terminal.Gui.App;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Builds and binds the Terminal.Gui app lifetime to the
///   <see cref="AppLifetime" /> static facade.
/// </summary>
public static class AppLifetimeBuilder
{
  /// <summary>
  ///   Builds the app lifetime over <paramref name="app" /> and binds it.
  /// </summary>
  /// <param name="app">The initialized Terminal.Gui application.</param>
  /// <param name="options">Lifetime configuration.</param>
  /// <exception cref="InvalidOperationException">
  ///   Thrown when a lifetime is already bound.
  /// </exception>
  public static void Build(IApplication app, AppLifetimeOptions options)
  {
    if (AppLifetime.Current != null)
    {
      throw new InvalidOperationException("AppLifetime is already built. Cannot build multiple times.");
    }

    new TerminalAppLifetime(app, options).BindStaticFacades();
  }
}
