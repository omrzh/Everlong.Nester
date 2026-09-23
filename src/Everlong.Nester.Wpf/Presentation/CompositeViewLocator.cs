using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Internal composite that chains the application's <see cref="IViewLocator" />
///   instances — the declared mapping table, consulted before the tree's plain
///   templates.
/// </summary>
/// <remarks>
///   <para>
///     The chain is discovered by scanning
///     <see cref="System.Windows.Application.Current" /> resources, merged
///     dictionaries included.  A merged dictionary is searched last entry
///     first, and the chain walks the same way, so the last registration wins.
///   </para>
///   <para>
///     This class is constructed automatically by the tree's view resolution
///     from <see cref="System.Windows.Application.Current" /> resources —
///     users do not interact with it directly.
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
    _locators = [.. ScanLocators(dataTemplateSource)];
  }

  /// <inheritdoc />
  public bool Match(object? data)
  {
    if (data is null)
      return false;

    for (var i = _locators.Count - 1; i >= 0; i--)
    {
      if (_locators[i].Match(data))
        return true;
    }

    return false;
  }

  /// <inheritdoc />
  public PControl? Build(object? data)
  {
    if (data is null)
      return null;

    // Last entry first — the same order a merged dictionary resolves in.
    for (var i = _locators.Count - 1; i >= 0; i--)
    {
      if (!_locators[i].Match(data))
        continue;
      if (_locators[i].Build(data) is { } view)
        return view;
    }

    return null;
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
