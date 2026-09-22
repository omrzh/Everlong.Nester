using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Everlong.Nester.Presentation;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Everlong.Nester.Tests.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The close-guard dialog flow (the WPF-template close chain): the window's
///   OnClosing translates to TryCloseIntent, the Director shows a confirm
///   dialog, and the button result decides allow/veto.  Regression surface
///   for the "clicked Yes Close, the window stays open" wiring break.
/// </summary>
[Collection("RealShell")]
public class CloseGuardDialogTests
{
  private sealed class TestShellWindow : Window, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage)
    {
      _contentLayer.Content = stage;
      _shell ??= shell;
    }

    internal IAvaloniaShell? Shell
    {
      get => _shell;   // wired at HostShell — the window sits above the stage, it cannot crawl to it
      init => _shell = value;
    }
    private IAvaloniaShell? _shell;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
      base.OnClosing(e);
      if (Shell is { } shell)
        shell.WindowClosingToTryCloseIntent(e);
    }
  }

  /// <summary>The template's close guard — a ConfirmDialogSession; "Yes, close" allows, "No, stay" vetoes.</summary>
  private sealed class GuardedDirector : IShellDirector
  {
    private IShell? _shell;
    private IRouter? _dialogs;

    public ConfirmDialogSession? ShownSession { get; private set; }

    /// <summary>The user's wiring point (manualDirector = the user's protocol): attach the shell the guard dispatches through.</summary>
    internal void Attach(IShell shell) => _shell = shell;


    public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      if (context.Intent is TryCloseIntent)
      {
        var session = new ConfirmDialogSession
        {
          Message = "Close?",
          ConfirmText = "Yes, close",
          CancelText = "No, stay",
          IsConfirmEnabled = true,
        };
        ShownSession = session;
        _dialogs ??= _shell!.Services!.GetRequiredService<IRouter>();
        bool? result = await _dialogs.ShowAsync<bool?>(session);
        if (result is not true)
        {
          context.Veto();
          return;
        }
      }
      await next(context);
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => false;
  }

  private static Task FlushUiThreadAsync()
    => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();

  private static (AvaloniaShell Shell, TestShellWindow Window, GuardedDirector Director) BuildShell()
  {
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(RealAppHarness.PageTemplate);
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());

    var window = new TestShellWindow();
    var shell = TestHost.CreateShell<GuardedDirector>(
      s => TestAppServices.AddWindowAuth(s),
      manualDirector: true,
      rootView: window);
    if (shell.Director is GuardedDirector director)
      director.Attach(shell);   // manualDirector: the user wires the shell reference
    shell.Start();
    var impl = Assert.IsType<TestShell<GuardedDirector>>(shell);
    return (shell, window, Assert.IsType<GuardedDirector>(impl.Director));
  }

  [AvaloniaFact]
  public async Task CloseGuard_ConfirmYes_ClosesWindow_AndDisposesShell()
  {
    var (shell, window, director) = BuildShell();
    await shell.Lifetime.Startup;
    Assert.True(window.IsVisible);

    // The close → the guard dialog appears.
    window.Close();
    await FlushUiThreadAsync();

    Assert.NotNull(director.ShownSession);
    var session = director.ShownSession!;

    // "Yes, close" → allow → the shell fallback disposes + closes (the
    // dialog dismissal + disposal span several dispatcher cycles).
    session.Close(true);
    for (int i = 0; i < 20 && shell.Lifetime.Lifecycle != ShellLifecycle.Disposed; i++)
    {
      await Task.Delay(50);
      await FlushUiThreadAsync();
    }

    Assert.True(shell.Lifetime.Lifecycle == ShellLifecycle.Disposed, "Confirming the close must dispose the shell.");
    Assert.False(window.IsVisible, "Confirming the close must close the window.");
  }

  [AvaloniaFact]
  public async Task CloseGuard_ConfirmNo_KeepsWindowOpen_ShellAlive()
  {
    var (shell, window, director) = BuildShell();
    await shell.Lifetime.Startup;

    window.Close();
    await FlushUiThreadAsync();

    Assert.NotNull(director.ShownSession);
    var session = director.ShownSession!;

    // "No, stay" → veto → the window stays.
    session.Close(false);
    await FlushUiThreadAsync();

    Assert.False(shell.Lifetime.Lifecycle == ShellLifecycle.Disposed, "A vetoed close must leave the shell alive.");
    Assert.True(window.IsVisible, "A vetoed close must keep the window open.");

    await shell.DisposeAsync();
  }
}
