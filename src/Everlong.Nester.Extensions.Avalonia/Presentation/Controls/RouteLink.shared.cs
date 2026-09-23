// NOTE: Single-source file — the Extensions.Wpf project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using System.ComponentModel;
using Everlong.Nester.RouteSync;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Shared logic for <see cref="RouteLink" /> on Avalonia and WPF.  A link
///   binds to its routing surface (the nearest <see cref="IRoutingView" />
///   host), follows the surface router's <see cref="IRouterStack" /> site
///   changes, and issues the item's navigation request on activation.
/// </summary>
public partial class RouteLink
{
  private IRouter? _surfaceRouter;
  private IRouterStack? _surfaceStack;

  /// <summary>
  ///   Raised when the link is activated, after its navigation request has
  ///   been issued.
  /// </summary>
  public event EventHandler? Click;

  /// <summary>The surface's currently presented site — the value this link matches against; <see langword="null" /> before it binds to a surface.</summary>
  private ILocation? CurrentSite => _surfaceRouter?.Stack.Location;

  private bool IsMatch(ILocation? current)
  {
    if (DataContext is not IRouteItem item || current is null)
    {
      return false;
    }

    return RouteItemMatch.IsHighlighted(item, current);
  }

  /// <summary>
  ///   Issues the navigation request carried by the current <c>DataContext</c>
  ///   and raises <see cref="Click" />.
  /// </summary>
  private void HandleClickLogic()
  {
    // The nav chrome addresses its own surface's router directly — a
    // navigation request is a directive, not a consultable intent.
    if (DataContext is IRouteItem { Destination: { } destination } && _surfaceRouter is { } router)
    {
      _ = router.RouteAsync(destination);
    }

    Click?.Invoke(this, EventArgs.Empty);
  }

  /// <summary>
  ///   Binds to the routing surface — resolves the surface's router by
  ///   walking ancestors to the <see cref="IRoutingView" /> host, follows the
  ///   stack's site changes and refreshes the active state.  Called by the
  ///   platform attach hooks; a no-op when already bound.
  /// </summary>
  private void BindSurface()
  {
    if (_surfaceStack is not null)
    {
      return;
    }

    _surfaceRouter = RoutingSurface.FindRouter(this);
    if (_surfaceRouter is null)
    {
      return;
    }

    _surfaceStack = _surfaceRouter.Stack;
    _surfaceStack.PropertyChanged += OnStackPropertyChanged;
    UpdateActive();
    UpdateDisplay();
  }

  /// <summary>Unbinds from the surface — called by the platform detach hooks; a no-op when not bound.</summary>
  private void UnbindSurface()
  {
    _surfaceStack?.PropertyChanged -= OnStackPropertyChanged;

    _surfaceStack = null;
    _surfaceRouter = null;
  }

  private void OnStackPropertyChanged(object? sender, PropertyChangedEventArgs e)
  {
    // The stack raises its whole surface on every landing — the site is the
    // only member the active state follows.
    if (e.PropertyName == nameof(IRouterStack.Location))
    {
      UpdateActive();
    }
  }

  private void UpdateActive()
  {
    IsRouteHighlighted = IsMatch(CurrentSite);
  }

  /// <summary>Recomputes the effective highlight — the app override wins when set.</summary>
  private void RefreshEffective()
  {
    IsActive = IsHighlighted ?? IsRouteHighlighted;
  }

  private void UpdateDisplay()
  {
    if (Content is not null)
    {
      UseDefaultContent = false;
      return;
    }

    if (DataContext is IRouteItem item)
    {
      Title = item.Title;
      Icon = item.Icon;
      HasIcon = Icon is not null;
    }
    else if (DataContext is not null)
    {
      Title = DataContext.ToString() ?? string.Empty;
      Icon = null;
      HasIcon = false;
    }
    else
    {
      Title = string.Empty;
      Icon = null;
      HasIcon = false;
    }

    UseDefaultContent = true;
  }

  private void OnDataContextChanged()
  {
    UpdateActive();
    UpdateDisplay();
  }

  private void OnContentChanged()
  {
    UseDefaultContent = Content is null;
  }
}
