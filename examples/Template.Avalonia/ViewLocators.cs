using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Everlong.Nester.Presentation;
using NesterApp.Models;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Login;
using NesterApp.Pages.Posts;

namespace NesterApp;

/// <summary>
/// [ViewLocator] triggers source generation to create a View-Factory that resolves
/// Views for ViewModels. There are two ways to register a view:
///
///   1. [ViewFor&lt;TViewModel&gt;] on the View code-behind (decentralized — view declares its VM).
///   2. [Mapping&lt;TViewModel, TView&gt;] on this class (centralized — ViewLocator owns all mappings).
///
/// Both approaches produce identical generated code. Use [Mapping] when the View lives in a
/// different assembly, or when you prefer all wiring visible in one place.
/// </summary>
[ViewLocator]
// Explicit mapping for VideoPlayerPage: the view's code-behind does NOT carry [ViewFor].
// This demonstrates the centralized [Mapping] alternative — useful for cross-assembly views
// or when you want every ViewModel-to-View relationship declared in one file.
[Mapping<VideoPlayerPageModel, VideoPlayerPage>]
// LoginPage (UserControl) is the reusable login form used in both the desktop LoginWindow
// (thin Window shell) and the browser scope (via LoginRouteViewModel navigation).
[Mapping<LoginPageModel, LoginPage>]
public partial class ViewLocator;

/// <summary>
/// To customize the view resolution logic.  A locator builds: Build returns a new
/// control for the data it is given, so a presenter re-pointed at other data shows
/// that data's view — never the one it showed before.
/// </summary>
public class ViewLocatorHook : IViewLocator
{
  public bool Match(object? data) => data is PostDetailPageModel or IShape or List<IShape> or EmojiIcon;

  public Control? Build(object? data)
  {
    return data switch
    {
      PostDetailPageModel => new PostDetailPage(),
      // EmojiIcon is a data record like the shapes below — the control is built
      // here rather than mapped to a view, because the "view" is one text line.
      EmojiIcon icon => new TextBlock { Text = icon.Glyph },
      // example: subtypes => different view
      Circle c => new Border
      {
        Background = Brushes.Blue,
        Margin = new Thickness(4),
        Width = c.Radius * 2,
        Height = c.Radius * 2,
        CornerRadius = new CornerRadius(c.Radius),
      },
      Rectangle r => new Border
      {
        Margin = new Thickness(4),
        Background = Brushes.Pink,
        Width = r.Width,
        Height = r.Height,
      },
      // more complicated example, generic type can be handled
      List<IShape> circles => new ItemsControl()
      {
        ItemsSource = circles
      },
      _ => null
    };
  }
}
