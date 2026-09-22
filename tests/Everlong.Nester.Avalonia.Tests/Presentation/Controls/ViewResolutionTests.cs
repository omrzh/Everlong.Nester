using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

// Avalonia hands a recycling template its previous child and asks whether that
// child is still applicable — a question only the template can answer.  A
// locator claims the application's whole mapping set, so it cannot answer it,
// and the locator is therefore a builder: one mount, one view.  The fixtures
// are the assembly's real generated locator (`TestViewLocator`), not a
// stand-in, so the pin covers the emitted code and the runtime together.

internal sealed class LocatorFirstModel;

internal sealed class LocatorSecondModel;

internal sealed class LocatorFirstView : TextBlock;

internal sealed class LocatorSecondView : TextBlock;

/// <summary>
///   Pins a locator's build contract: the view a presenter's child becomes is the
///   one the current content maps to, never the child it had before.
/// </summary>
public class ViewResolutionTests
{
  /// <summary>
  ///   Shows the presenter inside a window — a detached presenter drops its child
  ///   instead of rebuilding it, so the recycling question is never asked.
  /// </summary>
  private static ContentPresenter Show(ContentPresenter presenter)
  {
    var window = new Window { Content = presenter, Width = 100, Height = 100 };
    window.Show();
    return presenter;
  }

  [AvaloniaFact]
  public void Content_Of_A_Different_Type_Builds_Its_Own_View()
  {
    var presenter = Show(new ContentPresenter { ContentTemplate = new TestViewLocator() });

    presenter.Content = new LocatorFirstModel();
    Control? first = presenter.Child;

    var secondModel = new LocatorSecondModel();
    presenter.Content = secondModel;
    Control? second = presenter.Child;

    Assert.IsType<LocatorFirstView>(first);
    Assert.IsType<LocatorSecondView>(second);
    Assert.NotSame(first, second);
    Assert.Same(secondModel, second!.DataContext);
  }

  [AvaloniaFact]
  public void A_New_Instance_Of_The_Same_Type_Builds_Its_Own_View()
  {
    var presenter = Show(new ContentPresenter { ContentTemplate = new TestViewLocator() });

    presenter.Content = new LocatorFirstModel();
    Control? first = presenter.Child;

    presenter.Content = new LocatorFirstModel();
    Control? second = presenter.Child;

    Assert.IsType<LocatorFirstView>(first);
    Assert.IsType<LocatorFirstView>(second);
    Assert.NotSame(first, second);
  }

  [Fact]
  public void No_Shipped_Locator_Is_A_Recycling_Template()
  {
    // The contract: Avalonia is never offered a previous child to recycle — a
    // locator claims the whole mapping set, so it cannot answer whether the
    // child it built is still applicable.
    Assert.False(typeof(TestViewLocator).IsAssignableTo(typeof(IRecyclingDataTemplate)));
    Assert.False(typeof(NesterExtendedViewLocator).IsAssignableTo(typeof(IRecyclingDataTemplate)));
  }
}
