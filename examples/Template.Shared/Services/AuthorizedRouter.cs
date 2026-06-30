using Everlong.Nester.Auth;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using NesterApp.Pages.Landing;
using NesterApp.Properties;

namespace NesterApp.Services;

/// <summary>
///   The GUI templates' route-authorization wiring: every Route-direction
///   request is evaluated against the assembly's authorization registry
///   before the transaction resolves anything.  A denied request on the base
///   surface rewrites to the landing page; on a derived surface (dialog) it
///   vetoes — the dialog never opens.  Back/forward/refresh/jump replay
///   engagements that passed the gate when they were first committed and are
///   not re-evaluated.  Platform-neutral: the single source compiles into the
///   Avalonia and WPF template assemblies over their own <c>Router</c>
///   (the Terminal.GUI template has no <c>Router</c> and is excluded).
/// </summary>
public sealed class AuthorizedRouter : Router
{
  private readonly IAuthService _auth;
  private readonly IAuthRegistry _registry;
  private readonly INoticeService _notices;

  /// <summary>Builds the router over the scope's auth contracts.</summary>
  public AuthorizedRouter(INoticeService notices, IAuthService auth, IAuthRegistry registry, IServiceProvider services)
    : base(services)
  {
    _notices = notices;
    _auth = auth;
    _registry = registry;
  }

  /// <inheritdoc />
  protected override void HandleRouteRequest(TransactionContext ctx, TransactNext next)
  {
    if (ctx.Direction is not RoutingDirection.Route)
    {
      next(ctx);
      return;
    }

    // Only Route requests carry a locator — the traversal directions replay
    // engagements that already passed the gate.
    if (ctx is { Locator: { } locator }
        && !RouteAuth.IsAuthorized(locator, _registry, _auth))
    {
      // A denied derived surface vetoes — the presented dialog never opens;
      // a denied base request rewrites to the public landing page.
      if (Role == RouterRole.Base)
      {
        ctx.UpdateLocator(new LandingLocator());
        _notices.Toast(Lang.Shell.Unauthorized);
      }
      else
      {
        return;
      }
    }

    next(ctx);
  }
}
