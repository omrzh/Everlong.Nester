using Avalonia.Controls;
using Everlong.Nester.Controls;
using Everlong.Nester.Shell;
using PlatformControl = Avalonia.Controls.Control;

// The template's UiShell (compiled into the test project because
// Template.Shared page models call `new UiShell { DirectorType = ... }`
// directly) references the window types — both resolved via the [ViewFor]
// mapping (the shell delegates the root view to the view locator).
// The real window types are XAML code-behind (never compiled here); these
// stubs satisfy the compile-time dependency with the harness's host shape.
// The real-app suites drive TestShell<TDirector> instead, so the stubs are
// never presented.
namespace NesterApp.Pages.Shell
{
  public partial class MainWindow : Window, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;

    internal IAvaloniaShell? Shell { get; init; }   // wired by the template shell's PrepareHost
  }
}

namespace NesterApp.Pages.Login
{
  public partial class LoginWindow : Window, IAvaloniaShellHost
  {
    private readonly ContentLayer _contentLayer = new();

    public void HostShell(IAvaloniaShell shell, PlatformControl stage) => _contentLayer.Content = stage;

    internal IAvaloniaShell Shell { get; init; } = null!;   // wired by the template shell's PrepareHost
  }
}
