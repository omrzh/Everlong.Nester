using System.Windows;

namespace Everlong.Nester.Presentation;

/// <summary>
///   The platform's view resolution — the view for a data instance, resolved
///   from the tree the asking control sits in.
/// </summary>
/// <remarks>
///   Two sources, in order: the declared mapping table (a locator can build a
///   view no template can express — a control configured in code), then the
///   plain <see cref="DataTemplate" />s the tree carries, whose lookup walks
///   the asking control's own resource scope before the application's.
/// </remarks>
internal static class ViewResolution
{
  /// <summary>The application whose resources the cached chain was built from.</summary>
  private static PApp? _appSnapshot;

  /// <summary>The cached chain over the application's locators.</summary>
  private static IViewLocator? _locator;

  /// <summary>Builds the view <paramref name="data" /> resolves to, or <see langword="null" /> when nothing claims it.</summary>
  internal static PControl? Build(PControl from, object? data)
  {
    if (data is null || data is UIElement)
      return null;

    PControl? declared = GetLocator()?.Build(data);
    return declared ?? FromTemplates(from, data);
  }

  /// <summary>
  ///   The application's declared mapping table — a chain over the
  ///   <see cref="IViewLocator" /> entries in the application's resources,
  ///   rebuilt when the application instance changes (a stale snapshot would
  ///   pin a dead resource collection).
  /// </summary>
  private static IViewLocator? GetLocator()
  {
    PApp? app = PApp.Current;
    if (app is null)
      return null;
    if (!ReferenceEquals(_appSnapshot, app))
    {
      _appSnapshot = app;
      _locator = new CompositeViewLocator(app.Resources);
    }

    return _locator;
  }

  /// <summary>
  ///   The plain template the tree carries — the concrete type first, then its
  ///   base types, stopping before <see cref="object" /> (the platform's own
  ///   implicit lookup skips it too).  Loads the template's content; the data
  ///   context is the caller's to wire.
  /// </summary>
  private static PControl? FromTemplates(PControl from, object data)
  {
    for (Type? type = data.GetType(); type is not null && type != typeof(object); type = type.BaseType)
    {
      if (from.TryFindResource(new DataTemplateKey(type)) is DataTemplate template)
        return template.LoadContent() as PControl;
    }

    return null;
  }
}
