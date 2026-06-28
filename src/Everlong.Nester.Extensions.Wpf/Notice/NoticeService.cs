using System.Collections;
using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Notice;

/// <summary>
///   The WPF notice tenant — rents the notice band and hands every show
///   call to the <see cref="NoticeEngine" />.
/// </summary>
internal sealed class NoticeService(ILayerBroker broker, IShell shell, NoticeServiceOptions options,
                                    IViewLocator<PControl>? viewLocator = null)
  : NoticeServiceBase(broker, new NoticeEngine(viewLocator ?? shell.GetPlatformService<IViewLocator<PControl>>()
      ?? throw new InvalidOperationException(
        "The shell provides no view locator — GetPlatformService<IViewLocator<T>>() is not implemented.")), options)
{
  protected override object CreateHost()
  {
    var host = new PGrid();
    host.Children.Add(BuildStack(Engine.ToastEntries, Options.ToastPosition, Options.ToastMargin));
    host.Children.Add(BuildStack(Engine.SnackbarEntries, Options.SnackbarPosition, Options.SnackbarMargin));
    host.Children.Add(BuildStack(Engine.BannerEntries, Options.NotificationPosition, Options.NotificationMargin));
    return host;
  }

  private static PItemsControl BuildStack(IEnumerable items, NoticePosition position,
                                                 Primitives.Thickness margin)
  {
    // Convert to the platform thickness at the framework boundary — user code
    // configures NoticeServiceOptions with the platform-agnostic type.
    PThickness platformMargin = new(margin.Left, margin.Top, margin.Right, margin.Bottom);
    var (horizontal, vertical) = position switch
    {
      NoticePosition.TopLeft => (PHorizontalAlignment.Left, PVerticalAlignment.Top),
      NoticePosition.TopCenter => (PHorizontalAlignment.Center, PVerticalAlignment.Top),
      NoticePosition.TopRight => (PHorizontalAlignment.Right, PVerticalAlignment.Top),
      NoticePosition.BottomLeft => (PHorizontalAlignment.Left, PVerticalAlignment.Bottom),
      NoticePosition.BottomCenter => (PHorizontalAlignment.Center, PVerticalAlignment.Bottom),
      NoticePosition.BottomRight => (PHorizontalAlignment.Right, PVerticalAlignment.Bottom),
      _ => throw new ArgumentOutOfRangeException(nameof(position))
    };
    return new PItemsControl
    {
      ItemsSource = items,
      HorizontalAlignment = horizontal,
      VerticalAlignment = vertical,
      Margin = platformMargin
    };
  }
}
