using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.DI;
using Everlong.Nester.Activation;
using Everlong.Nester.Auth;
using Everlong.Nester.Dialog;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Dialogs;
using NesterApp.Pages.Landing;
using NesterApp.Pages.Login;
using NesterApp.Pages.Posts;
using NesterApp.Properties;
using NesterApp.Services;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The app Director — the same class for the main window and the
///   single-view scope; the environment decides what differs
///   (<see cref="AppLifetime.IsSingleView" />): the single view starts at
///   the login form (no window to hide behind), the main window starts at
///   the landing page.  Shared partial: the shell table, the startup
///   coroutine, the activation decisions and the close guard live here, so
///   both platforms maintain ONE copy.  Platform-specific intent cases live
///   in the platform partials (<c>MainViewModel.Avalonia.cs</c> /
///   <c>MainViewModel.Wpf.cs</c>), hooked in through
///   <see cref="HandlePlatformIntentAsync" />.
/// </summary>
public partial class MainViewModel : ObservableObject, IShellDirector
{
  private readonly bool _isSingleView;

  /// <summary>Shell-level access (options, intents).</summary>
  [Inject] protected partial IShell Shell { get; }

  /// <summary>Routing entry — the one entry point for navigation requests.</summary>
  [Inject] public partial IRouter Router { get; }

  public MainViewModel()
  {
    _isSingleView = AppLifetime.Current?.IsSingleView == true;
  }

  /// <summary>Runs once the shell is assembled — before the startup dispatch.</summary>
  public void OnAssembled(IShell shell)
  {
    // Pre-flight: the routing dimension and the authorization service are
    // resolved before any intent is dispatched — the first navigation and
    // the n:Authorize controls load against a valid graph.
    _ = shell.Services.GetRequiredService<IRouter>();
    _ = shell.Services.GetRequiredService<IAuthService>();

    // Director-owned background work: the workspace watcher serves a window.
    // The palette behind it is a desktop gesture, and a single view (mobile /
    // browser / TUI) has no key for it — so it never pays for a watcher.  The
    // service releases itself on IHostLifetime.Stopping: the shell's own
    // signal, handed to it by the container, awaited by nobody here.
    if (!_isSingleView)
      shell.Services.GetRequiredService<WorkspaceFileMonitor>().Start();
  }


  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    switch (context.Intent)
    {
      case ShellActivationIntent:
        // The empty activation — the startup input decided nothing: the
        // default first navigation (single view: always the login form;
        // main window: the landing page).
        await Router.RouteAsync(_isSingleView ? new LoginLocator() : new LandingLocator());
        context.Handle(this);
        return;
      case NegotiateActivationIntent { Converted: { } converted }:
        // negotiate activation — decide only; the framework dispatches the
        // converted intents into this chain once accepted.
        if (await NegotiateActivation(converted))
        {
          context.Handle(this);
          return;
        }

        await next(context);
        return;
      case UriActivationIntent { Uri: { } uri }:
        // Deep link: the first navigation (startup) or the runtime response
        // goes straight to the target page — one code path for both.
        await Router.RouteAsync(new PostsLocator());
        context.Handle(this);
        return;
      case FileActivationIntent { Files: { } files }:
        // File activation.  Desktop: a notification — does not decide the
        // first navigation (pass keeps the default page).  Platform
        // consumption examples: WPF copies the paths to the clipboard
        // (AppLifetime.MainShell?.GetPlatformService<IClipboardService>()),
        // Avalonia opens content via IStorageFile.OpenReadAsync /
        // GetPlatformService<ILauncher>().  Single view: the file event is
        // the whole intent — nothing else to do (consumed here).
        if (_isSingleView)
        {
          context.Handle(this);
          return;
        }

        await next(context);
        return;
      case ShowWorkspacePaletteIntent:
        // The palette is an ordinary derived-router presentation: the Director
        // presents it, the AuthorizedRouter gates it like any other surface,
        // and a dismissal arrives as null.  Its result — the file the app
        // opened — is the palette's outcome, not a navigation, so it is read
        // and dropped here.
        await Router.ShowAsync<string>(new WorkspacePaletteSession());
        context.Handle(this);
        return;
      case TryCloseIntent:
        // veto the close request (true); allow it by passing through.
        if (await CancelClosing())
        {
          context.Veto(this);
          return;
        }

        await next(context);
        return;
      default:
        // Platform intent cases (e.g. Avalonia's StorageItemActivationIntent)
        // live in the platform partials — see HandlePlatformIntentAsync.
        await HandlePlatformIntentAsync(context, next);
        return;
    }
  }

  /// <summary>
  ///   Platform seam — each platform partial implements this to handle its
  ///   own intent types (unknown intents pass through).
  /// </summary>
  private partial ValueTask HandlePlatformIntentAsync(IntentContext context, IntentDelegate next);

  public bool HandleError(Exception exception)
  {
    // A consumed error is invisible by contract (returning true swallows
    // the failure for the caller) — trace it first so a swallowed error
    // never goes fully silent.  Debug output + the console host.
    try
    {
      System.Diagnostics.Debug.WriteLine(exception);
      Console.Error.WriteLine(exception);
    }
    catch
    {
      // logging must never break the error path
    }

    // implement your error handling logic — returning true consumes the
    // error here; false routes it to the host-level error handler.
    return true;
  }

  private async Task<bool> CancelClosing()
  {
    ConfirmDialogSession confirmDialog = new()
    {
      Message = Lang.Shell.CloseConfirmMessage,
      ConfirmText = Lang.Shell.CloseConfirmYes,
      CancelText = Lang.Shell.CloseConfirmNo,
      IsConfirmEnabled = true
    };
    bool? result = await Router.ShowAsync<bool?>(confirmDialog);
    return result is not true;
  }

  private async Task<bool> NegotiateActivation(IReadOnlyList<IActivationIntent> converted)
  {
    // A pure decision point: what the follower wants handled (already
    // converted by the broker) is summarized for the user.
    //   Return true  -> leader accepts; the follower's ActivationOutcome would be "Yield".
    //   Return false -> leader declines; the follower's ActivationOutcome would be "Proceed".
    // To unconditionally turn down the follower => SingleInstance pattern:
    //   return Task.FromResult(true);
    string summary = converted.Count == 0
                       ? Lang.Shell.ActivationStartupArgs
                       : string.Join(", ", converted.Select(i => i switch
                       {
                         UriActivationIntent u => u.Uri.ToString(),
                         FileActivationIntent f => Lang.Shell.FormatActivationFileCount(f.Files.Count),
                         var other => other.GetType().Name,
                       }));
    return await Router.ConfirmAsync(
             Lang.Shell.FormatActivationRequest(summary),
             new ConfirmOptions
             { ConfirmText = Lang.Shell.ActivationAccept, CancelText = Lang.Shell.ActivationDecline });
  }
}
