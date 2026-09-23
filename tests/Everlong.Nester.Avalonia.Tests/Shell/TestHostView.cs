using Avalonia.Controls;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using PlatformControl = Avalonia.Controls.Control;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   Test stand-in for a real host view (a Window implementing
///   <see cref="IAvaloniaShellHost"/>): provides a logical content layer
///   (the framework mounts the stage into it).  The desktop protocol
///   requires the Director view to BE the host — test templates must
///   cooperate (the framework never falls back for uncooperative views; a
///   desktop shell without a host fails at host preparation).  Single-view
///   tests keep a non-host view on purpose (the framework connects the
///   MainView itself there).
/// </summary>
internal sealed class TestHostView : ContentControl, IAvaloniaShellHost
{
  private readonly ContentLayer _contentLayer = new();

  internal TestHostView()
  {
    // The stage must live inside this view's own tree — a mounted
    // navigation host resolves its shell by crawling the visual chain to
    // the stage (mirrors the template window's content layer; the
    // orphaned-layer shape would break view-side GetShell).
    Content = _contentLayer;
  }

  public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;

  /// <summary>The hosted surface (test/observer channel).</summary>
  internal ContentLayer ContentLayer => _contentLayer;
}
