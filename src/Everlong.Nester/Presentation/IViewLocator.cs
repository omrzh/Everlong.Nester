namespace Everlong.Nester.Presentation;

/// <summary>
///   Platform-agnostic view-resolution contract.
/// </summary>
/// <typeparam name="TView">The platform view type.</typeparam>
public interface IViewLocator<out TView> where TView : class
{
  /// <summary>
  ///   Builds and returns a view instance for the specified data.
  /// </summary>
  /// <param name="data">A view-model instance.</param>
  /// <returns>A new view instance, or <c>null</c> if no view is registered for the given data.</returns>
  TView? Build(object? data);
}
