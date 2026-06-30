using Everlong.Nester.Presentation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
namespace NesterApp.Pages.Posts;

[ViewFor<PostDetailPageModel>]
public partial class PostDetailPage : UserControl, ISceneTransition
{
  public PostDetailPage() => InitializeComponent();


  /// <summary>
  /// Entrance animation when navigating into the detail page.
  /// If the departing page exposes the source list item container, the title performs a
  /// shared-element fly-in; otherwise it falls back to rising from below.
  /// Body text, comments header, and comments section follow with an 80 ms staggered rise.
  /// </summary>
  /// <param name="ctx">Effect context providing the flying canvas and animation primitives.</param>
  /// <param name="token">
  /// Cancellation token. If canceled mid-flight, a <c>finally</c> block guarantees
  /// all element opacities are restored to 1.
  /// </param>
  /// <remarks>
  /// The shared-element path depends on a direct call to <see cref="PostsPage.PostsList"/>.
  /// This coupling is intentionally confined to this method; the ViewModel layer remains unaware of it.
  /// <para>
  /// After the first invocation completes, a <see cref="GhostFlyItem"/> is fired asynchronously
  /// at near-zero opacity to pre-warm its GPU shaders, preventing a JIT hitch on the first real
  /// exit animation. The warmup runs exactly once, guarded by <see cref="_shaderWarmupDone"/>
  /// via <see cref="System.Threading.Interlocked.CompareExchange"/>.
  /// </para>
  /// </remarks>
  public async Task AnimateEnterAsync(TransitionContext ctx, CancellationToken token)
  {
    Rect? sourceRect = null;
    if (DataContext is PostDetailPageModel { Post: { } post } &&
        ctx.DepartingHead is PostsPage { PostsList: { } postsList } &&
        postsList.ContainerFromItem(post) is { } container)
    {
      sourceRect = ctx.CaptureRelativeRect(container);
    }

    const int durationMs = 600;
    const int stagger = 80;
    try
    {
      HeroTitle.Opacity = CommentsHeader.Opacity = CommentsSection.Opacity = 0;
      var hero = sourceRect is { } sr
                   ? ctx.FlyIn(HeroTitle, sr, durationMs)
                   : ctx.RiseIn(HeroTitle, offsetY: 150, durationMs);
      var body = ctx.RiseIn(BodyText, offsetY: 20, durationMs, delayMs: stagger);
      var header = ctx.RiseIn(CommentsHeader, offsetY: 30, durationMs, delayMs: stagger * 2);
      var comments = ctx.RiseIn(CommentsSection, offsetY: 40, durationMs, delayMs: stagger * 3);

      ctx.HideDeparting();
      ctx.ShowArriving();

      await Task.WhenAll(hero(token), body(token), header(token), comments(token));
    }
    finally
    {
      HeroTitle.Opacity = CommentsHeader.Opacity = CommentsSection.Opacity = 1;
    }

    if (Interlocked.CompareExchange(ref _shaderWarmupDone, 1, 0) == 0)
    {
      // Pre-warm GhostFlyItem shaders so the real exit animation doesn't stutter on first run.
      _ = new GhostFlyItem(
        new Rect(5, 5, 10, 10),
        new Rect(10, 10, 15, 15),
        TimeSpan.FromMilliseconds(150))
      {
        StartOpacity = 0.03,
        EndOpacity = 0.01,
      }.FlyAsync(ctx.FlyingCanvas, CancellationToken.None);
    }
  }
  private static int _shaderWarmupDone; // 0 = no, 1 = yes

  /// <summary>
  /// Exit animation when navigating back to the preceding page.
  /// Deploys a <see cref="GhostFlyItem"/> decoy on the top-level canvas to simulate
  /// the current page flying back into its originating list item.
  /// Falls back to a standard downward slide if the arriving page is not a
  /// <see cref="PostsPage"/> or no matching container is found.
  /// </summary>
  /// <param name="ctx">Effect context.</param>
  /// <param name="token">Cancellation token.</param>
  /// <remarks>
  /// The ghost is launched from the current page's bounding rect and flies to the
  /// target container's rect over 400 ms.
  /// <para>
  /// Unlike the WPF counterpart — where restoring <c>container.Opacity</c> immediately
  /// after <c>FlyAsync</c> produces a gapless handoff — Avalonia's render dispatch
  /// introduces a noticeable visible gap between the ghost's last frame and the
  /// container becoming visible. To mask this, the container is restored 300 ms into
  /// the flight while the ghost still covers it. The only hard constraint is that the
  /// restore delay must be less than the ghost duration.
  /// </para>
  /// </remarks>
  public async Task AnimateExitAsync(TransitionContext ctx, CancellationToken token)
  {
    if (ctx.ArrivingHead is not PostsPage { PostsList: { } postsList } ||
        DataContext is not PostDetailPageModel { Post: { } post } ||
        postsList.ContainerFromItem(post) is not { } container)
    {
      await ctx.ExitWithSlideAsync(SlideDirection.TopToBottom, 50, 150, token);
      return;
    }

    var sourceRect = ctx.CaptureRelativeRect(this);
    ctx.HideDeparting();
    container.Opacity = 0;
    ctx.ShowArriving();

    DispatcherTimer.RunOnce(() => container.Opacity = 1, TimeSpan.FromMilliseconds(300), DispatcherPriority.Send);
    await new GhostFlyItem(sourceRect, ctx.CaptureRelativeRect(container))
    {
      Duration = TimeSpan.FromMilliseconds(400),
    }.FlyAsync(ctx.FlyingCanvas, token);
  }
}
