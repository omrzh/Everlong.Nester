using Avalonia.Controls;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   Host-mode test helper: assembles a shell over a FRESH window container
///   per call through the harness's own <see cref="TestShell{TDirector}" />
///   flow — every shell builds and owns its own provider, no process-global
///   state (the AppLifetime static facade is never touched).  The shell
///   tests use this instead of the AppLifetime flow (which is process-bound
///   in tests).
/// </summary>
internal static class TestHost
{
  /// <summary>Assembles a shell over a fresh window container (framework defaults + the factory's registrations + register).</summary>
  public static AvaloniaShell CreateShell<TDirector>(Action<IServiceCollection>? register = null,
                                                   ContentControl? rootView = null,
                                                   bool directMount = false,
                                                   bool manualDirector = false,
                                                   bool rootViewFromLocator = false,
                                                   bool noHost = false)
    where TDirector : class, IShellDirector, new()
    => CreateShell<TDirector>(register, out _, rootView, directMount, manualDirector, rootViewFromLocator, noHost);

  /// <summary>Assembles a shell and hands back the shell's own container root (test/observer channel).</summary>
  public static AvaloniaShell CreateShell<TDirector>(Action<IServiceCollection>? register,
                                                   out IServiceProvider root,
                                                   ContentControl? rootView = null,
                                                   bool directMount = false,
                                                   bool manualDirector = false,
                                                   bool rootViewFromLocator = false,
                                                   bool noHost = false)
    where TDirector : class, IShellDirector, new()
  {
    var shell = new TestShell<TDirector>(s =>
    {
      register?.Invoke(s);
    }, rootView, directMount, manualDirector, rootViewFromLocator, noHost);
    root = shell.RootProvider!;
    return shell;
  }
}
