using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Helpers;
using Xunit;

namespace Everlong.Nester.Tests.Helpers;

/// <summary>
///   The Loaded-event wait (<see cref="ControlExtensions.EnsureLoadedAsync" />):
///   immediate when already loaded, waits for the event otherwise.
/// </summary>
public sealed class EnsureLoadedTests
{
  [AvaloniaFact]
  public async Task EnsureLoadedAsync_AlreadyLoaded_CompletesImmediately()
  {
    var control = new ContentControl();
    var window = new Window { Content = control };
    window.Show();
    try
    {
      await control.EnsureLoadedAsync();   // the window is showing — the control loads
    }
    finally
    {
      window.Close();
    }
  }

  [AvaloniaFact]
  public async Task EnsureLoadedAsync_WaitsForLoadedEvent()
  {
    var control = new ContentControl();   // detached — not loaded yet
    Task loaded = control.EnsureLoadedAsync();
    Assert.False(loaded.IsCompleted);

    var window = new Window { Content = control };
    window.Show();
    try
    {
      await loaded;   // completes when the control loads
    }
    finally
    {
      window.Close();
    }
  }
}
