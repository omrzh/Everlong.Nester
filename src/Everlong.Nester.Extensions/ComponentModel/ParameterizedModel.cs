using Everlong.Nester.Routing;

namespace Everlong.Nester.ComponentModel;

/// <summary>
/// A routable model that receives arguments and serves an engagement.
/// </summary>
public abstract class ParameterizedModel<TArgs> : RoutableModel, IParameterized where TArgs : IArgs
{
  /// <summary>
  ///  The args that this instance engaged, strong typed
  /// </summary>
  protected TArgs? EngagedArgs;

  /// <inheritdoc />
  IArgs? IParameterized.EngagedArgs => EngagedArgs;

  /// <inheritdoc />
  public void DeliverArgs(IArgs? args)
  {
    EngagedArgs = args is TArgs typed ? typed : default;
    OnArgsDelivered();
  }

  /// <summary>
  /// Custom logic once args delivered.
  /// </summary>
  protected abstract void OnArgsDelivered();
}
