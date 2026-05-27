using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Tests.Routing;
using Xunit;

using static Everlong.Nester.Tests.Navigation.ChainBuilder;

namespace Everlong.Nester.Tests.Navigation;

/// <summary>
///   Participant votes.  A presented participant may claim a destination the
///   default target-type comparison would miss, reject a destination it does
///   not serve, or defer to the default.
/// </summary>
public class RouteHighlightTests
{
  private sealed class MainLayoutVm;

  private sealed class PostsVm;

  private sealed class LandingVm;

  private sealed class SettingsVm;

  private sealed class AccountVm;

  private sealed class SettingsLandingVm;

  private sealed record PostArgs(int Id) : Args;

  /// <summary>Claims the Posts section and disambiguates same-type instances by argument.</summary>
  private sealed class PostDetailVm : IRouteHighlight
  {
    private IArgs _args = Args.Empty;

    internal void Deliver(IArgs args) => _args = args;

    public bool? Represents(ILocator destination)
    {
      ITarget target = destination.Path[^1];
      if (target.Type == typeof(PostsVm))
        return true;
      if (target.Type == typeof(PostDetailVm) && target.Args is PostArgs requested)
        return _args is PostArgs realized && realized.Id == requested.Id;
      return null;
    }
  }

  /// <summary>Claims every destination whose chain runs through the layout.</summary>
  private sealed class SettingsLayoutVm : IRouteHighlight
  {
    public bool? Represents(ILocator destination)
      => destination.Path.Any(static t => t.Type == typeof(SettingsLayoutVm));
  }

  private sealed class DeferringVm : IRouteHighlight
  {
    public bool? Represents(ILocator destination) => null;
  }

  private sealed class RejectingVm : IRouteHighlight
  {
    public bool? Represents(ILocator destination) => false;
  }

  private static RouteItem PostsItem()
    => new() { Title = "Posts", Destination = RouteTo(typeof(PostsVm), typeof(MainLayoutVm)) };

  // ── Claim ─────────────────────────────────────────────────────────

  [Fact]
  public void Participant_ClaimsSection_KeepsItemHighlightedOnDetailPage()
  {
    Assert.True(PostsItem().IsHighlighted(Chain(typeof(MainLayoutVm), typeof(PostDetailVm))));
  }

  [Fact]
  public void Participant_Claim_WorksOnDirectEntry()
  {
    Assert.True(PostsItem().IsHighlighted(Chain(typeof(PostDetailVm))));
  }

  [Fact]
  public void Participant_Claim_DoesNotLeakToOtherSections()
  {
    Assert.False(PostsItem().IsHighlighted(Chain(typeof(MainLayoutVm), typeof(SettingsVm))));
  }

  [Fact]
  public void LayoutClaim_CoversChildPages()
  {
    RouteItem settingsLanding = new()
    {
      Title = "Settings",
      Destination = RouteTo(typeof(SettingsLandingVm), typeof(SettingsLayoutVm), typeof(MainLayoutVm))
    };

    Assert.True(settingsLanding.IsHighlighted(
      Chain(typeof(MainLayoutVm), typeof(SettingsLayoutVm), typeof(AccountVm))));
    Assert.False(settingsLanding.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  // ── Defer / reject ────────────────────────────────────────────────

  [Fact]
  public void Participant_Null_DefersToTheDefaultMatch()
  {
    RouteItem item = new() { Title = "Defer", Destination = RouteTo(typeof(DeferringVm), typeof(MainLayoutVm)) };

    Assert.True(item.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(DeferringVm))));
    Assert.False(item.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  [Fact]
  public void Participant_Reject_SuppressesTheDefaultMatch()
  {
    RouteItem item = new() { Title = "Reject", Destination = RouteTo(typeof(RejectingVm), typeof(MainLayoutVm)) };

    Assert.False(item.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(RejectingVm))));
  }

  // ── Parameterized identity ────────────────────────────────────────

  [Fact]
  public void ParameterizedItems_HighlightIndependently()
  {
    var first = new PostDetailVm();
    first.Deliver(new PostArgs(1));
    ILocation chain = Nodes((typeof(MainLayoutVm), null), (typeof(PostDetailVm), first));

    RouteItem item1 = new()
    {
      Title = "Post 1",
      Destination = RouteTo(typeof(PostDetailVm), new PostArgs(1), typeof(MainLayoutVm))
    };
    RouteItem item2 = new()
    {
      Title = "Post 2",
      Destination = RouteTo(typeof(PostDetailVm), new PostArgs(2), typeof(MainLayoutVm))
    };

    Assert.True(item1.IsHighlighted(chain));
    Assert.False(item2.IsHighlighted(chain));

    // The same participant also keeps the section item lit.
    Assert.True(PostsItem().IsHighlighted(chain));
  }

  [Fact]
  public void NonVoterParticipants_DoNotClaim()
  {
    RouteItem item = new() { Title = "Token", Destination = RouteTo(typeof(PostsVm), typeof(MainLayoutVm)) };

    Assert.False(item.IsHighlighted(Chain(typeof(MainLayoutVm), typeof(LandingVm))));
  }

  // ── Realized state ────────────────────────────────────────────────

  private sealed record DetailArgs(int Id) : Args;

  /// <summary>Adapts in place: the same instance serves revised arguments.</summary>
  private sealed class AdaptiveDetailVm : IRouteHighlight, IAdaptiveParameterized
  {
    private IArgs? _args = Args.Empty;

    public bool? Represents(ILocator destination)
    {
      ITarget target = destination.Path[^1];
      return target.Type == typeof(AdaptiveDetailVm) && target.Args is DetailArgs requested
        ? _args is DetailArgs realized && realized.Id == requested.Id
        : null;
    }

    public IArgs? EngagedArgs => _args;

    public bool IsAdaptable(IArgs? requested) => true;

    public void DeliverArgs(IArgs? args) => _args = args;
  }

  [Fact]
  public async Task ParticipantClaim_TracksTheAdaptedArguments()
  {
    var shell = new FakeShell();
    var router = new TestRouter(shell);
    var detail = new AdaptiveDetailVm();

    RouteItem item1 = new()
    {
      Title = "Detail 1",
      Destination = RouteTo(typeof(AdaptiveDetailVm), new DetailArgs(1), typeof(MainLayoutVm))
    };
    RouteItem item2 = new()
    {
      Title = "Detail 2",
      Destination = RouteTo(typeof(AdaptiveDetailVm), new DetailArgs(2), typeof(MainLayoutVm))
    };

    await router.RouteAsync(new Locator(
      [Target.Of(typeof(MainLayoutVm)), Target.Of(typeof(AdaptiveDetailVm), new DetailArgs(1), detail)]));

    Assert.True(item1.IsHighlighted(router.Stack.Location));
    Assert.False(item2.IsHighlighted(router.Stack.Location));

    // Absorption revises the live node's arguments — no new instance, no new
    // chain entry; the vote follows the realized state.
    await router.RouteAsync(new Locator(
      [Target.Of(typeof(MainLayoutVm)), Target.Of(typeof(AdaptiveDetailVm), new DetailArgs(2), detail)]));

    Assert.Same(detail, router.Stack.Location!.Instance);
    Assert.False(item1.IsHighlighted(router.Stack.Location));
    Assert.True(item2.IsHighlighted(router.Stack.Location));
  }
}
