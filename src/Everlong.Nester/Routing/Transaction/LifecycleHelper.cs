namespace Everlong.Nester.Routing;

/// <summary>
///   The lifecycle hook runners — the dual dispatch (instance first, then
///   the node's view) of a node's convergence hooks, each hook isolated.
///   Shared by the convergence phases and the model's close convergences.
/// </summary>
internal static class LifecycleHelper
{
  /// <summary>Runs the pre-departure hook on the node's instance and view — independently isolated.</summary>
  internal static void RunDeparting(Location node, IRoutingContext context, List<Exception>? failures)
  {
    if (node.Instance is IDeparting vmDeparting)
      RunSafely(context, failures, () => vmDeparting.OnDeparting(context));
    if (node.Presenter is IDeparting viewDeparting)
      RunSafely(context, failures, () => viewDeparting.OnDeparting(context));
  }

  /// <summary>Runs the post-departure hook on the node's instance and view — independently isolated.</summary>
  internal static void RunDeparted(Location node, IRoutingContext context, List<Exception>? failures)
  {
    if (node.Instance is IDeparted vmDeparted)
      RunSafely(context, failures, () => vmDeparted.OnDeparted(context));
    if (node.Presenter is IDeparted viewDeparted)
      RunSafely(context, failures, () => viewDeparted.OnDeparted(context));
  }

  /// <summary>Runs the pre-arrival hook on the node's instance and view — independently isolated.</summary>
  internal static async Task RunArrivingAsync(Location node, IRoutingContext context, List<Exception>? failures)
  {
    if (node.Instance is IArriving vmArriving)
      await RunHookAsync(context, failures, () => vmArriving.OnArrivingAsync(context));
    if (node.Presenter is IArriving viewArriving)
      await RunHookAsync(context, failures, () => viewArriving.OnArrivingAsync(context));
  }

  /// <summary>Runs the post-arrival hook on the node's instance and view — independently isolated.</summary>
  internal static async Task RunArrivedAsync(Location node, IRoutingContext context, List<Exception>? failures)
  {
    if (node.Instance is IArrived vmArrived)
      await RunHookAsync(context, failures, () => vmArrived.OnArrivedAsync(context));
    if (node.Presenter is IArrived viewArrived)
      await RunHookAsync(context, failures, () => viewArrived.OnArrivedAsync(context));
  }

  /// <summary>
  ///   Runs a hook — a cancellation of this transfer's own lifetime token
  ///   is silent; any other failure, a foreign cancellation included, is
  ///   collected for the error channel.
  /// </summary>
  private static async Task RunHookAsync(IRoutingContext context, List<Exception>? failures, Func<Task> hook)
  {
    try
    {
      await hook();
    }
    catch (OperationCanceledException) when (context.Lifetime.IsCancellationRequested)
    {
      // superseded mid-hook — benign
    }
    catch (Exception e)
    {
      failures?.Add(e);
    }
  }

  /// <summary>
  ///   Runs a synchronous hook — a cancellation of this transfer's own
  ///   lifetime token is silent; any other failure, a foreign
  ///   cancellation included, is collected for the error channel.
  /// </summary>
  private static void RunSafely(IRoutingContext context, List<Exception>? failures, Action action)
  {
    try
    {
      action();
    }
    catch (OperationCanceledException) when (context.Lifetime.IsCancellationRequested)
    {
      // superseded mid-hook — benign
    }
    catch (Exception e)
    {
      failures?.Add(e);
    }
  }
}
