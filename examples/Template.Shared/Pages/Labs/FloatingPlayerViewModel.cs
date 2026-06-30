using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NesterApp.Properties;
using System.Diagnostics;

namespace NesterApp.Pages.Labs;

/// <summary>
///   Content model of the floating picture-in-picture player — shown on the
///   Overlay floor while the video page is departed.  The view is resolved
///   by the app's DataTemplates ([ViewFor] mapping); the playback loop is a
///   mock that keeps the position advancing while <see cref="IsPlaying" />.
/// </summary>
public sealed partial class FloatingPlayerViewModel : ObservableObject
{
  private readonly CancellationTokenSource _cts = new();

  [ObservableProperty]
  public partial string? VideoTitle { get; set; }

  /// <summary>Playback status label, localized.</summary>
  public string NowPlaying => Lang.Labs.NowPlaying;

  [ObservableProperty]
  public partial double PlaybackPosition { get; set; }

  [ObservableProperty]
  public partial bool IsPlaying { get; set; }

  /// <summary>
  ///   Invoked when the view's close button is clicked — the host releases
  ///   the Overlay lease.
  /// </summary>
  public Action? CloseRequested { get; set; }

  [RelayCommand]
  private void Close()
  {
    CloseRequested?.Invoke();
  }

  /// <summary>
  ///   Starts the mock playback loop.  Runs until <see cref="IsPlaying" />
  ///   turns <see langword="false" /> or <see cref="Stop" /> is called.
  /// </summary>
  public void StartPlaybackLoop()
  {
    _ = PlayLoopAsync(_cts.Token);
  }

  /// <summary>
  ///   Stops the mock playback loop.
  /// </summary>
  public void Stop()
  {
    _cts.Cancel();
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
      Debug.WriteLine("FloatingPlayerViewModel: Playback loop canceled.");
      // Stopped.
    }
  }
}
