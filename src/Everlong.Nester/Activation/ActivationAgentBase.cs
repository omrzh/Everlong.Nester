using Everlong.Nester.Intent;

namespace Everlong.Nester.Activation;

/// <summary>
///   Base activation agent: the conservative args conversion, the host
///   channel binding, the pending queue (intents arriving without a channel)
///   and the startup drain (<see cref="FlushAsync" />).
/// </summary>
public abstract class ActivationAgentBase : IActivationAgent
{
  private static readonly DefaultActivationArgsConverter DefaultArgsConverter = new();

  private readonly object _gate = new();
  private readonly List<IActivationIntent> _pending = [];
  private IReadOnlyList<IActivationIntent>? _converted;
  private bool _startupDrained;
  private IActivationChannel? _channel;

  /// <inheritdoc />
  public virtual IReadOnlyList<IActivationIntent> Convert(IReadOnlyList<string> args)
    => DefaultArgsConverter.Convert(args);

  /// <summary>The bound host channel; <see langword="null" /> when none is bound.</summary>
  protected IActivationChannel? Channel
  {
    get { lock (_gate) return _channel; }
  }

  /// <inheritdoc />
  public virtual void Bind(IActivationChannel channel)
  {
    ArgumentNullException.ThrowIfNull(channel);
    lock (_gate)
    {
      _channel = channel;
    }
  }

  /// <inheritdoc />
  public virtual ValueTask<bool> UnbindAsync(IActivationChannel channel)
  {
    ArgumentNullException.ThrowIfNull(channel);
    lock (_gate)
    {
      if (!ReferenceEquals(_channel, channel))
        return ValueTask.FromResult(false);
      _channel = null;
    }

    return ValueTask.FromResult(true);
  }

  /// <inheritdoc />
  public IReadOnlyList<IActivationIntent> TakePendingIntents()
  {
    lock (_gate)
    {
      var taken = _pending.ToList();
      _pending.Clear();
      return taken;
    }
  }

  /// <summary>Releases the resources this agent holds; the base holds none.</summary>
  public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;

  /// <summary>
  ///   Dispatches an activation intent through the bound channel, or parks
  ///   it when no channel is bound (<see cref="FlushAsync" /> drains it).
  /// </summary>
  protected ValueTask<IntentResult> ForwardAsync(IActivationIntent intent)
  {
    IActivationChannel? channel;
    lock (_gate)
    {
      channel = _channel;
    }

    if (channel is null)
    {
      lock (_gate)
      {
        _pending.Add(intent);
      }

      return ValueTask.FromResult(IntentResult.Pass);
    }

    return channel.DispatchAsync(intent);
  }

  /// <inheritdoc />
  public async ValueTask<bool> FlushAsync()
  {
    IActivationChannel? channel;
    IReadOnlyList<IActivationIntent> inputs;
    lock (_gate)
    {
      channel = _channel;
      if (!_startupDrained)
      {
        _converted = Convert(ActivationArgs.Current);
        _startupDrained = true;
      }

      inputs = [.. _converted ?? [], .. _pending];
      _pending.Clear();
    }

    if (channel is null)
    {
      // No channel bound — keep the input queued (a later FlushAsync drains it).
      lock (_gate)
      {
        _pending.AddRange(inputs);
      }

      return false;
    }

    bool anyTerminated = false;
    foreach (var intent in inputs)
    {
      var result = await channel.DispatchAsync(intent);
      anyTerminated |= result != IntentResult.Pass;
    }

    return anyTerminated;
  }
}
