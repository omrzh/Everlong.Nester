using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Auth;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Hosting;
using Everlong.Nester.Messaging;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using NesterApp.Models;
using NesterApp.Pages.Admin;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Login;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Settings;
using NesterApp.Properties;
using NesterApp.Services;
using System.Diagnostics;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The persistent chrome around every page in the shell's layout chain: the
///   <c>RouteItem</c> navigation tree, back / forward / refresh dispatched as
///   intents, logout (which rebuilds the login window's shell), and the body
///   chain mirrored into a display string.
/// </summary>
/// <remarks>
///   Your seam: the menu (<c>Menu</c>, declared in <c>CreateMenu</c>), and
///   <c>MainLayout.axaml</c> is the chrome.  A page that must keep a section lit
///   where it does not navigate declares it itself — see
///   <c>PostDetailPageModel.Represents</c>.
/// </remarks>
[Transient]
public partial class MainLayoutModel : RoutableModel, IBodyChanged, IMessageRecipient
{
  [Inject] private partial IAuthService AuthService { get; }

  /// <summary>The backdrop domain's broadcast — the panel toggle projects from it.</summary>
  [Inject] private partial IMessageHub MessageHub { get; }

  /// <summary>The tuner panel's visibility as last announced by the backdrop domain.</summary>
  [ObservableProperty]
  public partial bool IsBackdropPanelVisible { get; set; } = true;

  /// <summary>
  ///   Projects the menu's rail form once per instance.  A constructor rather than a
  ///   field initializer: <see cref="Menu" /> is an instance member, so the rail's
  ///   projection is built when the layout is built and follows the same locale.
  /// </summary>
  public MainLayoutModel()
  {
    RailItems = [.. Destinations(Menu)];
  }

  private IDisposable? _messageSubscription;

  /// <summary>
  ///   The membership edge — the stack retains this model, so the hub
  ///   subscription is taken once and released with the model.
  /// </summary>
  protected override void OnRoutedTo(IRoutingContext context, bool isFirstRouted)
  {
    if (isFirstRouted)
      _messageSubscription = MessageHub.Register(this);
  }

  /// <inheritdoc />
  protected override void OnReleased()
  {
    _messageSubscription?.Dispose();
    _messageSubscription = null;
  }

  bool IMessageRecipient.CanReceive(IMessage message) => message is BackdropPanelVisibilityChangedMessage;

  void IMessageRecipient.Receive(IMessage message)
  {
    if (message is BackdropPanelVisibilityChangedMessage visibility)
      IsBackdropPanelVisible = visibility.IsVisible;
  }

  /// <summary>Asks the backdrop domain to flip the tuner panel — a request, not a state write.</summary>
  [RelayCommand]
  private Task ToggleBackdropPanel()
    => Shell.DispatchIntent(this, new ToggleBackdropPanelIntent()).AsTask();

  /// <summary>
  ///   Nester's body-change hook (demo wiring — not surfaced in the UI yet):
  ///   fires when the chain below this layout changes, e.g. the sub-page
  ///   swapped inside the layout.  The full body chain (outermost first) is
  ///   handed over, so breadcrumbs, page titles or nav state can be rebuilt
  ///   from it.  This demo only mirrors the body types into
  ///   <see cref="BodySummary"/> — polish as you see fit.
  /// </summary>
  public void OnBodyChanged(IReadOnlyList<object> bodyChain)
  {
    BodySummary = string.Join(" › ", bodyChain.Select(n => n.GetType().Name));
    Debug.WriteLine($"[MainLayoutModel] body: {BodySummary}");
  }

  /// <summary>Demo state: the current body chain as a display string.</summary>
  [ObservableProperty]
  public partial string BodySummary { get; set; } = string.Empty;

  /// <summary>The router's navigation-state surface — bindable back/forward reach.</summary>
  public IRouterStack Stack => Router.Stack;

  public string LogoutLabel => Lang.Navigation.Logout;

  /// <summary>
  ///   Builds the menu — one declaration, projected differently by the layout's two forms.
  ///   A factory rather than a field: the titles are read when a layout is built, so a
  ///   locale switch that rebuilds the shell reaches the menu too.
  /// </summary>
  public static IReadOnlyList<IRouteItem> CreateMenu() =>
  [
    new RouteItem
    {
      Title = Lang.Navigation.Main, Icon = new EmojiIcon("🏠"), Children =
      [
        new RouteItem { Title = Lang.Navigation.Landing, Icon = new EmojiIcon("🚀"), Destination = new LandingLocator() },
        new RouteItem
          { Title = Lang.Navigation.InteractionLab, Icon = new EmojiIcon("✨"), Destination = new InteractionLabLocator() },
        // PostDetailPageModel claims the Posts destination, so the Posts item
        // stays lit on the detail page — including a direct (deep-link) entry.
        new RouteItem
          { Title = Lang.Navigation.Posts, Icon = new EmojiIcon("📰"), Destination = new PostsLocator() }
      ]
    },

    new RouteItem
    {
      Title = Lang.Navigation.Settings, Icon = new EmojiIcon("⚙️"),
      Destination = new SettingsLocator(),
      Children =
      [
        new RouteItem
          { Title = Lang.Navigation.Account, Icon = new EmojiIcon("👤"), Destination = new AccountSettingsLocator() },
        new RouteItem
          { Title = Lang.Navigation.Security, Icon = new EmojiIcon("🔐"), Destination = new SecuritySettingsLocator() }
      ]
    },

    new RouteItem { Title = Lang.Navigation.Profile, Icon = new EmojiIcon("👤"), Destination = new ProfileLocator() },
    new RouteItem { Title = Lang.Navigation.AdminPanel, Icon = new EmojiIcon("🛡️"), Destination = new AdminLocator() },
    new RouteItem { Title = Lang.Navigation.RouteItemLab, Icon = new EmojiIcon("🔗"), Destination = new RouteItemLabLocator() },
    new RouteItem
      { Title = Lang.Navigation.VideoPlayer, Icon = new EmojiIcon("🎬"), Destination = new VideoPlayerLocator() }
  ];

  /// <summary>The menu this layout renders — built per instance, so its titles follow the locale in force.</summary>
  public IReadOnlyList<IRouteItem> Menu { get; } = CreateMenu();

  /// <summary>The menu the docked form renders — sections with collapsible children.</summary>
  public IReadOnlyList<IRouteItem> NavItems => Menu;

  /// <summary>
  ///   The menu the rail renders: the destinations <see cref="Destinations" /> exposes.
  ///   A rail cannot express containment, so the headings it has no room for are dropped
  ///   rather than shown as entries that do nothing, and their children stay reachable.
  /// </summary>
  public IReadOnlyList<IRouteItem> RailItems { get; }

  /// <summary>Flattens a menu to the destinations it exposes, depth first.</summary>
  /// <param name="items">The menu to flatten.</param>
  public static IEnumerable<IRouteItem> Destinations(IEnumerable<IRouteItem> items)
  {
    foreach (IRouteItem item in items)
    {
      if (item.Destination is not null)
        yield return item;

      foreach (IRouteItem child in Destinations(item.Children))
        yield return child;
    }
  }

  [RelayCommand]
  private async Task Logout()
  {
    if (OperatingSystem.IsBrowser())
    {
      AuthService.Logout();
      await base.Router.RouteAsync(new LoginLocator());
      return;
    }

    await Shell.DispatchIntent(this, new HideIntent());

    // Login window — the same assembly path as the App startup, written out
    // (the logout flow rebuilds the login window's shell).  AppShell is
    // the one shell class for every shell kind; LoginWindowModel lives in
    // Template.Shared, so Shared pages can name it directly.
    var loginShell = new UiShell { DirectorType = typeof(LoginWindowModel) };
    loginShell.Start();   // the login window is synchronously presented
    AppLifetime.SetMainShell(loginShell);   // the main-shell declaration is explicit: activation intents route here (Start never promotes)
    await loginShell.Lifetime.Startup;   // wait for the first navigation to settle

    await Shell.DispatchIntent(this, new CloseIntent());
  }

  // Admin shortcut shown only to Admin-role users via n:Authorize.Visible="Admin" in XAML.
  [RelayCommand]
  private Task GoToAdmin()
  {
    return base.Router.RouteAsync(new AdminLocator());
  }

  // GoBack/GoForward mirror browser history buttons. Router.Stack.CanGoBack / CanGoForward
  // are INotifyPropertyChanged-backed booleans you can bind to button IsEnabled in XAML.
  [RelayCommand]
  private Task GoBack()
  {
    return Shell.DispatchIntent(this, new BackIntent()).AsTask();
  }

  [RelayCommand]
  private Task GoForward()
  {
    return Shell.DispatchIntent(this, new ForwardIntent()).AsTask();
  }

  [RelayCommand]
  private Task Refresh()
  {
    return Shell.DispatchIntent(this, new RefreshIntent()).AsTask();
  }
}
