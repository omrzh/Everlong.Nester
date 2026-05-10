namespace Everlong.Nester.Routing;

/// <summary>Converts realized routing state into route requests.</summary>
public static class LocationExtensions
{
  /// <summary>Creates a route request from the site's trail — the container chain, the content target last, instances excluded.</summary>
  public static ILocator ToLocator(this ILocation site)
  {
    IReadOnlyList<ILocation> trail = site.Trail;
    var targets = new ITarget[trail.Count];
    for (int i = 0; i < trail.Count; i++)
      targets[i] = new Target(trail[i].Type, trail[i].Args);
    return new Locator(targets);
  }
}
