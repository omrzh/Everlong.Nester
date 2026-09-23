using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Everlong.Nester.Presentation;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

/// <summary>
///   The transition anchor annotation: an annotated button parks the
///   annotation's value in its window's plane on activation, and the plane
///   hands it back on a plain read — reading does not take it, so emptying
///   the slot is the reader's own.
/// </summary>
[Collection("RealShell")]
public class TransitionTests
{
  /// <summary>A button whose activation is driven without an input device.</summary>
  private sealed class ProbeButton : Button
  {
    public void Activate() => OnClick();
  }

  /// <summary>A command that answers on demand and never broadcasts a change.</summary>
  private sealed class QuietCommand : ICommand
  {
    public bool CanExecuteAnswer { get; set; } = true;

    public bool Executed { get; private set; }

    public bool CanExecute(object? parameter) => CanExecuteAnswer;

    public void Execute(object? parameter) => Executed = true;

    public event EventHandler? CanExecuteChanged { add { } remove { } }
  }

  [AvaloniaFact]
  public async Task Anchor_ParksTheAnnotatedValue()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var button = new ProbeButton();
      var card = new Border();
      Transition.SetAnchor(button, card);
      shell.Panel.Children.Add(button);

      button.Activate();

      Assert.Same(card, PlaneOf(shell).Anchor);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public async Task Anchor_ReadDoesNotTakeIt()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var button = new ProbeButton();
      var card = new Border();
      Transition.SetAnchor(button, card);
      shell.Panel.Children.Add(button);
      button.Activate();
      FlyingCanvas plane = PlaneOf(shell);

      // The contract the slot states: only the writer and Clear move it.
      Assert.Same(card, plane.Anchor);
      Assert.Same(card, plane.Anchor);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public async Task Anchor_XamlAnnotation_ParksTheValue()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var fixture = (UserControl)AvaloniaXamlLoader.Load(
        new Uri("avares://Everlong.Nester.Avalonia.Tests/Presentation/Controls/TransitionXaml.axaml"));
      var card = (Border)fixture.Content!;
      var grid = (Grid)card.Child!;
      var button = (Button)grid.Children[0];

      shell.Panel.Children.Add(fixture);

      // The annotation the templates carry, read back through the loader.
      Assert.Same(card, Transition.GetAnchor(button));

      button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

      Assert.Same(card, PlaneOf(shell).Anchor);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public async Task Anchor_LeavesTheSlotAlone_WhenTheCommandDeclines()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var command = new QuietCommand();
      var button = new ProbeButton { Command = command };
      var activated = false;
      button.Click += (_, _) => activated = true;
      Transition.SetAnchor(button, new Border());
      shell.Panel.Children.Add(button);

      // No broadcast: the button still reads as enabled, so the click is
      // raised and only the fresh CanExecute check can refuse the park.
      command.CanExecuteAnswer = false;

      button.Activate();

      Assert.True(activated);
      Assert.Null(PlaneOf(shell).Anchor);
      Assert.False(command.Executed);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  [AvaloniaFact]
  public async Task Anchor_Cleared_StopsParking()
  {
    var shell = RealShell.Create<RealShell.RealTestContext>();
    try
    {
      var button = new ProbeButton();
      var activated = false;
      button.Click += (_, _) => activated = true;
      Transition.SetAnchor(button, new Border());
      Transition.SetAnchor(button, null);
      shell.Panel.Children.Add(button);

      button.Activate();

      Assert.True(activated);
      Assert.Null(PlaneOf(shell).Anchor);
    }
    finally
    {
      await shell.Shell.DisposeAsync();
    }
  }

  private static FlyingCanvas PlaneOf(RealShell shell)
    => shell.Services.GetRequiredService<IFlyingLayer>().Canvas;
}
