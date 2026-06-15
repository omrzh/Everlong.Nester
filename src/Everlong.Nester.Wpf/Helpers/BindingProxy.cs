using System.Windows;

namespace Everlong.Nester.Helpers;

/// <summary>
/// A helper class that enables data binding across elements that do not share the same visual or logical tree.
/// </summary>
public class BindingProxy : Freezable
{
  /// <summary>
  /// Creates a new instance of the <see cref="BindingProxy"/> class.
  /// </summary>
  /// <returns>A new <see cref="BindingProxy"/> instance.</returns>
  protected override Freezable CreateInstanceCore() => new BindingProxy();

  /// <summary>
  /// Gets or sets the data object to be used as the binding source.
  /// </summary>
  public object Data
  {
    get => GetValue(DataProperty);
    set => SetValue(DataProperty, value);
  }

  /// <summary>
  /// Identifies the <see cref="Data"/> dependency property.
  /// </summary>
  public static readonly DependencyProperty DataProperty =
    DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
}
