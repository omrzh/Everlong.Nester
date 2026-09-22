using Everlong.Nester.Auth;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Auth;

/// <summary>
///   <see cref="RouteAuth.IsAuthorized" /> over a real <see cref="AuthService" />:
///   the entry-presence rule (a null descriptor never denies, a bare
///   descriptor requires authentication), the role and policy paths, chain
///   order and the unknown-policy failure.
/// </summary>
public class RouteAuthTests
{
  private sealed class FakeUser(string id, params string[] roles) : IIdentityUser
  {
    public string GetIdentity() => id;

    public IEnumerable<string> GetRoles() => roles;

    public IEnumerable<KeyValuePair<string, string>>? GetAdditionalClaims() => null;
  }

  private sealed class MapRegistry : IAuthRegistry
  {
    private readonly Dictionary<Type, AuthDescriptor> _map;

    public MapRegistry(params (Type Type, AuthDescriptor Descriptor)[] entries)
    {
      _map = entries.ToDictionary(e => e.Type, e => e.Descriptor);
    }

    public AuthDescriptor? GetDescriptor(Type type) => _map.GetValueOrDefault(type);
  }

  private sealed class OpenPage;

  private sealed class AdminPage;

  private sealed class MemberAreaPage;

  private static AuthService CreateAuth()
  {
    var options = new AuthorizationOptions();
    options.AddPolicy("Admin", p => p.RequireRole("Admin"));
    return new AuthService(options);
  }

  private static ILocator LocatorOf(params Type[] types)
  {
    return new Locator(types.Select(static t => Target.Of(t)).ToArray());
  }

  [Fact]
  public void TypeWithoutEntryNeverDenies()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry();

    Assert.True(RouteAuth.IsAuthorized(LocatorOf(typeof(OpenPage)), registry, auth));
  }

  [Fact]
  public void RolesRequirementDeniesAnonymousAndMismatchedRole()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry((typeof(AdminPage), new AuthDescriptor(Roles: "Admin")));
    var locator = LocatorOf(typeof(AdminPage));

    Assert.False(RouteAuth.IsAuthorized(locator, registry, auth)); // anonymous

    auth.Login(new FakeUser("user", "User"));
    Assert.False(RouteAuth.IsAuthorized(locator, registry, auth));

    auth.Login(new FakeUser("admin", "Admin"));
    Assert.True(RouteAuth.IsAuthorized(locator, registry, auth));
  }

  [Fact]
  public void BareRequirementRequiresAuthenticationOnly()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry((typeof(MemberAreaPage), new AuthDescriptor()));
    var locator = LocatorOf(typeof(MemberAreaPage));

    Assert.False(RouteAuth.IsAuthorized(locator, registry, auth)); // anonymous

    auth.Login(new FakeUser("any", "User"));
    Assert.True(RouteAuth.IsAuthorized(locator, registry, auth)); // any role
  }

  [Fact]
  public void PolicyRequirementEvaluatesTheRegisteredPolicy()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry((typeof(AdminPage), new AuthDescriptor(Policy: "Admin")));
    var locator = LocatorOf(typeof(AdminPage));

    auth.Login(new FakeUser("user", "User"));
    Assert.False(RouteAuth.IsAuthorized(locator, registry, auth));

    auth.Login(new FakeUser("admin", "Admin"));
    Assert.True(RouteAuth.IsAuthorized(locator, registry, auth));
  }

  [Fact]
  public void UnknownPolicyThrows()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry((typeof(AdminPage), new AuthDescriptor(Policy: "NoSuchPolicy")));

    auth.Login(new FakeUser("admin", "Admin"));
    Assert.Throws<InvalidOperationException>(
      () => RouteAuth.IsAuthorized(LocatorOf(typeof(AdminPage)), registry, auth));
  }

  [Fact]
  public void EveryDeclaringNodeMustPassInChainOrder()
  {
    var auth = CreateAuth();
    var registry = new MapRegistry(
      (typeof(MemberAreaPage), new AuthDescriptor()),
      (typeof(AdminPage), new AuthDescriptor(Roles: "Admin")));
    // The outermost node declares nothing, the content node requires Admin.
    var locator = LocatorOf(typeof(OpenPage), typeof(AdminPage));

    auth.Login(new FakeUser("admin", "Admin"));
    Assert.True(RouteAuth.IsAuthorized(locator, registry, auth));

    auth.Login(new FakeUser("user", "User"));
    Assert.False(RouteAuth.IsAuthorized(locator, registry, auth));
  }
}
