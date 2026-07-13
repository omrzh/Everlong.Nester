using Everlong.Nester.Intent;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The Terminal.Gui part of the main-window Director — the shared partial
///   routes unknown intents here; the terminal surface has no extra intent
///   family, so everything passes through.
/// </summary>
public partial class MainViewModel
{
  /// <summary>Platform seam — the shared <see cref="MainViewModel.HandleAsync" /> routes unknown intents here.</summary>
  private partial ValueTask HandlePlatformIntentAsync(IntentContext context, IntentDelegate next)
    => next(context);
}
