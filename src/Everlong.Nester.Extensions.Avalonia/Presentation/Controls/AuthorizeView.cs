using Avalonia.Metadata;
using Avalonia.VisualTree;
using Avalonia;
using Everlong.Nester.Auth;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A content control that displays different content based on the user's authorization status.
/// </summary>
/// <remarks>
///   <para>
///     AuthorizeView is a conditional content control that shows one of two content states based on whether the
///     current user meets specified authorization rules. It automatically performs authorization checks using the
///     authorization service resolved for the control's shell scope (see <see cref="Authorize.GetAuthorizationService"/>).
///   </para>
///   <para>
///     Use the <see cref="Rules"/> property to specify authorization requirements, the <see cref="Authorized"/>
///     property for content to show when authorized, and the <see cref="NotAuthorized"/> property for content
///     to show when not authorized.
///   </para>
/// </remarks>
public sealed class AuthorizeView : PContentControl
{
  /// <summary>
  ///   Defines the <see cref="Rules" /> property.
  /// </summary>
  public static readonly StyledProperty<string> RulesProperty =
    AvaloniaProperty.Register<AuthorizeView, string>(nameof(Rules), string.Empty);

  /// <summary>
  ///   Defines the <see cref="Authorized" /> property.
  /// </summary>
  public static readonly StyledProperty<object?> AuthorizedProperty =
    AvaloniaProperty.Register<AuthorizeView, object?>(nameof(Authorized));

  /// <summary>
  ///   Defines the <see cref="NotAuthorized" /> property.
  /// </summary>
  public static readonly StyledProperty<object?> NotAuthorizedProperty =
    AvaloniaProperty.Register<AuthorizeView, object?>(nameof(NotAuthorized));

  private bool _ignoreChanged;

  /// <summary>
  ///   Creates the view with the authorization state set to
  ///   <see cref="AuthorizationState.Pending"/>.
  /// </summary>
  public AuthorizeView()
  {
    Authorize.SetState(this, AuthorizationState.Pending);
  }

  /// <summary>
  ///   Gets or sets the authorization rules to evaluate.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The rules string specifies what authorization requirements must be met. The format supports:
  ///   </para>
  ///   <list type="bullet">
  ///     <item>
  ///       <description>Empty string (default): Requires the user to be authenticated.</description>
  ///     </item>
  ///     <item>
  ///       <description><c>"CanEditData"</c>: Requires the specified policy only.</description>
  ///     </item>
  ///     <item>
  ///       <description><c>"Admin,Manager"</c>: Requires one of the specified roles.</description>
  ///     </item>
  ///     <item>
  ///       <description><c>"CanEditData; Admin,Manager"</c>: Requires both the policy AND one of the roles.</description>
  ///     </item>
  ///     <item>
  ///       <description><c>"P=CanEditData; R=Admin,Manager"</c>: Long form of policy and role syntax.</description>
  ///     </item>
  ///     <item>
  ///       <description><c>"Policy=CanEditData; Roles=Admin,Manager"</c>: Full long form of policy and role syntax.</description>
  ///     </item>
  ///   </list>
  ///   <para>
  ///     When this property changes, the authorization check is re-evaluated and the displayed content is updated accordingly.
  ///   </para>
  /// </remarks>
  /// <value>A rules string specifying authorization requirements. Defaults to an empty string (authenticated users only).</value>
  public string Rules
  {
    get => GetValue(RulesProperty);
    set => SetValue(RulesProperty, value);
  }

  /// <summary>
  ///   Gets or sets the content to display when the user is authorized.
  /// </summary>
  /// <remarks>
  ///   When authorization checks pass, this content is displayed and the <see cref="NotAuthorized"/> content is hidden.
  ///   This property can be set via XAML as the default content property.
  /// </remarks>
  /// <value>The content to display when authorized, or <c>null</c> for no authorized content.</value>
  [Content]
  public object? Authorized
  {
    get => GetValue(AuthorizedProperty);
    set => SetValue(AuthorizedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the content to display when the user is not authorized.
  /// </summary>
  /// <remarks>
  ///   When authorization checks fail, this content is displayed and the <see cref="Authorized"/> content is hidden.
  /// </remarks>
  /// <value>The content to display when not authorized, or <c>null</c> for no unauthorized content.</value>
  public object? NotAuthorized
  {
    get => GetValue(NotAuthorizedProperty);
    set => SetValue(NotAuthorizedProperty, value);
  }

  /// <inheritdoc />
  /// <remarks>
  ///   When the <see cref="Rules"/> property changes, this override re-evaluates the authorization check using
  ///   the discovered authorization service. The displayed content is updated based on the new authorization result.
  ///   When the authorization state changes, the appropriate content is displayed or hidden.
  /// </remarks>
  protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
  {
    if (_ignoreChanged)
    {
      return;
    }

    if (change.Property == RulesProperty)
    {
      _ignoreChanged = true;
      Authorize.SetState(this, AuthorizationState.Pending);
      _ignoreChanged = false;
      if (this.IsAttachedToVisualTree() && Authorize.GetAuthorizationService(this) is { } service)
      {
        Authorize.PerformCheck(this, change.NewValue as string ?? string.Empty, service,
                               AuthorizationType.AuthorizeView);
      }

      return;
    }

    if (change.Property == Authorize.StateProperty
        && change.NewValue is AuthorizationState state
        && state is not AuthorizationState.Pending and not AuthorizationState.Skipped)
    {
      UpdateContent(state);
      return;
    }

    base.OnPropertyChanged(change);
  }

  private void UpdateContent(AuthorizationState state)
  {
    _ignoreChanged = true;
    if (state == AuthorizationState.Passed)
    {
      Content = Authorized;
    }
    else
    {
      Content = NotAuthorized;
    }

    _ignoreChanged = false;
  }
}
