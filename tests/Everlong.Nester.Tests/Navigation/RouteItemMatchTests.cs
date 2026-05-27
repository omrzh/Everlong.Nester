using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Xunit;

using static Everlong.Nester.Tests.Navigation.ChainBuilder;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   Default navigation highlight.  An item names a destination; the presented
///   chain is walked from its content target upward and a site matches when its
///   type equals the destination's content target type.
/// </summary>
public class RouteItemMatchTests
{
  private sealed class MainLayoutVm;

  private sealed class SettingsLayoutVm;

  private sealed class LandingVm;

  private sealed class PostsVm;

  private sealed class PostDetailVm;

  private sealed class SettingsVm;

  private sealed class AccountVm;

  // ── Destination / target derivation ───────────────────────────────

  [Fact]
  public void TargetType_DefaultsToContentTargetType()
  {
    RouteItem item = ItemTo(typeof(PostsVm), typeof(MainLayoutVm));

    Assert.Equal(typeof(PostsVm), item.TargetType);
  }

  [Fact]
  public void Item_WithoutDestination_HasNoTargetAndIsNeverHighlighted()
  {
    RouteItem group = new() { Title = "Main", Children = [ItemTo(typeof(LandingVm))] };

    Assert.Null(group.TargetType);
    Assert.False(group.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  // ── Default type containment ──────────────────────────────────────

  [Fact]
  public void Leaf_MatchesItsOwnPage()
  {
    Assert.True(ItemTo(typeof(LandingVm), typeof(MainLayoutVm))
      .IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void Leaf_DoesNotMatchSiblingPage()
  {
    Assert.False(ItemTo(typeof(PostsVm), typeof(MainLayoutVm))
      .IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void Leaf_DoesNotMatchWhenTargetIsAbsentFromTheChain()
  {
    Assert.False(ItemTo(typeof(PostsVm), typeof(MainLayoutVm))
      .IsHighlighted(Chain(typeof(MainLayoutVm), typeof(PostDetailVm))));
  }

  [Fact]
  public void Leaf_MatchesWithoutAnyLayout()
  {
    Assert.True(ItemTo(typeof(LandingVm)).IsHighlighted(Chain(typeof(LandingVm))));
  }

  [Fact]
  public void ContainerItem_MatchesWhileAChildPageIsPresented()
  {
    // A section item whose destination content target is the layout itself.
    RouteItem settings = ItemTo(typeof(SettingsLayoutVm), typeof(MainLayoutVm));

    Assert.True(settings.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(SettingsLayoutVm), typeof(AccountVm))));
    Assert.False(settings.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void RepeatedTypeInTheChain_IsUnambiguous()
  {
    Assert.True(ItemTo(typeof(PostsVm), typeof(MainLayoutVm))
      .IsHighlighted(Chain(typeof(MainLayoutVm), typeof(MainLayoutVm), typeof(PostsVm))));
  }

  // ── Edges ─────────────────────────────────────────────────────────

  [Fact]
  public void NullCurrent_IsNeverHighlighted()
  {
    Assert.False(ItemTo(typeof(PostsVm), typeof(MainLayoutVm)).IsHighlighted(null));
  }

  [Fact]
  public void Group_MatchesItsOwnPage()
  {
    RouteItem group = new()
    {
      Title = "Settings",
      Destination = RouteTo(typeof(SettingsVm), typeof(MainLayoutVm)),
      Children = [ItemTo(typeof(AccountVm), typeof(SettingsLayoutVm))]
    };

    Assert.True(group.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(SettingsLayoutVm), typeof(SettingsVm))));
  }

  [Fact]
  public void HasHighlightedDescendant_IsTrueForAnActiveChild()
  {
    RouteItem group = new() { Title = "Main", Children = [ItemTo(typeof(PostsVm), typeof(MainLayoutVm))] };

    Assert.True(RouteItemMatch.HasHighlightedDescendant(
      group, Chain(typeof(MainLayoutVm), typeof(PostsVm))));
    Assert.False(RouteItemMatch.HasHighlightedDescendant(
      group, Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void HasHighlightedDescendant_WalksNestedGroups()
  {
    RouteItem leaf = ItemTo(typeof(PostsVm), typeof(MainLayoutVm));
    RouteItem inner = new() { Title = "Inner", Children = [leaf] };
    RouteItem outer = new() { Title = "Outer", Children = [inner] };

    Assert.True(RouteItemMatch.HasHighlightedDescendant(outer, Chain(typeof(MainLayoutVm), typeof(PostsVm))));
  }

  [Fact]
  public void InterfaceDefaults_ApplyToCustomImplementations()
  {
    IRouteItem item = new BareItem(RouteTo(typeof(PostsVm), typeof(MainLayoutVm)));

    Assert.Equal(typeof(PostsVm), RouteItemMatch.TargetType(item));
    Assert.True(RouteItemMatch.IsHighlighted(item, Chain(typeof(MainLayoutVm), typeof(PostsVm))));
    Assert.False(RouteItemMatch.IsHighlighted(item, Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void CustomItem_WithoutDestination_HasNoTarget()
  {
    IRouteItem item = new BareItem(null);

    Assert.Null(RouteItemMatch.TargetType(item));
    Assert.False(RouteItemMatch.IsHighlighted(item, Chain(typeof(LandingVm))));
  }

  private sealed class BareItem(ILocator? destination) : IRouteItem
  {
    public string Title => "bare";

    public object? Icon => null;

    public IReadOnlyList<IRouteItem> Children => [];

    public ILocator? Destination => destination;
  }
}
