using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.DI;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Routing;
using NesterApp.Pages.Shell;
using NesterApp.Services;

namespace NesterApp.Pages.Labs;

[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class VideoPlayerPageModel : RoutableModel,
                                            IDeparted,
                                            IReleasable
{
  [Inject] private partial FloatingPlayerHost FloatingPlayer { get; }

  private CancellationTokenSource? _playCts;

  [ObservableProperty] public partial double PlaybackPosition { get; set; }
  [ObservableProperty] public partial bool IsPlaying { get; set; }
  [ObservableProperty] public partial string? VideoTitle { get; set; } = "Titanic";

  public void OnDeparted(IRoutingContext context)
  {
    IsPlaying = false;
    StopPlayLoop();

    // Picture-in-picture: hand the playback over to the floating player
    // (Overlay floor) so it keeps playing while this page is gone.
    if (VideoTitle is { Length: > 0 } title)
    {
      FloatingPlayer.Show(title, PlaybackPosition);
    }
  }

  protected override Task OnArrivedAsync(IRoutingContext context, bool isFirstArrived)
  {
    // Back on stage — take playback back from the floating player.
    FloatingPlayer.Close();

    if (!IsPlaying)
    {
      IsPlaying = true;
      StartPlayLoop();
    }

    return Task.CompletedTask;
  }

  public void Release()
  {
    IsPlaying = false;
    StopPlayLoop();
    FloatingPlayer.Close();
    // VideoDecoder?.Dispose();
  }

  /// <summary>
  ///   Mock playback: advances the position while <see cref="IsPlaying" />.
  /// </summary>
  private void StartPlayLoop()
  {
    _playCts?.Cancel();
    _playCts = new CancellationTokenSource();
    _ = PlayLoopAsync(_playCts.Token);
  }

  private void StopPlayLoop()
  {
    _playCts?.Cancel();
    _playCts = null;
  }

  private async Task PlayLoopAsync(CancellationToken token)
  {
    try
    {
      while (IsPlaying)
      {
        await Task.Delay(100, token);
        PlaybackPosition += 0.1;
      }
    }
    catch (OperationCanceledException)
    {
      // Stopped.
    }
  }
}
