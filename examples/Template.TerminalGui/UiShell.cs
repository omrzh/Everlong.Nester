using Everlong.Nester.Activation;
using Everlong.DI;
using Everlong.Nester.Auth;
using Everlong.Nester.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Messaging;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Presentation;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Properties;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;

namespace NesterApp;

/// <summary>
///   The app's shell — the Terminal.Gui surface: one full-screen stage per
///   process; the Director decides the first page.
/// </summary>
public sealed class UiShell(IActivationIntent? startupIntent = null) : TuiShell(startupIntent)
{
  protected override IServiceProvider InitializeServices()
  {
    ArgumentNullException.ThrowIfNull(DirectorType);
    var services = new ServiceCollection();
    services.AddScoped(DirectorType); // the Director — resolved via DirectorType
    services.AddNesterShell(this);    // the shell's identity
    services.AddNesterCore();         // logging / injector / routing dimension
    services.AddNesterNotice();       // the notice surface (toast/snackbar/notification)
    services.AddNesterDialog();       // the dialog domain (dimmer chrome model)
    services.AddSingleton<IViewLocator>(new PageViewLocator());
    services.BridgeSingleton<IAuthService>(); // cross-surface shared login state (bridged from the process container)
    services.BridgeSingleton<IMessageHub>();  // cross-surface shared broadcast hub (bridged from the process container)
    services.AddServices(new AppServices());
    return services.BuildServiceProvider(new ServiceProviderOptions
    {
      ValidateScopes = true,
      ValidateOnBuild = true
    });
  }

  protected override void OnAssembled()
  {
    // The settings store is a shell Singleton — no scope needed here.
    AppSettings.Default.Inject(Services);

    // Surface key translation — navigation intents through the intent chain.
    Surface.KeyDown += OnSurfaceKeyDown;
  }

  /// <summary>
  ///   Translates keyboard shortcuts into routing intents (F5 → refresh,
  ///   Alt+Left/Right → back/forward, Ctrl+Q → close).
  /// </summary>
  private async void OnSurfaceKeyDown(object? sender, Key key)
  {
    IIntent? intent = key switch
    {
      { IsCtrl: true, KeyCode: KeyCode.Q } => new CloseIntent(),
      { KeyCode: KeyCode.F5 } => new RefreshIntent(),
      { IsAlt: true, KeyCode: KeyCode.CursorLeft } => new BackIntent(),
      { IsAlt: true, KeyCode: KeyCode.CursorRight } => new ForwardIntent(),
      _ => null
    };

    if (intent == null)
    {
      return;
    }

    key.Handled = true;
    await this.DispatchIntent(Surface, intent);
  }
}
