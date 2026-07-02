using Everlong.Nester.Presentation;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace NesterApp.Pages.Labs;

[ViewFor<RouteItemLabPageModel>]
public partial class RouteItemLabPage : UserControl, ISceneTransition
{
  public RouteItemLabPage()
  {
    InitializeComponent();
  }

  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    return context.EnterWithSlideAsync(SlideDirection.BottomToTop, 100, 100, token);
  }

  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    return Task.CompletedTask;
  }
}

/// <summary>
///   Converts a <see cref="SvgIcon.PathData" /> string to a WPF <see cref="Geometry" />.
///   Registered as a static resource in RouteItemLabPage.xaml and used by the SvgIcon DataTemplate.
/// </summary>
internal sealed class SvgIconConverter : IValueConverter
{
  public static readonly SvgIconConverter Instance = new();

  public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    return value is string s ? Geometry.Parse(s) : null;
  }

  public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotSupportedException();
  }
}
