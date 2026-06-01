using Avalonia.Collections;
using Avalonia.Controls.Templates;

namespace Everlong.Nester.Presentation;

/// <summary>
///   Composes multiple Avalonia data-template locators into a single locator.
/// </summary>
/// <remarks>
///   Resolution is evaluated from first to last, so earlier locators have higher precedence —
///   the application's own order is priority order, the same reading the platform applies.
/// </remarks>
internal sealed class CompositeViewLocator : IViewLocator
{
  private readonly AvaloniaList<IDataTemplate> _locators;

  /// <summary>
  /// Directly holds the provided <see cref="DataTemplates"/> collection,
  /// allowing dynamic updates to the locator chain at runtime.
  /// </summary>
  /// <param name="templates">The data templates collection to use.</param>
  public CompositeViewLocator(DataTemplates templates)
  {
    _locators = templates;
  }

  /// <summary>
  ///   Determines whether any composed locator can handle the specified data.
  /// </summary>
  /// <param name="data">The candidate data context.</param>
  /// <returns><see langword="true" /> when at least one locator matches; otherwise <see langword="false" />.</returns>
  public bool Match(object? data)
  {
    for (var i = 0; i < _locators.Count; i++)
      if (_locators[i].Match(data))
        return true;

    return false;
  }

  /// <summary>
  ///   Builds a control for the specified data.
  /// </summary>
  /// <param name="param">The data context to resolve.</param>
  /// <returns>The resolved control, or <see langword="null" /> when no locator matches.</returns>
  public PlatformControl? Build(object? param)
  {
    for (var i = 0; i < _locators.Count; i++)
      if (_locators[i].Match(param))
        return _locators[i].Build(param);

    return null;
  }
}
