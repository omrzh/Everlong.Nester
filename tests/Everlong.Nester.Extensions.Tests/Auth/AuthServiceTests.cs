using Everlong.Nester.Auth;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Auth;

/// <summary>
///   Authorize/authenticate flow of <see cref="AuthService" />: short-circuit
///   order, cache hit/expiry semantics, cache isolation between users, and
///   cache invalidation on login/logout — driven by a fake clock.
/// </summary>
public class AuthServiceTests
{
  private sealed class FakeUser(string id, params string[] roles) : IIdentityUser
  {
    public string GetIdentity() => id;

    public IEnumerable<string> GetRoles() => roles;

    public IEnumerable<KeyValuePair<string, string>>? GetAdditionalClaims() => null;
  }

  private sealed class CountingAssertionHandler : IAuthorizationHandler
  {
    public int Calls { get; private set; }

    public void Handle(AuthorizationHandlerContext context)
    {
      foreach (AssertionRequirement req in context.Requirements.OfType<AssertionRequirement>())
      {
        Calls++;
        req.Handle(context);
      }
    }
  }

  private static AuthService Create(out CountingAssertionHandler handler, out FakeTimeProvider time)
  {
    var options = new AuthorizationOptions
    {
      EnableCache = true,
      CacheExpiration = TimeSpan.FromSeconds(10)
    };
    options.AddPolicy("satisfied", b => b.RequireAssertion(_ => true));
    options.AddPolicy("unsatisfied", b => b.RequireAssertion(_ => false));

    handler = new CountingAssertionHandler();
    time = new FakeTimeProvider();
    return new AuthService(options: options, handlers: [handler], timeProvider: time);
  }

  [Fact]
  public void Authorize_NotLoggedIn_ReturnsFailed_WithoutEvaluatingHandlers()
  {
    var service = Create(out var handler, out _);

    AuthorizationResult result = service.Authorize("admin", "satisfied");

    Assert.False(result.Succeeded);
    Assert.Equal(0, handler.Calls);
  }

  [Fact]
  public void Authorize_RolesMatch_ReturnsSuccess()
  {
    var service = Create(out _, out _);
    service.Login(new FakeUser("u1", "admin"));

    Assert.True(service.Authorize("admin", null).Succeeded);
  }

  [Fact]
  public void Authorize_RolesMismatch_ReturnsFailed()
  {
    var service = Create(out _, out _);
    service.Login(new FakeUser("u1", "user"));

    Assert.False(service.Authorize("admin", null).Succeeded);
  }

  [Fact]
  public void Authorize_PolicySatisfied_ReturnsSuccess()
  {
    var service = Create(out var handler, out _);
    service.Login(new FakeUser("u1", "user"));

    Assert.True(service.Authorize(null, "satisfied").Succeeded);
    Assert.Equal(1, handler.Calls);
  }

  [Fact]
  public void Authorize_UnknownPolicy_Throws()
  {
    var service = Create(out _, out _);
    service.Login(new FakeUser("u1"));

    Assert.Throws<InvalidOperationException>(() => service.Authorize(null, "missing"));
  }

  [Fact]
  public void Authorize_RolesFail_SkipsPolicyEvaluation()
  {
    var service = Create(out var handler, out _);
    service.Login(new FakeUser("u1", "user"));

    AuthorizationResult result = service.Authorize("admin", "satisfied");

    Assert.False(result.Succeeded);
    Assert.Equal(0, handler.Calls);
  }

  [Fact]
  public void Authorize_CachesSuccess_HandlerRunsOnce()
  {
    var service = Create(out var handler, out _);
    service.Login(new FakeUser("u1", "admin"));

    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);

    Assert.Equal(1, handler.Calls);
  }

  [Fact]
  public void Authorize_CachesDenial_HandlerRunsOnce()
  {
    var service = Create(out var handler, out _);
    service.Login(new FakeUser("u1", "user"));

    Assert.False(service.Authorize("admin", "satisfied").Succeeded);
    Assert.False(service.Authorize("admin", "satisfied").Succeeded);

    Assert.Equal(0, handler.Calls);
  }

  [Fact]
  public void Authorize_CacheExpiry_RerunsHandlers()
  {
    var service = Create(out var handler, out var time);
    service.Login(new FakeUser("u1", "admin"));

    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(1, handler.Calls);

    time.Advance(TimeSpan.FromSeconds(9));
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(1, handler.Calls);

    time.Advance(TimeSpan.FromSeconds(2));
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(2, handler.Calls);
  }

  [Fact]
  public void Authorize_SwitchingUsers_InvalidatesCache_AndRerunsHandlers()
  {
    var service = Create(out var handler, out _);
    service.Login(new FakeUser("admin-user", "admin"));
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(1, handler.Calls);

    // A different user whose roles fail must not hit the first user's cached
    // entry — switching users invalidates the cache.
    service.Login(new FakeUser("plain-user", "user"));
    Assert.False(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(1, handler.Calls);

    // Switching back re-evaluates instead of reusing the stale success.
    service.Login(new FakeUser("admin-user", "admin"));
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.Equal(2, handler.Calls);
  }

  [Fact]
  public void Authorize_WhenCacheDisabled_AlwaysEvaluates()
  {
    var options = new AuthorizationOptions { EnableCache = false };
    options.AddPolicy("satisfied", b => b.RequireAssertion(_ => true));
    var handler = new CountingAssertionHandler();
    var service = new AuthService(options: options, handlers: [handler]);

    service.Login(new FakeUser("u1", "admin"));
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);
    Assert.True(service.Authorize("admin", "satisfied").Succeeded);

    Assert.Equal(2, handler.Calls);
  }

  [Fact]
  public void Authenticate_ReflectsLoginState()
  {
    var service = Create(out _, out _);

    Assert.False(service.Authenticate().Succeeded);

    service.Login(new FakeUser("u1"));
    Assert.True(service.Authenticate().Succeeded);

    service.Logout();
    Assert.False(service.Authenticate().Succeeded);
  }
}
