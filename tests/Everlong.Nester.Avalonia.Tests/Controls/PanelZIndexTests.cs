using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Everlong.Nester.Tests.Controls;

public class PanelZIndexTests
{
  [AvaloniaFact]
  public void Changing_ZIndex_Should_Trigger_Specific_Events()
  {
    // Arrange
    var window = new Window();
    var panel = new Panel(); // Default Panel
    var child1 = new Button { Content = "Child 1" };
    var child2 = new Button { Content = "Child 2" };

    panel.Children.Add(child1);
    panel.Children.Add(child2);
    window.Content = panel;

    var events = new List<string>();

    void LogEvent(object? sender, string name)
    {
      var btn = sender as Button;
      events.Add($"{btn?.Content}_{name}");
    }

    child1.AttachedToVisualTree += (s, e) => LogEvent(s, "AttachedToVisualTree");
    child1.DetachedFromVisualTree += (s, e) => LogEvent(s, "DetachedFromVisualTree");
    child1.Loaded += (s, e) => LogEvent(s, "Loaded");
    child1.Unloaded += (s, e) => LogEvent(s, "Unloaded");
    child1.PropertyChanged += (s, e) =>
    {
      if (e.Property.Name == "ZIndex")
      {
        LogEvent(s, "ZIndexChanged");
      }
    };

    window.Show();

    // Clear initial load events
    events.Clear();

    // Act
    // Change ZIndex of child1
    child1.ZIndex = 10;

    // Force a layout pass/render frame just in case (Headless might need it to process events)
    // Usually property changes are immediate for events, but rendering might differ.
    // However, Loaded/Unloaded are about Visual Tree attachment.

    // Assert
    // I expect only PropertyChanged for ZIndex.
    // No Loaded/Unloaded/Attached/Detached should fire.

    Assert.DoesNotContain("Child 1_Loaded", events);
    Assert.DoesNotContain("Child 1_Unloaded", events);
    Assert.DoesNotContain("Child 1_AttachedToVisualTree", events);
    Assert.DoesNotContain("Child 1_DetachedFromVisualTree", events);

    Assert.Contains("Child 1_ZIndexChanged", events);
  }
}
