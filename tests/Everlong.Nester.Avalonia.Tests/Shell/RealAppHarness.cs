using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Shared harness bits for the real-app suites (RealAppTests /
///   MultiWindowTests / SingleViewHostTests) — the fake visual-stack
///   template and the ground-entry reader.
/// </summary>
internal static class RealAppHarness
{
  /// <summary>The page-mapping template — every NesterApp view model maps to a tagged control (the shell's ROOT view is the harness's own <c>TestShell</c> default host; pages resolve through the app template chain).</summary>
  internal static RealAppDataTemplate PageTemplate => new();

  /// <summary>Routes a bare target on the shell's router (the ground entry).</summary>
  internal static Task RouteAsync(IShell shell, Type target)
    => shell.Services!.GetRequiredService<IRouter>().RouteAsync(new Locator(target));

  /// <summary>The ground navigation structure's current page model of a shell.</summary>
  internal static object? GroundEntry(IShell shell)
  {
    var router = shell.Services!.GetRequiredService<IRouter>() as Router;
    return router?.View.Location?.Instance;
  }

  /// <summary>Fake visual stack: every NesterApp view model maps to a tagged control.</summary>
  internal sealed class RealAppDataTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new ContentControl { Tag = param.GetType() };

    public bool Match(object? data)
      => data is not null && data.GetType().Namespace?.StartsWith("NesterApp") == true;
  }
}
