using Avalonia;
using Avalonia.Controls;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Classifies an element by the width of the layout slot it was given —
///   while the slot is narrower than <see cref="CompactBelowProperty" />, the
///   element carries <see cref="CompactClass" />.
/// </summary>
/// <remarks>
///   The classification reads the element's own arranged width and is measured
///   per element, so a control placed in a narrow slot classifies as narrow
///   whatever the window does.  A width of zero means "not measured yet": the
///   element keeps the full form until a layout pass gives it a slot.
/// </remarks>
public static partial class Responsive
{
  /// <summary>The class name carried while the element's slot is narrower than <see cref="CompactBelowProperty" />.</summary>
  public const string CompactClass = "compact";

  /// <summary>
  ///   The element's slot width below which it carries
  ///   <see cref="CompactClass" />, in device-independent pixels.  Zero
  ///   disables the classification.
  /// </summary>
  public static readonly AttachedProperty<double> CompactBelowProperty =
    AvaloniaProperty.RegisterAttached<PControl, double>("CompactBelow", typeof(Responsive));

  // Whether the element asked for a threshold, so the size subscription is
  // live.  Held on the element itself — no registry, nothing survives it.
  private static readonly AttachedProperty<bool> ArmedProperty =
    AvaloniaProperty.RegisterAttached<PControl, bool>("Armed", typeof(Responsive));

  static Responsive()
  {
    CompactBelowProperty.Changed.AddClassHandler<PControl>(OnCompactBelowChanged);
  }

  /// <summary>Sets <see cref="CompactBelowProperty" />.</summary>
  public static void SetCompactBelow(PControl element, double value)
    => element.SetValue(CompactBelowProperty, value);

  /// <summary>Gets <see cref="CompactBelowProperty" />.</summary>
  public static double GetCompactBelow(PControl element)
    => element.GetValue(CompactBelowProperty);

  private static void OnCompactBelowChanged(PControl element, AvaloniaPropertyChangedEventArgs args)
  {
    bool armed = args.GetNewValue<double>() > 0;
    if (armed == element.GetValue(ArmedProperty))
    {
      if (armed)
        Update(element, element.Bounds.Width);
      return;
    }

    element.SetValue(ArmedProperty, armed);
    if (armed)
    {
      element.SizeChanged += OnSizeChanged;
      Update(element, element.Bounds.Width);
    }
    else
    {
      element.SizeChanged -= OnSizeChanged;
      element.Classes.Remove(CompactClass);
    }
  }

  private static void OnSizeChanged(object? sender, SizeChangedEventArgs args)
  {
    if (sender is PControl element)
      Update(element, args.NewSize.Width);
  }

  private static void Update(PControl element, double width)
  {
    double threshold = element.GetValue(CompactBelowProperty);
    element.Classes.Set(CompactClass, IsCompactSlot(threshold, width));
  }
}
