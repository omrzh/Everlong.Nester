namespace Everlong.Nester.Hosting;

/// <summary>
///   Extension methods for bootstrapping Nester on a WPF application.
/// </summary>
public static class AppLifetimeBuilder
{
  /// <summary>
  ///   Builds the app lifetime for this application and binds it to the
  ///   <see cref="AppLifetime" /> static facade.
  /// </summary>
  /// <param name="options">Lifetime configuration.</param>
  public static void Build(AppLifetimeOptions options)
  {
    if (AppLifetime.Current != null)
      throw new InvalidOperationException("AppLifetime is already built. Cannot build multiple times.");
    if (PApp.Current == null)
      throw new InvalidOperationException(
        "No current application found. Ensure this is called after the application is initialized.");
    new AppLifetimeImpl(options).BindStaticFacades();
  }
}
