using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
/// A WPF <see cref="PContentControl"/> that bypasses the declarative
/// <see cref="ResourceDictionary"/>/<see cref="DataTemplate"/> pipeline by delegating view
/// creation to <see cref="ViewLocatorBase.Build(object?)"/> imperatively on each <see cref="FrameworkElement.DataContext"/>
/// change.
/// </summary>
/// <typeparam name="TLocator">
/// The concrete <see cref="IViewLocator"/> implementation whose <c>Build</c> method
/// contains the imperative routing logic. A single static instance is shared across all controls
/// of the same generic instantiation.
/// </typeparam>
internal sealed class ImperativeContentControl<TLocator> : PContentControl
  where TLocator : IViewLocator, new()
{
  private static readonly TLocator Locator = new TLocator();

  protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    if (e.Property == DataContextProperty && e.NewValue is not null)
    {
      Content = Locator.Build(e.NewValue);
      return;
    }

    base.OnPropertyChanged(e);
  }
}

internal sealed class DynamicContentControl<TLocator> : PContentControl
  where TLocator : IViewLocator
{
  internal static TLocator? Locator { get; set; }

  protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
  {
    if (e.Property == DataContextProperty && e.NewValue is not null)
    {
      Content = Locator?.Build(e.NewValue);
      return;
    }

    base.OnPropertyChanged(e);
  }
}
