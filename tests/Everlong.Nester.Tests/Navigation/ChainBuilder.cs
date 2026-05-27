using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Routing;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>Chain and locator builders for navigation-highlight tests.</summary>
internal static class ChainBuilder
{
  /// <summary>Builds a realized chain over the given types, outermost first, instances created per type.</summary>
  internal static ILocation Chain(params Type[] types)
    => Nodes([.. types.Select(static t => (t, (object?)null))]);

  /// <summary>Builds a realized chain over the given (type, instance) nodes, outermost first.</summary>
  internal static ILocation Nodes(params (Type Type, object? Instance)[] nodes)
  {
    Location? parent = null;
    Location? terminal = null;
    foreach ((Type type, object? instance) in nodes)
    {
      var node = new TestLocation(type, Args.Empty, instance ?? Activator.CreateInstance(type)!);
      if (parent is not null)
        node.Parent = parent;
      parent = node;
      terminal = node;
    }

    return terminal!;
  }

  /// <summary>Builds a locator whose content target is <paramref name="pageType" /> and whose layouts are <paramref name="layouts" />.</summary>
  internal static Locator RouteTo(Type pageType, params Type[] layouts)
    => new([.. layouts.Select(static t => Target.Of(t)), Target.Of(pageType)]);

  /// <summary>Builds a locator whose content target carries <paramref name="args" /> and whose layouts are <paramref name="layouts" />.</summary>
  internal static Locator RouteTo(Type pageType, IArgs args, params Type[] layouts)
    => new([.. layouts.Select(static t => Target.Of(t)), Target.Of(pageType, args)]);

  /// <summary>Builds a leaf item over the given content target and layouts.</summary>
  internal static RouteItem ItemTo(Type pageType, params Type[] layouts)
    => new() { Title = pageType.Name, Destination = RouteTo(pageType, layouts) };
}
