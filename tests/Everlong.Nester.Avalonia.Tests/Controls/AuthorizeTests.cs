using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Auth;
using Everlong.Nester.Controls;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Models;
using Xunit;
using Everlong.Nester.Tests.Hosting;
using Everlong.Nester.Tests.Routing;

namespace Everlong.Nester.Tests.Controls;

/// <summary>
///   The <c>n:Authorize</c> attached properties (Visible / Enable): the
///   Pending annotation must arm the mount-time check — a control annotated
///   in XAML checks its authorization when it attaches under the shell's
///   stage.  Regression guard: the Coerce path must annotate through the
///   attach subscription — a raw SetValue skips the mount check and leaves
///   the annotated control invisible.
/// </summary>
public class AuthorizeTests
{
  private static (AvaloniaShell Shell, IAuthService Auth) StartLoggedInShell(string username, string[] roles)
  {
    AvaloniaShell shell = TestHost.CreateShell<NoopDirector>(s => TestAppServices.AddWindowAuth(s));
    shell.Start();

    // The auth service lives in the shell's scope — the gating controls
    // resolve it by crawling to the stage; no tree attach is involved.
    var auth = ((IShell)shell).Services!.GetRequiredService<IAuthService>();
    auth.Login(new DemoUser(username, roles));
    return (shell, auth);
  }

  /// <summary>Mounts the content under the shell's stage — the subtree from which gating controls resolve the shell scope.</summary>
  private static Window MountUnderStage(AvaloniaShell shell, Control content)
  {
    var panel = shell.StagePanel!;
    panel.Children.Add(content);
    var window = new Window { Content = panel, Width = 800, Height = 600 };
    window.Show();
    return window;
  }

  [AvaloniaFact]
  public void Authorize_Visible_AnnotatedBeforeMount_ChecksOnMount()
  {
    // The regression: an admin-role control annotated BEFORE mounting stayed
    // invisible — the annotation must arm the mount-time authorization check.
    var (shell, _) = StartLoggedInShell("admin", ["Admin"]);
    var button = new Button();
    Authorize.SetVisible(button, "Admin");   // annotated while detached → Pending → armed
    Assert.False(button.IsVisible);

    var window = MountUnderStage(shell, button);
    try
    {
      Assert.True(button.IsVisible, "the admin-role control must pass its authorization check on mount");
    }
    finally
    {
      window.Close();
      shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }

  [AvaloniaFact]
  public void Authorize_Visible_Unauthenticated_StaysHidden()
  {
    var (shell, _) = StartLoggedInShell("user", ["User"]);
    var button = new Button();
    Authorize.SetVisible(button, "Admin");   // the "Admin" role rule against a User session

    var window = MountUnderStage(shell, button);
    try
    {
      Assert.False(button.IsVisible, "a non-Admin session must keep the Admin control hidden");
    }
    finally
    {
      window.Close();
      shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }

  [AvaloniaFact]
  public void Authorize_Visible_LoginAfterMount_Rechecks()
  {
    // AuthorizationChanged re-check: a control mounted while logged OUT
    // becomes visible when the user logs in (its strong service binding).
    var (shell, auth) = StartLoggedInShell("user", ["User"]);
    var button = new Button();
    Authorize.SetVisible(button, "Admin");

    var window = MountUnderStage(shell, button);
    try
    {
      Assert.False(button.IsVisible);

      auth.Login(new DemoUser("admin", ["Admin"]));   // role change → AuthorizationChanged → recheck
      Avalonia.Threading.Dispatcher.UIThread.RunJobs();   // the recheck is posted — drain the queue before asserting
      Assert.True(button.IsVisible);
    }
    finally
    {
      window.Close();
      shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
  }
}
