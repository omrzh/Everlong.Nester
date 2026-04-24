namespace Everlong.Nester.Routing;

/// <summary>
///   The request-scoped features, keyed by interface type.
/// </summary>
public interface IFeatureCollection : IEnumerable<object>
{
  /// <summary>
  ///   Gets the feature of type <typeparamref name="T"/>, or
  ///   <see langword="null" /> when none is registered.
  /// </summary>
  T? Get<T>() where T : class;
}
