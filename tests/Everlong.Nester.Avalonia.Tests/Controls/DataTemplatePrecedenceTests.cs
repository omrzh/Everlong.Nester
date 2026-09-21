using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Everlong.Nester.Tests.Controls;

/// <summary>
///   Pins how the template list is read: Avalonia's own lookup takes the first
///   entry first and walks the logical tree before the application collection,
///   and the framework resolves through that same lookup.  A change in either
///   silently reorders the chain — the extension locator, the generated
///   mappings and the consumer hook — without breaking any other test.
/// </summary>
/// <remarks>
///   The WPF half is fixed here without an assertion of its own: its locator
///   walks the same declared list last entry first, mirroring the platform,
///   which resolves an implicit template out of the merged resource
///   dictionaries that way — the order was measured on a live WPF host.  This
///   repository has no WPF headless test host, so the order stays a stated fact
///   rather than a test, and adding a head for it is decided against.
/// </remarks>
public class DataTemplatePrecedenceTests
{
  private sealed class Model;

  private sealed class Probe(string name) : IDataTemplate
  {
    public string Name { get; } = name;

    public Control? Build(object? param) => new TextBlock { Text = Name };

    public bool Match(object? data) => data is Model;
  }

  [AvaloniaFact]
  public void First_Added_Template_Should_Win_Inside_One_Collection()
  {
    var first = new Probe("first");
    var second = new Probe("second");
    var host = new ContentControl();
    host.DataTemplates.Add(first);
    host.DataTemplates.Add(second);

    Assert.Same(first, host.FindDataTemplate(new Model()));
  }

  [AvaloniaFact]
  public void Nearest_Host_Should_Win_Over_An_Ancestor()
  {
    var farther = new Probe("farther");
    var nearer = new Probe("nearer");
    var parent = new ContentControl();
    parent.DataTemplates.Add(farther);
    var host = new ContentControl();
    host.DataTemplates.Add(nearer);
    parent.Content = host;

    Assert.Same(nearer, host.FindDataTemplate(new Model()));
  }

  [AvaloniaFact]
  public void Application_Templates_Should_Be_Considered_Last()
  {
    var global = new Probe("global");
    var local = new Probe("local");
    var host = new ContentControl();
    host.DataTemplates.Add(local);
    Application.Current!.DataTemplates.Add(global);
    try
    {
      Assert.Same(local, host.FindDataTemplate(new Model()));
    }
    finally
    {
      Application.Current.DataTemplates.Remove(global);
    }
  }
}
