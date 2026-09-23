using System.Runtime.ExceptionServices;
using System.Windows.Threading;
using System.Windows;
using Everlong.Nester.Auth;
using Everlong.Nester.Shell;
using Microsoft.Extensions.DependencyInjection;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Provides attached properties to control UI element visibility based on authorization.
///   The authorization service is resolved from the shell scope of the control's stage
///   (<see cref="GetAuthorizationService"/>); annotated controls subscribe to the service's
///   <see cref="IAuthService.AuthorizationChanged"/> while loaded and unsubscribe on unload.
/// </summary>
public static class Authorize
{
  /// <summary>
  ///   Defines the Visible attached property.
  ///   Controls the <see cref="UIElement.IsVisible" /> property based on authorization.
  /// </summary>
  public static readonly DependencyProperty VisibleProperty =
    DependencyProperty.RegisterAttached("Visible", typeof(string), typeof(Authorize),
                                        new PropertyMetadata(null, OnVisibleChanged));

  /// <summary>
  ///   Defines the Enable attached property.
  ///   Controls the <see cref="UIElement.IsEnabled" /> property based on authorization.
  /// </summary>
  public static readonly DependencyProperty EnableProperty =
    DependencyProperty.RegisterAttached("Enable", typeof(string), typeof(Authorize),
                                        new PropertyMetadata(null, OnEnableChanged));

  internal static readonly DependencyProperty StateProperty =
    DependencyProperty.RegisterAttached("State", typeof(AuthorizationState), typeof(Authorize),
                                        new PropertyMetadata(AuthorizationState.Skipped));

  // Guards the per-control Loaded/Unloaded subscription made when a control
  // is annotated (SetState with Pending); the subscriptions drive the
  // mount-time check and the unload unsubscribe.
  private static readonly DependencyProperty ArmedProperty =
    DependencyProperty.RegisterAttached("Armed", typeof(bool), typeof(Authorize),
                                        new PropertyMetadata(false));

  // The service an annotated control is currently bound to — the strong
  // subscription ends at unload (Unbind clears it), so no weak registry.
  internal static readonly DependencyProperty BindingProperty =
    DependencyProperty.RegisterAttached("AuthBinding", typeof(ServiceBinding), typeof(Authorize),
                                        new PropertyMetadata(null));

  /// <summary>The strong per-control service subscription — unsubscribed on unload.</summary>
  internal sealed class ServiceBinding
  {
    internal ServiceBinding(IAuthService service, EventHandler handler)
    {
      Service = service;
      Handler = handler;
    }

    internal IAuthService Service { get; }

    internal EventHandler Handler { get; }
  }

  /// <summary>
  ///   Gets the authorization service of the control's shell scope — the
  ///   stage's shell container (<see cref="IShell.Services"/>);
  ///   <see langword="null"/> when the control is not under a shell's stage
  ///   or the shell does not register the service.
  /// </summary>
  public static IAuthService? GetAuthorizationService(DependencyObject element)
    => element is PControl control ? control.GetShell()?.Services.GetService<IAuthService>() : null;

  private static void OnLoaded(object sender, RoutedEventArgs e)
  {
    if (sender is PControl control && control.IsLoaded)
    {
      Bind(control);
    }
  }

  private static void OnUnloaded(object sender, RoutedEventArgs e)
  {
    if (sender is PControl control)
    {
      Unbind(control);
    }
  }

  /// <summary>
  ///   Sets visibility rules: allows a single policy plus multiple roles, but does not allow multiple policies.
  ///   <example>
  ///     <code>
  /// <![CDATA[
  /// <!-- Only check if authenticated -->
  /// <!-- "n:" stands for "Everlong.Nester.Presentation" namespace -->
  /// <TextBox n:Authorize.Visible="" />
  /// <!-- Only Policy (Positional) -->
  /// <TextBox n:Authorize.Visible="CanEditData" />
  /// <!-- Policy + Roles (Positional) -->
  /// <TextBox n:Authorize.Visible="CanEditData; Admin,Manager" />
  /// <!-- Explicit Keys (Order Independent) -->
  /// <TextBox n:Authorize.Visible="Policy=CanEditData" />
  /// <TextBox n:Authorize.Visible="Roles=Admin,Manager" />
  /// <TextBox n:Authorize.Visible="Policy=CanEditData; Roles=Admin" />
  /// <!-- Short Keys -->
  /// <TextBox n:Authorize.Visible="P=CanEditData; R=Admin" />
  /// ]]>
  /// </code>
  ///   </example>
  /// </summary>
  public static void SetVisible(DependencyObject element, string? value)
  {
    element.SetValue(VisibleProperty, value);
  }

  /// <summary>
  ///   Gets the Visible attached property.
  /// </summary>
  public static string? GetVisible(DependencyObject element)
  {
    return (string?)element.GetValue(VisibleProperty);
  }

  /// <summary>
  ///   Sets enable rules: allows a single policy plus multiple roles, but does not allow multiple policies.
  ///   <example>
  ///     <code>
  /// <![CDATA[
  /// <!-- Only check if authenticated -->
  /// <!-- "n:" stands for "Everlong.Nester.Presentation" namespace -->
  /// <TextBox n:Authorize.Enable="" />
  /// <!-- Only Policy (Positional) -->
  /// <TextBox n:Authorize.Enable="CanEditData" />
  /// <!-- Policy + Roles (Positional) -->
  /// <TextBox n:Authorize.Enable="CanEditData; Admin,Manager" />
  /// <!-- Explicit Keys (Order Independent) -->
  /// <TextBox n:Authorize.Enable="Policy=CanEditData" />
  /// <TextBox n:Authorize.Enable="Roles=Admin,Manager" />
  /// <TextBox n:Authorize.Enable="Policy=CanEditData; Roles=Admin" />
  /// <!-- Short Keys -->
  /// <TextBox n:Authorize.Enable="P=CanEditData; R=Admin" />
  /// ]]>
  /// </code>
  ///   </example>
  /// </summary>
  public static void SetEnable(DependencyObject element, string? value)
  {
    element.SetValue(EnableProperty, value);
  }


  /// <summary>
  ///   Gets the Enable attached property.
  /// </summary>
  public static string? GetEnable(DependencyObject element)
  {
    return (string?)element.GetValue(EnableProperty);
  }

  internal static void SetState(DependencyObject element, AuthorizationState value)
  {
    if (value == AuthorizationState.Pending
        && element.GetValue(ArmedProperty) is false
        && element is PControl control)
    {
      // WPF's Loaded broadcast is skipped for elements without an instance
      // handler (BroadcastEventHelper only fires Loaded when
      // SubtreeHasLoadedChangeHandler is set — class handlers alone do not
      // set it).  Arm instance handlers so the mount-time check runs.
      control.SetValue(ArmedProperty, true);
      control.Loaded += OnLoaded;
      control.Unloaded += OnUnloaded;
    }

    element.SetValue(StateProperty, value);
  }

  internal static AuthorizationState GetState(DependencyObject element)
  {
    return (AuthorizationState)element.GetValue(StateProperty);
  }

  private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is PControl fe)
    {
      CoerceByType(fe, (string?)e.NewValue, AuthorizationType.Visible);
    }
  }

  private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is PControl fe)
    {
      CoerceByType(fe, (string?)e.NewValue, AuthorizationType.Enable);
    }
  }

  private static void CoerceByType(PControl obj, string? value, AuthorizationType type)
  {
    if (value != null)
    {
      if (obj is not AuthorizeView)
      {
        SetState(obj, AuthorizationState.Pending);
      }

      switch (type)
      {
        case AuthorizationType.Visible:
          obj.Visibility = Visibility.Collapsed;
          break;
        case AuthorizationType.Enable:
          obj.IsEnabled = false;
          break;
        case AuthorizationType.AuthorizeView:
          break;
      }

      // Authorization check runs once the control is loaded and the shell's
      // auth service is reachable through the shell scope.
      if (obj.IsLoaded)
      {
        Bind(obj);
      }
    }
  }

  /// <summary>
  ///   Binds the control to its shell scope's service — subscribes to
  ///   <see cref="IAuthService.AuthorizationChanged"/> and runs the initial
  ///   check.  A control already bound to the same service is refreshed only.
  /// </summary>
  private static void Bind(PControl control)
  {
    if (GetState(control) == AuthorizationState.Skipped)
    {
      return;
    }

    ServiceBinding? binding = (ServiceBinding?)control.GetValue(BindingProperty);
    IAuthService? service = binding?.Service ?? GetAuthorizationService(control);
    if (service is null)
    {
      return;
    }

    if (binding is null || !ReferenceEquals(binding.Service, service))
    {
      binding?.Service.AuthorizationChanged -= binding.Handler;

      Dispatcher dispatcher = control.Dispatcher;
      var fresh = new ServiceBinding(service, (_, _) =>
        dispatcher.InvokeAsync(() => Recheck(control, service)));
      service.AuthorizationChanged += fresh.Handler;
      control.SetValue(BindingProperty, fresh);
    }

    UpdateCheck(control, service);
  }

  /// <summary>Unsubscribes the control's service binding — the strong subscription ends here.</summary>
  private static void Unbind(PControl control)
  {
    if (control.GetValue(BindingProperty) is not ServiceBinding binding)
    {
      return;
    }

    binding.Service.AuthorizationChanged -= binding.Handler;
    control.SetValue(BindingProperty, null);
  }

  private static void Recheck(PControl control, IAuthService service)
  {
    // The control may have unloaded while the dispatch was queued — its
    // binding is gone then (re-armed on the next load).
    if (control.IsLoaded
        && control.GetValue(BindingProperty) is ServiceBinding current
        && ReferenceEquals(current.Service, service))
    {
      UpdateCheck(control, service);
    }
  }

  private static void UpdateCheck(PControl control, IAuthService service)
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

  internal static void PerformCheck(PControl control,
                                    string roleAndPolicy,
                                    IAuthService service,
                                    AuthorizationType type)
  {
    try
    {
      (string? policy, string? roles) = AuthStringParser.ParseAuthInfo(roleAndPolicy);
      AuthorizationResult result = service.Authorize(roles, policy);

      if (type == AuthorizationType.Visible)
      {
        control.Visibility = result.Succeeded ? Visibility.Visible : Visibility.Collapsed;
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
      control.Dispatcher.InvokeAsync(() => ExceptionDispatchInfo.Capture(ex).Throw());
    }
  }
}
