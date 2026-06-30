using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using NesterApp.Models;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace NesterApp.Pages.Posts;

/// <summary>
///   The list half of list→detail: arrival-time loading through <c>IArrived</c>,
///   a ground navigation to the parameterized detail route, and the same page
///   presented in an overlay on a derived router.
/// </summary>
/// <remarks>
///   Your seam: <c>OnArrivedAsync</c> is where a page loads its data;
///   <c>Router.RouteAsync</c> and <c>Router.Derive()</c> are the two ways to
///   leave it.
/// </remarks>
[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class PostsPageModel : RoutableModel
{
  [Inject] private partial JsonPlaceholderService ApiService { get; }
  public PagesStrings PagesStrings => Lang.Pages;

  [ObservableProperty] public partial ObservableCollection<Post> Posts { get; private set; } = [];

  public Post? SelectedPost { get; private set; }

  protected override async Task OnArrivedAsync(IRoutingContext context, bool isFirstArrived)
  {
    if (!isFirstArrived && Posts is not { Count: 0 })
    {
      return;
    }

    Debug.WriteLine("Posts: OnArrived loading");
    Post[] result = await ApiService.GetPostsAsync(context.Lifetime);
    if (result is { Length: > 0 })
    {
      Posts = [.. result];
    }
  }

  [RelayCommand]
  private Task OpenPost(Post post)
  {
    SelectedPost = post;
    // Ground navigation is fire-and-forget: RouteAsync commits the route,
    // no result is bridged back.
    return Router.RouteAsync(new PostDetailLocator(new PostDetailArgs(post)));
  }

  [RelayCommand]
  private Task OpenInDialog(Post post)
  {
    PostDetailArgs args = new(post);

    // Derived-router presentation: PostDetail opens in an overlay over the
    // dimmer + chrome chain (its own layer, its own chrome) — the dimmer is
    // non-light-dismiss, so the overlay closes by back alone.
    IRouter result = Router.Derive();
    return result.RouteAsync(new Locator(
      [
        new DefaultDimmerModel { LightDismiss = true },
        Target.Of(typeof(PostDetailChromeLayoutModel)),
        Target.Of(typeof(PostDetailPageModel), args)
      ]));
  }

  [RelayCommand]
  private Task Refresh()
  {
    // Refresh is an intent: the router replays the arrival over the current
    // chain — no new entry, the current instance reloads.
    return Shell.DispatchIntent(this, new RefreshIntent()).AsTask();
  }
}
