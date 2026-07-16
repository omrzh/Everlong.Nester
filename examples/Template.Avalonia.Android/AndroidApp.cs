using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace NesterApp.Android;

/// <summary>
///   The Android application class — Avalonia 12 shape: the app-level
///   initializer (AppBuilder) lives here, not on the activity.  The shared
///   App project (Template.Avalonia) carries the whole bootstrap flow — the
///   shell assembly runs from <see cref="App.OnFrameworkInitializationCompleted"/>
///   (single-view path: AppShell's direct mount — the stage becomes the
///   MainView via <c>IActivityApplicationLifetime.MainViewFactory</c>).
/// </summary>
[Application]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
  protected AndroidApp(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
  {
  }

  protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
  {
    return base.CustomizeAppBuilder(builder)
      .WithInterFont()
      .ConfigureFonts(fontManager => fontManager.AddFontCollection(new CjkFontCollection()));
  }
}
