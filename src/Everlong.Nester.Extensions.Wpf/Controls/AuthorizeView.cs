using System.Windows;
using Everlong.Nester.Auth;

namespace Everlong.Nester.Controls;

/// <summary>
///   A content control that displays different content based on the user's authorization status.
/// </summary>
public sealed class AuthorizeView : PContentControl
{
  /// <summary>
  ///   Defines the <see cref="Rules" /> property.
  /// </summary>
  public static readonly DependencyProperty RulesProperty = DependencyProperty.Register(
    nameof(Rules), typeof(string), typeof(AuthorizeView), new PropertyMetadata(string.Empty, OnRulesChanged));

  /// <summary>
  ///   Defines the <see cref="Authorized" /> property.
  /// </summary>
  public static readonly DependencyProperty AuthorizedProperty = DependencyProperty.Register(
    nameof(Authorized), typeof(object), typeof(AuthorizeView), new PropertyMetadata(null));

  /// <summary>
  ///   Defines the <see cref="NotAuthorized" /> property.
  /// </summary>
  public static readonly DependencyProperty NotAuthorizedProperty = DependencyProperty.Register(
    nameof(NotAuthorized), typeof(object), typeof(AuthorizeView), new PropertyMetadata(null));

  static AuthorizeView()
  {
    Authorize.StateProperty.OverrideMetadata(typeof(AuthorizeView), new FrameworkPropertyMetadata(OnStateChanged));
  }

  /// <summary>
  ///   Creates the view with the authorization state set to
  ///   <see cref="AuthorizationState.Pending"/> — this arms the
  ///   <see cref="Authorize"/> load/unload bind that performs the check.
  /// </summary>
  public AuthorizeView()
  {
    Authorize.SetState(this, AuthorizationState.Pending);
  }

  /// <summary>
  ///   Gets or sets the authorization rules.
  ///   Can be a policy name, a list of roles, or a combination. <br />
  ///   <para>
  ///     "" (default: Authenticated)
  ///   </para>
  ///   <para>
  ///     "CanEditData" (Policy only)
  ///   </para>
  ///   <para>
  ///     "CanEditData; Admin,Manager" or
  ///     "P=CanEditData; R=Admin,Manager" or
  ///     "Policy=CanEditData; Roles=Admin,Manager" (Policy AND Roles)
  ///   </para>
  ///   <para>
  ///     "Policy=CanEditData" or "P=CanEditData"
  ///   </para>
  ///   <para>
  ///     "Roles=Admin,Manager" or "R=Admin,Manager"
  ///   </para>
  /// </summary>
  public string Rules
  {
    get => (string)GetValue(RulesProperty);
    set => SetValue(RulesProperty, value);
  }

  /// <summary>
  ///   Gets or sets the content to display when the user is authorized.
  /// </summary>
  public object? Authorized
  {
    get => GetValue(AuthorizedProperty);
    set => SetValue(AuthorizedProperty, value);
  }

  /// <summary>
  ///   Gets or sets the content to display when the user is not authorized.
  /// </summary>
  public object? NotAuthorized
  {
    get => GetValue(NotAuthorizedProperty);
    set => SetValue(NotAuthorizedProperty, value);
  }

  private static void OnRulesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is AuthorizeView av)
    {
      Authorize.SetState(av, AuthorizationState.Pending);
      if (av.IsLoaded && Authorize.GetAuthorizationService(av) is { } service)
      {
        Authorize.PerformCheck(av, e.NewValue as string ?? string.Empty, service, AuthorizationType.AuthorizeView);
      }
    }
  }

  private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is AuthorizeView av
        && e.NewValue is AuthorizationState state
        && state != AuthorizationState.Pending
        && state != AuthorizationState.Skipped)
    {
      av.UpdateContent(state);
    }
  }

  private void UpdateContent(AuthorizationState state)
  {
    Content = state == AuthorizationState.Passed ? Authorized : NotAuthorized;
  }
}
