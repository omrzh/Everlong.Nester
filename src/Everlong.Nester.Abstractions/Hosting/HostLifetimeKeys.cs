namespace Everlong.Nester.Hosting;

/// <summary>Conventional service keys for <see cref="IHostLifetime" /> registrations.</summary>
public static class HostLifetimeKeys
{
  /// <summary>The application-level <see cref="IHostLifetime" /> registration key.</summary>
  public const string App = "nester:lifetime:app";
}
