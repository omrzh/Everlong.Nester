namespace Everlong.Nester.Auth;

/// <summary>
///   Handles authorization requests.
/// </summary>
public interface IAuthorizationHandler
{
  /// <summary>
  ///   Decides whether authorization is allowed.
  /// </summary>
  /// <param name="context">The authorization information.</param>
  void Handle(AuthorizationHandlerContext context);
}

/// <summary>
///   Base class for authorization handlers that need to handle a specific requirement type.
/// </summary>
/// <typeparam name="TRequirement">The type of the requirement to handle.</typeparam>
public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
  where TRequirement : IAuthorizationRequirement
{
  /// <inheritdoc />
  public void Handle(AuthorizationHandlerContext context)
  {
    foreach (TRequirement req in context.Requirements.OfType<TRequirement>())
    {
      HandleRequirement(context, req);
    }
  }

  /// <summary>
  ///   Decides whether authorization is allowed for a specific requirement.
  /// </summary>
  /// <param name="context">The authorization information.</param>
  /// <param name="requirement">The requirement to evaluate.</param>
  protected abstract void HandleRequirement(AuthorizationHandlerContext context, TRequirement requirement);
}

/// <summary>
///   Base class for authorization handlers that need to handle a specific requirement type and resource type.
/// </summary>
/// <typeparam name="TRequirement">The type of the requirement to handle.</typeparam>
/// <typeparam name="TResource">The type of the resource to handle.</typeparam>
public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler
  where TRequirement : IAuthorizationRequirement
{
  /// <inheritdoc />
  public void Handle(AuthorizationHandlerContext context)
  {
    if (context.Resource is TResource resource)
    {
      foreach (TRequirement req in context.Requirements.OfType<TRequirement>())
      {
        HandleRequirement(context, req, resource);
      }
    }
  }

  /// <summary>
  ///   Decides whether authorization is allowed for a specific requirement and resource.
  /// </summary>
  /// <param name="context">The authorization information.</param>
  /// <param name="requirement">The requirement to evaluate.</param>
  /// <param name="resource">The resource to evaluate.</param>
  protected abstract void HandleRequirement(AuthorizationHandlerContext context,
                                            TRequirement requirement,
                                            TResource resource);
}

/// <summary>
///   Dispatches each requirement to its <see cref="ISelfHandlingAuthorizationRequirement" /> implementation.
/// </summary>
internal class DefaultAuthorizationHandler : IAuthorizationHandler
{
  /// <inheritdoc />
  public void Handle(AuthorizationHandlerContext context)
  {
    foreach (IAuthorizationRequirement req in context.Requirements)
    {
      if (req is ISelfHandlingAuthorizationRequirement selfHandlingReq)
      {
        selfHandlingReq.Handle(context);
      }
    }
  }
}
