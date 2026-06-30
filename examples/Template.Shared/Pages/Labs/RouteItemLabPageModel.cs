using Everlong.Nester.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Routing;
using Everlong.Nester.RouteSync;
using NesterApp.Pages.Admin;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using NesterApp.Models;

namespace NesterApp.Pages.Labs;

/// <summary>Simple POCO for demonstrating <c>TreeControl</c> with hierarchical data templates.</summary>
public sealed record TreeNode(string Label, IReadOnlyList<TreeNode>? Children = null);

/// <summary>
///   Custom icon type used in Section D to demonstrate Pattern B icon customization.
///   Holds a raw SVG path-data string; rendering is handled by a <c>DataTemplate</c> in XAML.
///   The view model stays platform-agnostic — it never references UI types.
/// </summary>
public sealed record SvgIcon(string PathData);

[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class RouteItemLabPageModel : RoutableModel
{
  public LabsStrings LabsStrings => Lang.Labs;

  /// <summary>
  ///   Flat menu items for Section A — demonstrates the existing ItemsControl + RouteLink approach.
  /// </summary>
  public IReadOnlyList<IRouteItem> FlatNavItems { get; } =
  [
    new RouteItem { Title = Lang.Navigation.Landing, Icon = new EmojiIcon("🏠"), Destination = new LandingLocator() },
    new RouteItem
      { Title = Lang.Navigation.InteractionLab, Icon = new EmojiIcon("🧪"), Destination = new InteractionLabLocator() },
    new RouteItem { Title = Lang.Navigation.Posts, Icon = new EmojiIcon("📰"), Destination = new PostsLocator() },
    new RouteItem { Title = Lang.Navigation.Profile, Icon = new EmojiIcon("👤"), Destination = new ProfileLocator() },
    new RouteItem { Title = Lang.Navigation.Settings, Icon = new EmojiIcon("⚙️"), Destination = new SettingsLocator() },
    new RouteItem { Title = Lang.Navigation.AdminPanel, Icon = new EmojiIcon("🛡️"), Destination = new AdminLocator() },
    new RouteItem { Title = Lang.Navigation.RouteItemLab, Icon = new EmojiIcon("🔗"), Destination = new RouteItemLabLocator() }
  ];

  /// <summary>
  ///   Tree-structured menu items for Section B — demonstrates RouteTreeControl + RouteTreeItem dispatch.
  ///   Includes one pure section group and one settings group.
  /// </summary>
  public IReadOnlyList<IRouteItem> TreeNavItems { get; } =
  [
    new RouteItem
    {
      Title = Lang.Navigation.Main,
      Children =
      [
        new RouteItem { Title = Lang.Navigation.Landing, Icon = new EmojiIcon("🏠"), Destination = new LandingLocator() },
        new RouteItem
          { Title = Lang.Navigation.InteractionLab, Icon = new EmojiIcon("🧪"), Destination = new InteractionLabLocator() },
        new RouteItem { Title = Lang.Navigation.Posts, Icon = new EmojiIcon("📰"), Destination = new PostsLocator() }
      ]
    },

    new RouteItem
    {
      Title = Lang.Navigation.Settings, Icon = new EmojiIcon("⚙️"),
      Children =
      [
        new RouteItem { Title = Lang.Navigation.General, Icon = new EmojiIcon("⚙️"), Destination = new SettingsLocator() },
        new RouteItem
          { Title = Lang.Navigation.Account, Icon = new EmojiIcon("👤"), Destination = new AccountSettingsLocator() },
        new RouteItem
          { Title = Lang.Navigation.Security, Icon = new EmojiIcon("🔐"), Destination = new SecuritySettingsLocator() }
      ]
    },

    new RouteItem { Title = Lang.Navigation.Profile, Icon = new EmojiIcon("👤"), Destination = new ProfileLocator() },
    new RouteItem { Title = Lang.Navigation.AdminPanel, Icon = new EmojiIcon("🛡️"), Destination = new AdminLocator() },
    new RouteItem { Title = Lang.Navigation.RouteItemLab, Icon = new EmojiIcon("🔗"), Destination = new RouteItemLabLocator() }
  ];

  /// <summary>
  ///   Section D nav items — demonstrates Pattern B icon customization.
  ///   <see cref="SvgIcon" /> is a plain C# record; a <c>DataTemplate</c> in the XAML
  ///   maps it to a <c>PathIcon</c> / <c>Path</c> element.  The framework and this
  ///   view model are completely unaware of the rendering details.
  /// </summary>
  public IReadOnlyList<IRouteItem> SvgIconNavItems { get; } =
  [
    new RouteItem
    {
      Title = Lang.Navigation.Landing,
      Icon = new SvgIcon("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"),
      Destination = new LandingLocator()
    },
    new RouteItem
    {
      Title = Lang.Navigation.Posts,
      Icon = new SvgIcon(
        "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"),
      Destination = new PostsLocator()
    },
    new RouteItem
    {
      Title = Lang.Navigation.Profile,
      Icon = new SvgIcon(
        "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"),
      Destination = new ProfileLocator()
    },
    new RouteItem
    {
      Title = Lang.Navigation.Settings,
      Icon = new SvgIcon(
        "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54C14.46 2.18 14.25 2 14 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1c-.23-.09-.49 0-.61.22L2.74 8.87c-.13.22-.07.49.12.64L4.57 11c-.04.34-.07.67-.07 1s.03.65.07.97l-2.11 1.66c-.19.15-.25.42-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1.01c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.58 1.69-.98l2.49 1.01c.22.08.49 0 .61-.22l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.66zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z"),
      Destination = new SettingsLocator()
    },
    new RouteItem
    {
      Title = Lang.Navigation.RouteItemLab,
      Icon = new SvgIcon(
        "M3.9 12c0-1.71 1.39-3.1 3.1-3.1h4V7H7c-2.76 0-5 2.24-5 5s2.24 5 5 5h4v-1.9H7c-1.71 0-3.1-1.39-3.1-3.1zM8 13h8v-2H8v2zm9-6h-4v1.9h4c1.71 0 3.1 1.39 3.1 3.1s-1.39 3.1-3.1 3.1h-4V17h4c2.76 0 5-2.24 5-5s-2.24-5-5-5z"),
      Destination = new RouteItemLabLocator()
    }
  ];

  /// <summary>
  ///   Hierarchical POCO nodes for Section C — demonstrates pure <c>TreeControl</c> without navigation.
  /// </summary>
  public IReadOnlyList<TreeNode> TreeControlNodes { get; } =
  [
    new("🗂 Documents",
    [
      new TreeNode("📁 Work",
      [
        new TreeNode("📄 Report Q1"),
        new TreeNode("📄 Report Q2")
      ]),
      new TreeNode("📁 Personal",
      [
        new TreeNode("📄 Resume"),
        new TreeNode("📄 Notes")
      ])
    ]),
    new("🗂 Projects",
    [
      new TreeNode("📁 Nester",
      [
        new TreeNode("📄 README"),
        new TreeNode("📄 Changelog")
      ]),
      new TreeNode("📁 Archive")
    ]),
    new("⚙ Settings")
  ];
}
