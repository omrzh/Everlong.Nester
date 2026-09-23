using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Messaging;
using Everlong.Nester.Hosting;
using Everlong.Nester.Routing;
using NesterApp.Pages.Admin;
using NesterApp.Pages.Labs;
using NesterApp.Pages.Posts;
using NesterApp.Pages.Profile;
using NesterApp.Pages.Settings;
using NesterApp.Pages.Shell;
using NesterApp.Properties;
using System.Diagnostics;

namespace NesterApp.Pages.Landing;

/// <summary>
///   The smallest complete page — a routed model plus its view.  Copy these
///   three files (<c>LandingPageModel.cs</c>, <c>LandingPage.axaml</c>,
///   <c>LandingPage.axaml.cs</c>) for a new page: the model keeps
///   <c>[Routable]</c> + <c>[Layout&lt;MainLayoutModel&gt;]</c>, the view keeps
///   <c>[ViewFor&lt;LandingPageModel&gt;]</c>, and that is the whole wiring.
/// </summary>
[Routable]
[Layout<MainLayoutModel>]
[Transient]
public partial class LandingPageModel : RoutableModel, IMessageRecipient
{
  public PagesStrings PagesStrings => Lang.Pages;

  /// <summary>The workspace palette's hint line — the feature, named where a user lands first.</summary>
  public WorkspaceStrings WorkspaceStrings => Lang.Workspace;

  /// <summary>The process broadcast hub — the session-ending request arrives through it.</summary>
  [Inject] private partial IMessageHub MessageHub { get; }

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

  bool IMessageRecipient.CanReceive(IMessage message) => message is SessionEndingMessage;

  void IMessageRecipient.Receive(IMessage message)
  {
    if (message is not SessionEndingMessage sessionEnding)
      return;

    // Demo guard: the landing page holds nothing unsaved, so the prompt is
    // only a way to exercise the session-ending orchestration.  Replace
    // Confirmed with the real flush.
    sessionEnding.Guard(
      PagesStrings.SessionEndingTitle,
      PagesStrings.SessionEndingMessage,
      () => Debug.WriteLine("[LandingPageModel] session ending confirmed"));
  }

  [RelayCommand]
  private Task OpenInteractionLab()
  {
    return Router.RouteAsync(new InteractionLabLocator());
  }

  [RelayCommand]
  private Task OpenPosts()
  {
    return Router.RouteAsync(new PostsLocator());
  }

  [RelayCommand]
  private Task OpenProfile()
  {
    return Router.RouteAsync(new ProfileLocator());
  }

  [RelayCommand]
  private Task OpenSettings()
  {
    return Router.RouteAsync(new SettingsLocator());
  }

  [RelayCommand]
  private Task OpenAdmin()
  {
    return Router.RouteAsync(new AdminLocator());
  }
}
