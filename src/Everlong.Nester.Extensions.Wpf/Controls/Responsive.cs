using System.Windows;

namespace Everlong.Nester.Controls;

/// <summary>
///   Classifies an element by the width of the layout slot it was given —
///   while the slot is narrower than <see cref="CompactBelowProperty" />, the
///   element reports <see cref="IsCompactProperty" />.
/// </summary>
/// <remarks>
///   The classification reads the element's own arranged width, so a control
///   placed in a narrow slot is narrow whatever the window does.  A width of
///   zero means "not measured yet": the element keeps the full form until a
///   layout pass gives it a slot.  WPF has no style classes, so the state is a
///   read-only attached property a template reads through a trigger.
/// </remarks>
public static partial class Responsive
{
  /// <summary>
  ///   The element's slot width below which it reports
  ///   <see cref="IsCompactProperty" />, in device-independent pixels.  Zero
  ///   disables the classification.
  /// </summary>
  public static readonly DependencyProperty CompactBelowProperty =
    DependencyProperty.RegisterAttached("CompactBelow", typeof(double), typeof(Responsive),
                                        new FrameworkPropertyMetadata(0d, OnCompactBelowChanged));

  private static readonly DependencyPropertyKey IsCompactKey =
    DependencyProperty.RegisterAttachedReadOnly("IsCompact", typeof(bool), typeof(Responsive),
                                                new FrameworkPropertyMetadata(false));

  /// <summary>Whether the element's slot is narrower than <see cref="CompactBelowProperty" />.</summary>
  public static readonly DependencyProperty IsCompactProperty = IsCompactKey.DependencyProperty;

  // Whether the element asked for a threshold, so the size subscription is
  // live.  Held on the element itself — no registry, nothing survives it.
  private static readonly DependencyProperty ArmedProperty =
    DependencyProperty.RegisterAttached("Armed", typeof(bool), typeof(Responsive),
                                        new PropertyMetadata(false));

  /// <summary>Sets <see cref="CompactBelowProperty" />.</summary>
  public static void SetCompactBelow(DependencyObject element, double value)
    => element.SetValue(CompactBelowProperty, value);

  /// <summary>Gets <see cref="CompactBelowProperty" />.</summary>
  public static double GetCompactBelow(DependencyObject element)
    => (double)element.GetValue(CompactBelowProperty);

  /// <summary>Gets <see cref="IsCompactProperty" />.</summary>
  public static bool GetIsCompact(DependencyObject element)
    => (bool)element.GetValue(IsCompactProperty);

  private static void OnCompactBelowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not FrameworkElement element)
    {
      return;
    }

    bool armed = e.NewValue is double threshold && threshold > 0;
    if (armed == (bool)element.GetValue(ArmedProperty))
    {
      if (armed)
      {
        Update(element, element.ActualWidth);
      }

      return;
    }

    element.SetValue(ArmedProperty, armed);
    if (armed)
    {
      element.SizeChanged += OnSizeChanged;
      Update(element, element.ActualWidth);
    }
    else
    {
      element.SizeChanged -= OnSizeChanged;
      element.SetValue(IsCompactKey, false);
    }
  }

  private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
  {
    if (sender is FrameworkElement element)
    {
      Update(element, e.NewSize.Width);
    }
  }

  private static void Update(FrameworkElement element, double width)
  {
    double threshold = GetCompactBelow(element);
    element.SetValue(IsCompactKey, IsCompactSlot(threshold, width));
  }
}
