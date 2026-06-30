using Everlong.Nester.Presentation;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
namespace NesterApp.Pages.Labs;

[ViewFor<RouteItemLabPageModel>]
public partial class RouteItemLabPage : UserControl, ISceneTransition
{
  public RouteItemLabPage() => InitializeComponent();

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => context.ExitWithSlideAsync(SlideDirection.BottomToTop, 50, 100, token);
}

/// <summary>
///   Converts a <see cref="SvgIcon.PathData"/> string to an Avalonia <see cref="Geometry"/>.
///   Registered as a static resource in RouteItemLabPage.axaml and used by the SvgIcon DataTemplate.
/// </summary>
internal sealed class SvgIconConverter : IValueConverter
{
  public static readonly SvgIconConverter Instance = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    => value is string s ? Geometry.Parse(s) : null;

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    => throw new NotSupportedException();
}
