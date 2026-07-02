using Everlong.Nester.Presentation;
using System.Windows;
using System.Windows.Controls;
namespace NesterApp.Pages.Posts;

[ViewFor<PostDetailPageModel>]
public partial class PostDetailPage : UserControl, ISceneTransition
{
  public PostDetailPage()
  {
    InitializeComponent();
  }

  public async Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    Rect? sourceRect = null;
    if (DataContext is PostDetailPageModel { Post: { } post } &&
        ctx.DepartingHead is PostsPage { PostList: { } postsList } &&
        postsList.ItemContainerGenerator.ContainerFromItem(post) is FrameworkElement container)
    {
      sourceRect = ctx.CaptureRelativeRect(container);
    }

    const int durationMs = 600;
    const int stagger = 80;

    Func<CancellationToken, Task> hero = sourceRect is { } sr
                                           ? ctx.FlyIn(HeroTitle, sr, durationMs)
                                           : ctx.RiseIn(HeroTitle, 150, durationMs);
    Func<CancellationToken, Task> body = ctx.RiseIn(BodyText, 20, durationMs, stagger);
    Func<CancellationToken, Task> comments = ctx.RiseIn(CommentsSection, 30, durationMs, stagger * 2);

    ctx.HideDeparting();
    ctx.ShowArriving();

    await Task.WhenAll(hero(token), body(token), comments(token));
  }

  public async Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
  {
    if (ctx.ArrivingHead is not PostsPage { PostList: { } postsList } ||
        DataContext is not PostDetailPageModel { Post: { } post } ||
        postsList.ItemContainerGenerator.ContainerFromItem(post) is not FrameworkElement container)
    {
      await ctx.ExitWithSlideAsync(SlideDirection.TopToBottom, 50, 50, token);
      return;
    }

    Rect sourceRect = ctx.CaptureRelativeRect(this);
    ctx.HideDeparting();
    container.Opacity = 0;
    ctx.ShowArriving();
    await new GhostFlyItem(sourceRect, ctx.CaptureRelativeRect(container)).FlyAsync(ctx, token);
    container.Opacity = 1;
  }
}
