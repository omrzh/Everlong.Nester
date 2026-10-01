// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using System.Collections.ObjectModel;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Notice;

/// <summary>
///   The default notice panel — one region per channel (toast / snackbar /
///   notification), anchored and spaced by the panel model, each entry
///   presented with the channel's default scene.
/// </summary>
/// <remarks>
///   A view resolved from <see cref="NoticePanelModel" /> through the
///   application's template table.  The entry's own
///   <see cref="ISceneTransition" /> wins when it implements one; otherwise
///   the panel animates it: a vertical slide for toast and snackbar, from the
///   anchored side for a notification, both with a fade.
/// </remarks>
public sealed class NoticePanel : PGrid, INoticePanel
{
  private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(450);
  private static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(400);
  private const double Offset = 60;

  private readonly ObservableCollection<PControl> _toastItems = [];
  private readonly ObservableCollection<PControl> _snackbarItems = [];
  private readonly ObservableCollection<PControl> _notificationItems = [];

  private readonly PItemsControl _toast = new();
  private readonly PItemsControl _snackbar = new();
  private readonly PItemsControl _notification = new();

  private NoticePosition _toastPosition = NoticePosition.TopCenter;
  private NoticePosition _snackbarPosition = NoticePosition.BottomCenter;
  private NoticePosition _notificationPosition = NoticePosition.TopRight;

  /// <summary>Initializes the panel with its three empty regions.</summary>
  public NoticePanel()
  {
    _toast.ItemsSource = _toastItems;
    _snackbar.ItemsSource = _snackbarItems;
    _notification.ItemsSource = _notificationItems;
    Children.Add(_toast);
    Children.Add(_snackbar);
    Children.Add(_notification);
  }

  /// <inheritdoc />
  public void AttachModel(NoticePanelModel model)
  {
    _toastPosition = model.ToastPosition;
    _snackbarPosition = model.SnackbarPosition;
    _notificationPosition = model.NotificationPosition;
    Place(_toast, model.ToastPosition, model.ToastMargin);
    Place(_snackbar, model.SnackbarPosition, model.SnackbarMargin);
    Place(_notification, model.NotificationPosition, model.NotificationMargin);
  }

  /// <inheritdoc />
  public async Task PresentAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token)
  {
    ObservableCollection<PControl> items = ItemsOf(channel);
    PControl view = ViewResolution.Build(this, entry) ?? new PTextBlock { Text = "Cannot resolve notice view" };
    view.DataContext = entry;
    view.Opacity = 0;
    view.IsHitTestVisible = false;
    items.Add(view);

    try
    {
      if (view is ISceneTransition director)
        await director.AnimateEnterAsync(new TransitionContext(null, TransitionKind.Enter) { Arriving = view }, token);
      else
        await EnterAsync(view, channel, token);
    }
    catch (OperationCanceledException)
    {
      // Preempted — the entry still lands visibly.
    }
    catch (Exception)
    {
      // A broken scene must not strand the entry off screen.
    }
    finally
    {
      view.Opacity = 1;
      view.IsHitTestVisible = true;
    }
  }

  /// <inheritdoc />
  public async Task DismissAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token)
  {
    ObservableCollection<PControl> items = ItemsOf(channel);
    PControl? view = items.FirstOrDefault(v => ReferenceEquals(v.DataContext, entry));
    if (view is null)
      return;

    try
    {
      if (view is ISceneTransition director)
        await director.AnimateExitAsync(new TransitionContext(null, TransitionKind.Dismiss) { Departing = view }, token);
      else
        await ExitAsync(view, channel, token);
    }
    catch (OperationCanceledException)
    {
      // Preempted — the entry still leaves.
    }
    catch (Exception)
    {
      // A broken scene must not leak the entry on screen.
    }
    finally
    {
      items.Remove(view);
    }
  }

  private Task EnterAsync(PControl view, NoticeChannel channel, CancellationToken token)
    => Task.WhenAll(
      TransitionEffects.SlideInAsync(view, EnterDirection(channel), Offset, (int)EnterDuration.TotalMilliseconds, token),
      TransitionEffects.FadeInAsync(view, (int)EnterDuration.TotalMilliseconds, token));

  private Task ExitAsync(PControl view, NoticeChannel channel, CancellationToken token)
    => Task.WhenAll(
      TransitionEffects.SlideOutAsync(view, Opposite(EnterDirection(channel)), Offset, (int)ExitDuration.TotalMilliseconds, token),
      TransitionEffects.FadeOutAsync(view, (int)ExitDuration.TotalMilliseconds, token));

  private SlideDirection EnterDirection(NoticeChannel channel)
    => channel is NoticeChannel.Notification
      ? PositionOf(channel) is NoticePosition.TopLeft or NoticePosition.BottomLeft
        ? SlideDirection.LeftToRight
        : SlideDirection.RightToLeft
      : PositionOf(channel) is NoticePosition.TopLeft or NoticePosition.TopCenter or NoticePosition.TopRight
        ? SlideDirection.TopToBottom
        : SlideDirection.BottomToTop;

  private static SlideDirection Opposite(SlideDirection direction)
    => direction switch
    {
      SlideDirection.BottomToTop => SlideDirection.TopToBottom,
      SlideDirection.TopToBottom => SlideDirection.BottomToTop,
      SlideDirection.LeftToRight => SlideDirection.RightToLeft,
      _ => SlideDirection.LeftToRight,
    };

  private NoticePosition PositionOf(NoticeChannel channel)
    => channel switch
    {
      NoticeChannel.Toast => _toastPosition,
      NoticeChannel.Snackbar => _snackbarPosition,
      _ => _notificationPosition,
    };

  private ObservableCollection<PControl> ItemsOf(NoticeChannel channel)
    => channel switch
    {
      NoticeChannel.Toast => _toastItems,
      NoticeChannel.Snackbar => _snackbarItems,
      _ => _notificationItems,
    };

  private static void Place(PItemsControl region, NoticePosition position, Primitives.Thickness margin)
  {
    PThickness platformMargin = new(margin.Left, margin.Top, margin.Right, margin.Bottom);
    var (horizontal, vertical) = position switch
    {
      NoticePosition.TopLeft => (PHorizontalAlignment.Left, PVerticalAlignment.Top),
      NoticePosition.TopCenter => (PHorizontalAlignment.Center, PVerticalAlignment.Top),
      NoticePosition.TopRight => (PHorizontalAlignment.Right, PVerticalAlignment.Top),
      NoticePosition.BottomLeft => (PHorizontalAlignment.Left, PVerticalAlignment.Bottom),
      NoticePosition.BottomCenter => (PHorizontalAlignment.Center, PVerticalAlignment.Bottom),
      NoticePosition.BottomRight => (PHorizontalAlignment.Right, PVerticalAlignment.Bottom),
      _ => throw new ArgumentOutOfRangeException(nameof(position)),
    };
    region.HorizontalAlignment = horizontal;
    region.VerticalAlignment = vertical;
    region.Margin = platformMargin;
  }
}
