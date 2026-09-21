using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Everlong.Nester.Presentation;
using NesterApp.Models;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Posts;
using NesterApp.Views;

namespace NesterApp;

/// <summary>
///   [WpfViewLocator] triggers source generation for the WPF View-Factory.
///   [Mapping&lt;TViewModel, TView&gt;] on this class provides explicit, centralized view registration —
///   an alternative to placing [ViewFor&lt;TViewModel&gt;] on each view's code-behind.
///   Use [Mapping] for cross-assembly views or when you prefer all wiring visible here.
/// </summary>
[WpfViewLocator]
[Mapping<AlertMessage, AlertView>]
// Explicit mapping demo: VideoPlayerPage declares no [ViewFor] — the link lives here.
[Mapping<VideoPlayerPageModel, VideoPlayerPage>]
internal partial class ViewLocator;

/// <summary>
///   Custom view locator that handles instance-level routing logic.
/// </summary>
internal sealed class DynamicViewLocator : ViewLocatorBase
{
  public DynamicViewLocator()
  {
    EnableDynamicLocating(this);
    AddDynamicTemplate<PostDetailPageModel>();
    AddDynamicTemplate<Circle>();
    AddDynamicTemplate<Rectangle>();
    AddDynamicTemplate<List<IShape>>();
  }

  public override bool Match(object? data)
    => data is PostDetailPageModel or Circle or Rectangle or List<IShape>;

  public override FrameworkElement? Build(object? data)
  {
    return data switch
    {
      PostDetailPageModel => new PostDetailPage(),
      // The shapes are data records; the control is built here rather than
      // mapped to a view, because the "view" is a few property assignments.
      // EmojiIcon is the same kind of record and is declared as an implicit
      // DataTemplate in App.xaml (a per-item view must not pay for the
      // ContentControl that dynamic locating has to insert).
      Circle c => new Border
      {
        Background = Brushes.Blue,
        Margin = new Thickness(4),
        Width = c.Radius * 2,
        Height = c.Radius * 2,
        CornerRadius = new CornerRadius(c.Radius)
      },
      Rectangle r => new Border
      {
        Margin = new Thickness(4),
        Background = Brushes.Pink,
        Width = r.Width,
        Height = r.Height
      },
      List<IShape> shapes => new ItemsControl { ItemsSource = shapes },
      _ => null
    };
  }
}
