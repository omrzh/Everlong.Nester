using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Internal composite that chains <see cref="IViewLocator"/> instances for the imperative
///   <c>Build()</c> path.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="Build"/> tries each locator's own resource dictionary first (fast path),
///     then falls back to <see cref="System.Windows.Application.TryFindResource"/> to catch
///     any plain XAML <see cref="DataTemplate"/> entries not covered by a registered locator.
///   </para>
///   <para>
///     This class is constructed automatically by <c>AddNesterWpf()</c> by scanning
///     <see cref="System.Windows.Application.Current"/> resources — users do not interact with it directly.
///   </para>
/// </remarks>
internal sealed class CompositeViewLocator : IViewLocator
{
  private readonly IReadOnlyList<IViewLocator> _locators;

  /// <param name="locators">The internal locators to use</param>
  public CompositeViewLocator(IEnumerable<IViewLocator> locators)
    => _locators = [.. locators];

  public CompositeViewLocator(ResourceDictionary? dataTemplateSource = null)
  {
    dataTemplateSource ??= PApp.Current.Resources;
    var locators = ScanLocators(dataTemplateSource).ToList();
    _locators = locators;
  }


  /// <inheritdoc/>
  public PControl? Build(object? data)
  {
    if (data is null)
      return null;

    // Fast path: each locator's self-dictionary lookup
    for (var i = _locators.Count - 1; i >= 0; i--)
    {
      var result = _locators[i].Build(data);
      if (result is not null)
        return result;
    }

    // Fallback: WPF resource system (catches plain XAML DataTemplates)
    var key = new DataTemplateKey(data.GetType());
    return PApp.Current.TryFindResource(key) is DataTemplate t
             ? t.LoadContent() as PControl
             : null;
  }

  private static IEnumerable<IViewLocator> ScanLocators(ResourceDictionary dict)
  {
    if (dict is IViewLocator l)
      yield return l;
    foreach (var merged in dict.MergedDictionaries)
      foreach (var inner in ScanLocators(merged))
        yield return inner;
  }
}
