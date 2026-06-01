// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

using Everlong.Nester.Layer;

namespace Everlong.Nester.Controls;

/// <summary>
///   The platform lease — <see cref="LayerLeaseBase"/> over the concrete
///   <see cref="ContentLayer"/> surface.
/// </summary>
internal sealed class ContentLayerLease : LayerLeaseBase
{
  internal ContentLayerLease(ContentLayer surface, ILayerLedger ledger, int z)
    : base(ledger, z)
  {
    Surface = surface;
#if AVALONIA
    surface.ZIndex = z;
#else
    System.Windows.Controls.Canvas.SetZIndex(surface, z);
#endif
  }

  /// <summary>The mounted surface (the mount hook's extraction channel).</summary>
  internal ContentLayer Surface { get; }

  /// <inheritdoc />
  public override object? Content { get => Surface.Content; set => Surface.Content = value; }

#if AVALONIA
  /// <inheritdoc />
  public override bool IsVisible { get => Surface.IsVisible; set => Surface.IsVisible = value; }
#elif WPF
  /// <inheritdoc />
  public override bool IsVisible
  {
    get => Surface.Visibility != System.Windows.Visibility.Collapsed;
    set => Surface.Visibility = value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
  }
#endif
}
