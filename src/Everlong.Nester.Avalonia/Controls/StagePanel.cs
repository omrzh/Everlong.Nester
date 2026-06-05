// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Controls;

/// <summary>
///   The stage panel — the visual surface that hosts the layer floors and
///   their content slots, and carries the owning shell's identity.  Its
///   public face is <see cref="IShellStage" />.
/// </summary>
internal class StagePanel : PGrid, IShellStage, ILayerStage
{
  /// <inheritdoc />
  public IShell Shell { get; internal set; } = null!;

  private readonly Dictionary<ContentLayer, LayerListener> _listeners = [];

  /// <summary>
  ///   Mounts a tenant's lease surface onto the stage and connects its layer
  ///   listener (content replacement and policy changes recompute the tab
  ///   reachability of every layer).
  /// </summary>
  public void MountLease(ILayerLease lease)
  {
    ContentLayer surface = ((ContentLayerLease)lease).Surface;
    Children.Add(surface);
    _listeners[surface] = new LayerListener(this, surface);
  }

  /// <summary>Detaches a tenant's lease surface from the stage and recomputes the layers' tab reachability.</summary>
  public void UnmountLease(ILayerLease lease)
  {
    ContentLayer surface = ((ContentLayerLease)lease).Surface;
    if (_listeners.Remove(surface, out LayerListener? listener))
      listener.Detach();
    Children.Remove(surface);
    RecomputeTabModes();
  }

  /// <summary>
  ///   Recomputes the layers' Tab modes, topmost first: a layer whose content
  ///   declares no intent is excluded; a trapped layer cycles and excludes
  ///   every layer beneath it; a reachable layer cycles.  The mode is applied
  ///   to the layer surface itself — the content may be any object the
  ///   surface's template renders.
  /// </summary>
  private void RecomputeTabModes()
  {
    bool excluded = false;
    foreach (ContentLayer layer in Children.OfType<ContentLayer>().OrderByDescending(LayerZ))
    {
      FocusPolicy? policy = (layer.Content as IFocusPolicySurface)?.FocusPolicy;
      bool trapped = !excluded && policy == FocusPolicy.Trapped;
      bool reachable = !excluded && policy == FocusPolicy.Reachable;
      if (trapped)
        excluded = true;

      if (!trapped && !reachable)
      {
        SetTabMode(layer, TabMode.Excluded);
        continue;
      }

      SetTabMode(layer, TabMode.Cycle);
    }
  }

  private enum TabMode
  {
    Excluded,
    Cycle,
  }

  private static void SetTabMode(ContentLayer layer, TabMode mode)
  {
#if AVALONIA
    var value = mode == TabMode.Excluded
      ? Avalonia.Input.KeyboardNavigationMode.None
      : Avalonia.Input.KeyboardNavigationMode.Cycle;
    Avalonia.Input.KeyboardNavigation.SetTabNavigation(layer, value);
#else
    var value = mode == TabMode.Excluded
      ? System.Windows.Input.KeyboardNavigationMode.None
      : System.Windows.Input.KeyboardNavigationMode.Cycle;
    System.Windows.Input.KeyboardNavigation.SetTabNavigation(layer, value);
#endif
  }

  private static int LayerZ(ContentLayer layer)
  {
#if AVALONIA
    return layer.ZIndex;
#else
    return System.Windows.Controls.Canvas.GetZIndex(layer);
#endif
  }

  /// <summary>
  ///   The per-layer event wiring: content replacement and the content's
  ///   policy changes both feed the stage's tab recompute.  Detached on
  ///   unmount.
  /// </summary>
  private sealed class LayerListener
  {
    private readonly StagePanel _stage;
    private readonly ContentLayer _layer;
    private IFocusPolicySurface? _content;

    internal LayerListener(StagePanel stage, ContentLayer layer)
    {
      _stage = stage;
      _layer = layer;
      _layer.ContentReplaced += OnContentReplaced;
      AttachContent();
      _stage.RecomputeTabModes();
    }

    internal void Detach()
    {
      _layer.ContentReplaced -= OnContentReplaced;
      DetachContent();
    }

    private void OnContentReplaced()
    {
      DetachContent();
      AttachContent();
      _stage.RecomputeTabModes();
    }

    private void AttachContent()
    {
      _content = _layer.Content as IFocusPolicySurface;
      if (_content is not null)
        _content.FocusPolicyChanged += _stage.RecomputeTabModes;
    }

    private void DetachContent()
    {
      if (_content is not null)
        _content.FocusPolicyChanged -= _stage.RecomputeTabModes;
      _content = null;
    }
  }
}
