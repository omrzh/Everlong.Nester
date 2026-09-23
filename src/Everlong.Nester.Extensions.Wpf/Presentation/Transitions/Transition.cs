using System.Windows;
using System.Windows.Controls;
using Everlong.Nester.Shell;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Marks the annotated <see cref="Button" /> as the anchor of a transition:
///   activating it parks the annotation's value in the flying plane's
///   <see cref="FlyingCanvas.Anchor" />.
/// </summary>
/// <remarks>
///   The park runs inside the button's <c>Click</c>, which the button raises
///   before it executes its command — so a director that runs because of that
///   command reads the anchor after this wrote it, in the same turn.
///   <para>
///   The value is parked as it is: the view layer hands over whatever it wants
///   flown, and the plane neither resolves nor validates it.  A null value
///   annotates nothing.
///   </para>
///   <para>
///   A click whose command declines parks nothing: the fresh <c>CanExecute</c>
///   is asked here, because the button's own enabled state is the cached
///   answer a command is free never to refresh.
///   </para>
/// </remarks>
public static class Transition
{
  /// <summary>Defines the Anchor attached property — the value to park.</summary>
  public static readonly DependencyProperty AnchorProperty =
    DependencyProperty.RegisterAttached("Anchor", typeof(object), typeof(Transition),
                                        new PropertyMetadata(null, OnAnchorChanged));

  /// <summary>Sets the Anchor attached property.</summary>
  public static void SetAnchor(DependencyObject element, object? value) => element.SetValue(AnchorProperty, value);

  /// <summary>Gets the Anchor attached property.</summary>
  public static object? GetAnchor(DependencyObject element) => element.GetValue(AnchorProperty);

  private static void OnAnchorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not Button button)
      return;

    // Idempotent on purpose: a repeated set must not stack a second handler.
    button.Click -= OnClick;

    if (e.NewValue is not null)
      button.Click += OnClick;
  }

  private static void OnClick(object sender, RoutedEventArgs e)
  {
    if (sender is not Button button
        || button.GetFlyingCanvas() is not { } plane
        || button.Command is { } command && !command.CanExecute(button.CommandParameter))
    {
      return;
    }

    plane.Anchor = GetAnchor(button);
  }
}
