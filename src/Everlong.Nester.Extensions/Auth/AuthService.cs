using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace Everlong.Nester.Auth;

/// <summary>
///   Evaluates authorization requirements against the current principal, caching the results.
/// </summary>
internal class AuthService : IAuthService
{
  // Cache: CacheKey -> (Result, ExpirationTime)
  private readonly Dictionary<AuthorizationCacheKey, (bool Result, DateTime Expiration)> _cache = [];
  private readonly object _cacheLock = new();
  private readonly IList<IAuthorizationHandler> _handlers;
  private readonly AuthorizationOptions _options;
  private readonly IAuthorizationPolicyProvider _policyProvider;
  private readonly ILogger<AuthService> _logger;
  private readonly TimeProvider _timeProvider;

  /// <summary>
  ///   Initializes a new instance of the <see cref="AuthService" /> class.
  /// </summary>
  /// <param name="options">Authorization options.</param>
  /// <param name="policyProvider">Policy provider.</param>
  /// <param name="handlers">Authorization handlers.</param>
  /// <param name="logger">The logger.</param>
  /// <param name="timeProvider">The time source for cache expiration.</param>
  public AuthService(
    AuthorizationOptions? options = null,
    IAuthorizationPolicyProvider? policyProvider = null,
    IEnumerable<IAuthorizationHandler>? handlers = null,
    ILogger<AuthService>? logger = null,
    TimeProvider? timeProvider = null)
  {
    _options = options ?? new AuthorizationOptions();
    _policyProvider = policyProvider ?? new DefaultAuthorizationPolicyProvider(_options);
    _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance;
    _timeProvider = timeProvider ?? TimeProvider.System;

    _handlers = handlers?.ToList() ?? [];
    if (!_handlers.OfType<DefaultAuthorizationHandler>().Any())
    {
      _handlers.Add(new DefaultAuthorizationHandler());
    }

    // Subscribe to our own state change to clear cache
    AuthenticationStateChanged += OnAuthenticationStateChanged;
  }

  /// <summary>Creates the service over the given handlers with default options, provider and logger.</summary>
  public AuthService(IList<IAuthorizationHandler> handlers)
    : this(null, null, handlers, null)
  {
  }

  /// <inheritdoc />
  public event EventHandler? AuthorizationChanged;

  /// <inheritdoc />
  public ClaimsPrincipal? CurrentPrincipal { get; private set; }

  /// <summary>
  ///   Gets the current authenticated user object.
  /// </summary>
  public IIdentityUser? CurrentUser { get; private set; }

  /// <inheritdoc />
  public event EventHandler<AuthenticationStateChangedEventArgs>? AuthenticationStateChanged;

  /// <inheritdoc />
  public virtual void Login(IIdentityUser user)
  {
    SetUser(user);
  }

  /// <inheritdoc />
  public virtual void Logout()
  {
    SetUser(null);
  }

  /// <inheritdoc />
  public AuthorizationResult Authorize(ClaimsPrincipal user,
                                       object? resource,
                                       IEnumerable<IAuthorizationRequirement> requirements)
  {
    ArgumentNullException.ThrowIfNull(requirements);

    AuthorizationHandlerContext authContext = new(requirements, user, resource);
    foreach (IAuthorizationHandler handler in _handlers)
    {
      handler.Handle(authContext);
    }

    if (authContext.HasSucceeded)
    {
      _logger.LogDebug("Authorization succeeded for user {User} on resource {Resource}.", user.Identity?.Name,
                       resource);
      return AuthorizationResult.Success();
    }

    _logger.LogWarning("Authorization failed for user {User} on resource {Resource}.", user.Identity?.Name, resource);
    return AuthorizationResult.Failed(authContext.HasFailed
                                        ? AuthorizationFailure.ExplicitFail()
                                        : AuthorizationFailure.Failed(authContext.PendingRequirements));
  }

  /// <inheritdoc />
  public AuthorizationResult Authorize(ClaimsPrincipal user, object? resource, string policyName)
  {
    ArgumentNullException.ThrowIfNull(policyName);

    AuthorizationPolicy? policy = _policyProvider.GetPolicy(policyName);
    return policy == null
             ? throw new InvalidOperationException($"No policy found: {policyName}.")
             : Authorize(user, resource, policy);
  }

  /// <inheritdoc />
  public AuthorizationResult Authorize(string? roles, string? policyName)
  {
    ClaimsPrincipal? principal = CurrentPrincipal;

    // Unauthenticated users are denied directly
    if (principal == null || principal.Identity?.IsAuthenticated != true)
    {
      return AuthorizationResult.Failed();
    }

    // Check cache
    if (_options.EnableCache && TryGetCachedResult(principal, roles, policyName, out bool cachedResult))
    {
      return cachedResult ? AuthorizationResult.Success() : AuthorizationResult.Failed();
    }

    bool isAuthorized = true;

    // 1. Check Roles
    if (!string.IsNullOrWhiteSpace(roles))
    {
      string[] roleList = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      AuthorizationPolicy rolePolicy = new PolicyBuilder().RequireRole(roleList).Build();
      AuthorizationResult result = Authorize(principal, null, rolePolicy);
      if (!result.Succeeded)
      {
        isAuthorized = false;
      }
    }

    // 2. Check Policy (only if passed roles check)
    if (isAuthorized && !string.IsNullOrWhiteSpace(policyName))
    {
      AuthorizationResult result = Authorize(principal, null, policyName!);
      if (!result.Succeeded)
      {
        isAuthorized = false;
      }
    }

    // Cache result
    if (_options.EnableCache)
    {
      CacheResult(principal, roles, policyName, isAuthorized);
    }

    return isAuthorized ? AuthorizationResult.Success() : AuthorizationResult.Failed();
  }

  /// <summary>
  ///   Checks whether the current principal is authenticated.
  /// </summary>
  /// <returns>
  ///   <see cref="AuthorizationResult.Success" /> when authenticated; otherwise
  ///   <see cref="AuthorizationResult.Failed()" />.
  /// </returns>
  public AuthorizationResult Authenticate()
  {
    ClaimsPrincipal? principal = CurrentPrincipal;

    if (principal == null || principal.Identity?.IsAuthenticated != true)
    {
      return AuthorizationResult.Failed();
    }

    return AuthorizationResult.Success();
  }

  /// <summary>
  ///   Sets the current user and notifies authentication state changes.
  /// </summary>
  /// <param name="user">The user object. If null, logs out the user.</param>
  private void SetUser(IIdentityUser? user)
  {
    ClaimsPrincipal? oldPrincipal = CurrentPrincipal;

    if (user is null)
    {
      _logger.LogInformation("User logged out.");
      CurrentUser = null;
      CurrentPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
    }
    else
    {
      _logger.LogInformation("User logged in: {User}", user.GetIdentity());
      CurrentUser = user;
      CurrentPrincipal = CreatePrincipal(user);
    }

    AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs(CurrentPrincipal, oldPrincipal));
  }

  /// <summary>
  ///   Creates a ClaimsPrincipal from the user object.
  /// </summary>
  /// <param name="user">The user object.</param>
  /// <returns>The ClaimsPrincipal.</returns>
  protected virtual ClaimsPrincipal CreatePrincipal(IIdentityUser user)
  {
    ClaimsIdentity identity = new("Authentication");

    string id = user.GetIdentity();
    identity.AddClaim(new Claim("sub", id));
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id));

    foreach (string role in user.GetRoles())
    {
      identity.AddClaim(new Claim(ClaimTypes.Role, role));
    }

    IEnumerable<KeyValuePair<string, string>>? claims = user.GetAdditionalClaims();
    if (claims != null)
    {
      foreach (KeyValuePair<string, string> claim in claims)
      {
        identity.AddClaim(new Claim(claim.Key, claim.Value));
      }
    }

    return new ClaimsPrincipal(identity);
  }

  private void OnAuthenticationStateChanged(object? sender, AuthenticationStateChangedEventArgs e)
  {
    lock (_cacheLock)
    {
      _cache.Clear();
    }

    AuthorizationChanged?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  ///   Checks if a user meets a specific policy for the specified resource.
  /// </summary>
  public AuthorizationResult Authorize(ClaimsPrincipal user, object? resource, AuthorizationPolicy policy)
  {
    return policy == null
             ? throw new ArgumentNullException(nameof(policy))
             : Authorize(user, resource, policy.Requirements);
  }

  private bool TryGetCachedResult(ClaimsPrincipal user, string? roles, string? policy, out bool result)
  {
    AuthorizationCacheKey key = new(GetCacheUserId(user), roles, policy);

    lock (_cacheLock)
    {
      if (_cache.TryGetValue(key, out (bool Result, DateTime Expiration) cacheEntry))
      {
        if (_timeProvider.GetUtcNow().UtcDateTime < cacheEntry.Expiration)
        {
          result = cacheEntry.Result;
          return true;
        }

        _cache.Remove(key);
      }
    }

    result = false;
    return false;
  }

  private void CacheResult(ClaimsPrincipal user, string? roles, string? policy, bool result)
  {
    AuthorizationCacheKey key = new(GetCacheUserId(user), roles, policy);
    DateTime expiration = _timeProvider.GetUtcNow().UtcDateTime.Add(_options.CacheExpiration);

    lock (_cacheLock)
    {
      _cache[key] = (result, expiration);
    }
  }

  private static string GetCacheUserId(ClaimsPrincipal user)
  {
    // The principal built by CreatePrincipal carries the user id as both
    // ClaimTypes.NameIdentifier and "sub" — use them instead of
    // Identity.Name, which is null unless a Name claim is present.
    return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? user.FindFirst("sub")?.Value
        ?? string.Empty;
  }

  /// <summary>
  ///   Cache key structure for authorization results
  /// </summary>
  private readonly record struct AuthorizationCacheKey(string UserId, string? Roles, string? Policy);
}
