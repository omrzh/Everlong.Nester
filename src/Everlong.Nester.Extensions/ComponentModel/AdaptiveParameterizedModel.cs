using Everlong.Nester.Routing;

namespace Everlong.Nester.ComponentModel;

/// <summary>
/// A routable parameterized model that serves a revised request in place.
/// </summary>
public abstract class AdaptiveParameterizedModel<TArgs> : ParameterizedModel<TArgs>, IAdaptiveParameterized
  where TArgs : IArgs
{
  /// <inheritdoc />
  bool IAdaptiveParameterized.IsAdaptable(IArgs? requested) => requested is TArgs;
}
