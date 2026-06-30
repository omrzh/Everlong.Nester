using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Helpers;
using Everlong.Nester.Routing;
using Everlong.Nester.RouteSync;
using NesterApp.Models;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;

namespace NesterApp.Pages.Posts;

/// <summary>The typed arguments of the detail route — a record, so value equality is the identity.</summary>
public sealed record PostDetailArgs(Post Post) : Args;

/// <summary>
///   The detail half of list→detail, and the page that shows the whole lifecycle
///   written out: the load starts on the route edge (<c>OnRoutedTo</c>, the
///   earliest hook), waits a bounded budget in <c>OnArrivingAsync</c>, and is
///   applied in <c>OnArrivedAsync</c> (the guaranteed path).
/// </summary>
/// <remarks>
///   Your seam: <c>OnArgsDelivered</c> is where typed arguments become page
///   state, <c>OnDeparted</c> is where retention is driven (there is no
///   retention declaration on the route), and <c>Represents</c> is how the page
///   keeps the Posts menu item lit although it is not the Posts page.
/// </remarks>
[Transient]
[Routable<PostDetailArgs>]
[Layout<MainLayoutModel>]
public partial class PostDetailPageModel
  : ParameterizedModel<PostDetailArgs>, IArriving, IDeparted, IRouteHighlight
{
  [Inject] private partial JsonPlaceholderService Service { get; }

  /// <summary>The Posts navigation destination this detail page belongs to.</summary>
  public bool? Represents(ILocator destination)
    => destination.Path[^1].Type == typeof(PostsPageModel) ? true : null;

  public PagesStrings PagesStrings => Lang.Pages;

  [ObservableProperty] public partial Comment[] Comments { get; set; } = [];

  [ObservableProperty] public partial Post Post { get; private set; } = Post.Empty;

  // ── Self-owned preload — the framework provides no preload stage ────────────
  //
  // The pattern: fire the background load on the route edge (the earliest
  // hook — `OnRoutedTo` runs at the truth, before the arrival ceremonies),
  // wait a bounded budget for it in `OnArrivingAsync` (borrowed time before
  // the reveal), and apply the settled result in `OnArrivedAsync` (the
  // guaranteed path — awaited, idempotent apply).  One load per post
  // engagement: a different post restarts it, a re-entry reuses it.

  private Task<Comment[]>? _commentsTask;

  private int _commentsFor; // the post id the in-flight load serves

  protected override void OnRoutedTo(IRoutingContext context, bool isFirstRouted)
  {
    if (isFirstRouted && EngagedArgs is { } args
                      && (_commentsTask is null || _commentsFor != args.Post.Id))
    {
      _commentsFor = args.Post.Id;
      _commentsTask = Service.GetCommentsAsync(args.Post.Id, context.Lifetime);
    }
  }

  public async Task OnArrivingAsync(IRoutingContext context)
  {
    // Borrowed time before the reveal — wait a bounded budget; a slow load
    // degrades the arrival, never blocks it.
    if (_commentsTask is { } preload)
    {
      try
      {
        var comments = await preload.WaitAsync(300.Milliseconds, context.Lifetime);
        Comments = comments;
      }
      catch
      {
        // Timeout or supersession — the reveal proceeds; OnArrived applies
        // the load when it settles.
      }
    }
  }

  protected override async Task OnArrivedAsync(IRoutingContext context, bool isFirstArrived)
  {
    // The guaranteed path — a faulted load surfaces through the router's
    // error channel and the page keeps its previous comments.
    if (Comments is { Length: 0 } && _commentsTask is { } preload)
    {
      Comments = await preload;
    }
  }

  protected override void OnArgsDelivered()
  {
    if (EngagedArgs is { } args)
    {
      Post = args.Post;
    }
  }


  public void OnDeparted(IRoutingContext context)
  {
    // ── Retention is wired here, not declared on the route ───────────────────
    //
    // There is no route-level retention declaration anymore — the old ephemeral
    // hint is retired with no replacement attribute.  The page owns its policy
    // and drives it through `Router.Stack`: backing out drops the forward trail,
    // so Forward cannot re-enter this stale instance (the router's tree is the
    // only retention there is).
    //
    // A one-shot *surface* is the other lever, and it is derived explicitly —
    // a capsule that must take one route request and hand every later one off
    // instead of stacking it is `router.Derive(isEphemeral: true)` (what
    // `PresentOnDerivedAsync` does for dialogs).
    if (context.Direction == RoutingDirection.Back)
    {
      Router.Stack.TrimForward();
    }
  }
}
