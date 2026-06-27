using System.Runtime.ExceptionServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia;
using Everlong.Nester.Auth;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Controls;

/// <summary>
///   Provides attached properties to control UI element visibility based on authorization.
///   The authorization service is resolved from the shell scope of the control's stage
///   (<see cref="GetAuthorizationService"/>); annotated controls subscribe to the service's
///   <see cref="IAuthService.AuthorizationChanged"/> while attached and unsubscribe on detach.
/// </summary>
public static class Authorize
{
  /// <summary>
  ///   Defines the Visible attached property.
  ///   Controls the <see cref="PVisual.IsVisible" /> property based on authorization.
  /// </summary>
  public static readonly AttachedProperty<string?> VisibleProperty =
    AvaloniaProperty.RegisterAttached<PControl, string?>("Visible", typeof(Authorize), coerce: CoerceVisible);

  /// <summary>
  ///   Defines the Enable attached property.
  ///   Controls the <see cref="InputElement.IsEnabled" /> property based on authorization.
  /// </summary>
  public static readonly AttachedProperty<string?> EnableProperty =
    AvaloniaProperty.RegisterAttached<PControl, string?>("Enable", typeof(Authorize), coerce: CoerceEnable);

  // Internal state flag — guards against a repeated authorization check.
  internal static readonly AttachedProperty<AuthorizationState> StateProperty =
    AvaloniaProperty.RegisterAttached<PControl, AuthorizationState>("AuthorizationState", typeof(Authorize),
                                                                           AuthorizationState.Skipped);

  // Guards the per-control AttachedToVisualTree/DetachedFromVisualTree
  // subscription made when a control is annotated (SetState with Pending);
  // the subscriptions drive the initial check and the detach unsubscribe.
  private static readonly AttachedProperty<bool> AttachSubscribedProperty =
    AvaloniaProperty.RegisterAttached<PControl, bool>("AttachSubscribed", typeof(Authorize));

  // The service an annotated control is currently bound to — the strong
  // subscription ends at detach (Unbind clears it), so no weak registry.
  internal static readonly AttachedProperty<ServiceBinding?> BindingProperty =
    AvaloniaProperty.RegisterAttached<PControl, ServiceBinding?>("AuthBinding", typeof(Authorize));

  /// <summary>The strong per-control service subscription — unsubscribed on detach.</summary>
  internal sealed record ServiceBinding(IAuthService Service, EventHandler Handler);

  /// <summary>
  ///   Gets the authorization service of the control's shell scope — the
  ///   stage's shell container (<see cref="IShell.Services"/>);
  ///   <see langword="null"/> when the control is not under a shell's stage
  ///   or the shell does not register the service.
  /// </summary>
  public static IAuthService? GetAuthorizationService(PControl control)
    => control.GetShell()?.Services.GetService<IAuthService>();

  private static string? CoerceVisible(AvaloniaObject obj, string? value)
  {
    return obj is AuthorizeView ? null : CoerceByType(obj, value, AuthorizationType.Visible);
  }

  private static string? CoerceEnable(AvaloniaObject obj, string? value)
  {
    return obj is AuthorizeView ? null : CoerceByType(obj, value, AuthorizationType.Enable);
  }

  private static string? CoerceByType(AvaloniaObject obj, string? value, AuthorizationType type)
  {
    if (value != null && obj is Control ctrl)
    {
      if (ctrl is not AuthorizeView)
      {
        // SetState — NOT a raw SetValue: the Pending annotation must arm the
        // attach/detach subscriptions (the mount-time check trigger); a raw
        // SetValue leaves the control invisible forever (no mount check, and
        // the pre-mount AuthorizationChanged was missed).
        SetState(ctrl, AuthorizationState.Pending);
      }

      switch (type)
      {
        case AuthorizationType.Visible:
          ctrl.IsVisible = false;
          break;
        case AuthorizationType.Enable:
          ctrl.IsEnabled = false;
          break;
        case AuthorizationType.AuthorizeView:
          return null;
      }

      // Authorization check runs once the control is attached and the shell's
      // auth service is reachable through the shell scope.
      if (ctrl.IsAttachedToVisualTree())
      {
        Bind(ctrl);
      }
    }

    return value;
  }

  internal static void SetState(Control element, AuthorizationState? value)
  {
    if (value == AuthorizationState.Pending && !element.GetValue(AttachSubscribedProperty))
    {
      element.SetValue(AttachSubscribedProperty, true);
      element.AttachedToVisualTree += OnAttachedToVisualTree;
      element.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    element.SetValue(StateProperty, value);
  }

  internal static AuthorizationState GetState(Control element)
  {
    return element.GetValue(StateProperty);
  }

  private static void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
  {
    if (sender is Control control && control.IsAttachedToVisualTree())
    {
      Bind(control);
    }
  }

  private static void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
  {
    if (sender is Control control)
    {
      Unbind(control);
    }
  }

  /// <summary>
  ///   Binds the control to its shell scope's service — subscribes to
  ///   <see cref="IAuthService.AuthorizationChanged"/> and runs the initial
  ///   check.  A control already bound to the same service is refreshed only.
  /// </summary>
  private static void Bind(Control control)
  {
    if (GetState(control) == AuthorizationState.Skipped)
    {
      return;
    }

    ServiceBinding? binding = control.GetValue(BindingProperty);
    IAuthService? service = binding?.Service ?? GetAuthorizationService(control);
    if (service is null)
    {
      return;
    }

    if (binding is null || !ReferenceEquals(binding.Service, service))
    {
      binding?.Service.AuthorizationChanged -= binding.Handler;

      var fresh = new ServiceBinding(service, (_, _) =>
        Dispatcher.UIThread.Post(() => Recheck(control, service)));
      service.AuthorizationChanged += fresh.Handler;
      control.SetValue(BindingProperty, fresh);
    }

    UpdateCheck(control, service);
  }

  /// <summary>Unsubscribes the control's service binding — the strong subscription ends here.</summary>
  private static void Unbind(Control control)
  {
    if (control.GetValue(BindingProperty) is not { } binding)
    {
      return;
    }

    binding.Service.AuthorizationChanged -= binding.Handler;
    control.SetValue(BindingProperty, null);
  }

  private static void Recheck(Control control, IAuthService service)
  {
    // The control may have detached while the post was queued — its binding
    // is gone then (re-armed on the next attach).
    if (control.IsAttachedToVisualTree()
        && ReferenceEquals(control.GetValue(BindingProperty)?.Service, service))
    {
      UpdateCheck(control, service);
    }
  }

  private static void UpdateCheck(Control control, IAuthService service)
  {
    if (GetState(control) == AuthorizationState.Skipped)
    {
      return;
    }

    if (control is AuthorizeView av)
    {
      PerformCheck(control, av.Rules, service, AuthorizationType.AuthorizeView);
      return;
    }

    if (GetVisible(control) is { } v)
    {
      PerformCheck(control, v, service, AuthorizationType.Visible);
    }

    if (GetEnable(control) is { } e)
    {
      PerformCheck(control, e, service, AuthorizationType.Enable);
    }
  }

  internal static void PerformCheck(Control control, string roleAndPolicy, IAuthService service, AuthorizationType type)
  {
    try
    {
      (string? policy, string? roles) = AuthStringParser.ParseAuthInfo(roleAndPolicy);
      AuthorizationResult result = service.Authorize(roles, policy);
      if (type == AuthorizationType.Visible)
      {
        control.IsVisible = result.Succeeded;
        SetState(control, AuthorizationState.Checked);
      }
      else if (type == AuthorizationType.Enable)
      {
        control.IsEnabled = result.Succeeded;
        SetState(control, AuthorizationState.Checked);
      }
      else if (type == AuthorizationType.AuthorizeView && control is AuthorizeView av)
      {
        SetState(control, result.Succeeded ? AuthorizationState.Passed : AuthorizationState.Failed);
      }
    }
    catch (Exception ex)
    {
      Dispatcher.UIThread.Post(() => ExceptionDispatchInfo.Capture(ex).Throw());
    }
  }

  /// <summary>
  ///   Sets visibility rules: allows a single policy plus multiple roles, but does not allow multiple policies.
  ///   <example>
  ///     <code>
  /// <![CDATA[
  /// <!-- Only check if authenticated -->
  /// <TextBox nav:Authorize.Visible="" />
  /// <!-- Only Policy (Positional) -->
  /// <TextBox nav:Authorize.Visible="CanEditData" />
  /// <!-- Policy + Roles (Positional) -->
  /// <TextBox nav:Authorize.Visible="CanEditData; Admin,Manager" />
  /// <!-- Explicit Keys (Order Independent) -->
  /// <TextBox nav:Authorize.Visible="Policy=CanEditData" />
  /// <TextBox nav:Authorize.Visible="Roles=Admin,Manager" />
  /// <TextBox nav:Authorize.Visible="Policy=CanEditData; Roles=Admin" />
  /// <!-- Short Keys -->
  /// <TextBox nav:Authorize.Visible="P=CanEditData; R=Admin" />
  /// ]]>
  /// </code>
  ///   </example>
  /// </summary>
  public static void SetVisible(Control element, string? value)
  {
    element.SetValue(VisibleProperty, value);
  }

  /// <summary>
  ///   Gets the Visible attached property.
  /// </summary>
  public static string? GetVisible(Control element)
  {
    return element.GetValue(VisibleProperty);
  }

  /// <summary>
  ///   Sets enable rules: allows a single policy plus multiple roles, but does not allow multiple policies.
  ///   <example>
  ///     <code>
  /// <![CDATA[
  /// <!-- Only check if authenticated -->
  /// <TextBox nav:Authorize.Enable="" />
  /// <!-- Only Policy (Positional) -->
  /// <TextBox nav:Authorize.Enable="CanEditData" />
  /// <!-- Policy + Roles (Positional) -->
  /// <TextBox nav:Authorize.Enable="CanEditData; Admin,Manager" />
  /// <!-- Explicit Keys (Order Independent) -->
  /// <TextBox nav:Authorize.Enable="Policy=CanEditData" />
  /// <TextBox nav:Authorize.Enable="Roles=Admin,Manager" />
  /// <TextBox nav:Authorize.Enable="Policy=CanEditData; Roles=Admin" />
  /// <!-- Short Keys -->
  /// <TextBox nav:Authorize.Enable="P=CanEditData; R=Admin" />
  /// ]]>
  /// </code>
  ///   </example>
  /// </summary>
  public static void SetEnable(Control element, string? value)
  {
    element.SetValue(EnableProperty, value);
  }

  /// <summary>
  ///   Gets the Enable attached property.
  /// </summary>
  public static string? GetEnable(Control element)
  {
    return element.GetValue(EnableProperty);
  }
}
