using Everlong.Nester.Shell;

namespace Everlong.Nester.Hosting;

/// <summary>
///   Extension methods for bootstrapping Nester on an Avalonia desktop application.
/// </summary>
public static class AppLifetimeBuilder
{
  /// <summary>
  ///   Builds the app lifetime for this application and binds it to the
  ///   <see cref="AppLifetime" /> static facade.
  /// </summary>
  /// <param name="options">Lifetime configuration.</param>
  /// <exception cref="InvalidOperationException">
  ///   Thrown when a lifetime is already bound, or when no Avalonia
  ///   application is initialized.
  /// </exception>
  public static void Build(AppLifetimeOptions options)
  {
    if (AppLifetime.Current != null)
      throw new InvalidOperationException("AppLifetime is already built. Cannot build multiple times.");
    if (PApp.Current == null)
      throw new InvalidOperationException(
        "No current application found. Ensure this is called after the application is initialized.");

    // The runtime shape decides whether this host presents a single MainView:
    // browser/Android lifetimes do; classic desktop does not.  OS facts never
    // answer this question — a desktop session can host either shape.
    var isSingleView = SingleViewLifetime.IsSingleView(PApp.Current?.ApplicationLifetime);
    new AppLifetimeImpl(options, isSingleView).BindStaticFacades();
  }
}
