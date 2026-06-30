using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Activation;

namespace NesterApp.Pages.Shell;

/// <summary>
///   The Avalonia part of the main-window Director — the shared partial
///   (<c>Pages/Shell/MainViewModel.cs</c>) holds the shell table, the startup
///   coroutine and the common activation decisions; this part adds the
///   Avalonia OS file-event intent (<see cref="StorageItemActivationIntent" />).
/// </summary>
public partial class MainViewModel
{
  /// <summary>
  /// If your app only ship to avalonia, you can use IAvaloniaShell which is also an IShell
  /// </summary>
  protected IAvaloniaShell AvaloniaShell => (IAvaloniaShell)Shell;


  /// <summary>Platform seam — the shared <see cref="MainViewModel.HandleAsync" /> routes unknown intents here.</summary>
  private partial ValueTask HandlePlatformIntentAsync(IntentContext context, IntentDelegate next)
  {
    switch (context.Intent)
    {
      case StorageItemActivationIntent { Files: { } files }:
        // OS file event — items are platform objects (files or folders).
        // Open content via IStorageFile.OpenReadAsync, or hand to the system:
        //   if (await shell.GetPlatformService<ILauncher>() is { } launcher)
        //     await launcher.LaunchUriAsync(item.Path);
        // The Director owns the items' lifetime (IStorageItem : IDisposable).
        break;
    }
    return next(context);
  }
}
