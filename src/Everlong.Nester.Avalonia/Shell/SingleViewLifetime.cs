// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Everlong.Nester.Shell;

/// <summary>
///   The single-view presentation surface — one abstraction over the two
///   Avalonia shapes: the browser's <see cref="ISingleViewApplicationLifetime"/>
///   (a single <c>MainView</c>) and Android's <see cref="IActivityApplicationLifetime"/>
///   (a <c>MainViewFactory</c> — Android can create multiple activity
///   instances, so the surface is a factory).  Tracks the last attached view
///   so the "still owned by this shell?" guard works even where the lifetime
///   exposes no getter (Android).
/// </summary>
internal static class SingleViewLifetime
{
  private static Control? _attached;

  /// <summary>True when the app runs a single-view surface (browser / Android).</summary>
  public static bool IsSingleView(IApplicationLifetime? lifetime)
    => lifetime is ISingleViewApplicationLifetime or IActivityApplicationLifetime;

  /// <summary>Attaches (or detaches) the single view; remembers it for the ownership guard.</summary>
  public static void SetMainView(IApplicationLifetime? lifetime, Control? view)
  {
    switch (lifetime)
    {
      case ISingleViewApplicationLifetime singleView:
        singleView.MainView = view;
        break;
      case IActivityApplicationLifetime activity:
        activity.MainViewFactory = view is null ? null : () => view;
        break;
    }

    _attached = view;
  }

  /// <summary>
  ///   Detaches the current view when it still belongs to <paramref name="owned"/>
  ///   — a replaced shell must never clear the new shell's view.
  /// </summary>
  public static bool ClearIfOwned(IApplicationLifetime? lifetime, object? owned)
  {
    if (!ReferenceEquals(_attached, owned))
      return false;

    SetMainView(lifetime, null);
    return true;
  }
}
