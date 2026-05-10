namespace Everlong.Nester.Routing;

/// <summary>
///   The default <see cref="IFeatureCollection"/> — a dictionary keyed by
///   interface type.
/// </summary>
internal sealed class FeatureCollection : IFeatureCollection
{
  private Dictionary<Type, object>? _features;

  /// <inheritdoc />
  public T? Get<T>() where T : class
    => _features is { } features && features.TryGetValue(typeof(T), out object? feature) ? (T)feature : null;

  internal void Set<T>(T? feature) where T : class
  {
    if (feature is null)
    {
      _features?.Remove(typeof(T));
      return;
    }

    (_features ??= [])[typeof(T)] = feature;
  }

  /// <inheritdoc />
  public IEnumerator<object> GetEnumerator()
    => (_features?.Values ?? Enumerable.Empty<object>()).GetEnumerator();

  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
