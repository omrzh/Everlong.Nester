using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

[assembly: AvaloniaTestApplication(typeof(Everlong.Nester.Tests.Hosting.LifecycleTestApp))]

namespace Everlong.Nester.Tests.Hosting;

public class LifecycleTestApp : Application
{
  public override void Initialize()
  {
    // The platform chrome themes — a real app merges these too.  Loading them
    // here lets headless tests exercise control templates and item containers.
    Styles.Add((Styles)AvaloniaXamlLoader.Load(
      new Uri("avares://Everlong.Nester.Avalonia/Themes/NesterTheme.axaml")));
    Styles.Add((Styles)AvaloniaXamlLoader.Load(
      new Uri("avares://Everlong.Nester.Extensions.Avalonia/Themes/NesterExtendedTheme.axaml")));
  }

  public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<LifecycleTestApp>()
      .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
