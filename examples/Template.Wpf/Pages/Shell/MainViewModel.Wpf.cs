using Everlong.Nester.Intent;
using Everlong.Nester.Shell;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The WPF part of the main-window Director — the shared partial
///   (<c>Pages/Shell/MainViewModel.cs</c>) holds the shell table, the startup
///   coroutine and the common activation decisions; this part currently adds
///   no platform intent cases (the seam stays for the pattern).
/// </summary>
public partial class MainViewModel
{
  /// <summary>
  /// If your app only ship to Wpf, you can use IWpfShell which is also an IShell
  /// </summary>
  protected IWpfShell WpfShell => (IWpfShell)Shell;

  /// <summary>Platform seam — the shared <see cref="MainViewModel.HandleAsync" /> routes unknown intents here.</summary>
  private partial ValueTask HandlePlatformIntentAsync(IntentContext context, IntentDelegate next)
    => next(context);
}
