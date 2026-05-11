namespace Everlong.Nester.Routing;

partial class RouterStack
{
  // ── ④ the release pins and the result channel ───────────────────────────────

  /// <summary>Unpins a dropped entry's nodes — the ledger takes them; an instance with no holdings left is dead.</summary>
  protected void QueueRelease(IReadOnlyList<Location> dropped)
  {
    foreach (var node in dropped)
    {
      if (_pins.TryGetValue(node.Instance, out var count))
      {
        if (count > 1)
          _pins[node.Instance] = count - 1;
        else
          _pins.Remove(node.Instance);
      }
      _pendingRelease.Add(node);
    }
  }

  /// <summary>Pins the landed entry's nodes — the incremental live set: every (entry, node) holding counts once.</summary>
  internal void PinChain(Location[] chain)
  {
    foreach (var node in chain)
      _pins[node.Instance] = _pins.TryGetValue(node.Instance, out var count) ? count + 1 : 1;
  }

  /// <summary>Settles the layer-lifetime result — completes <see cref="Result" />.</summary>
  internal void Settle(object? result) => _tcs.TrySetResult(result);

  /// <summary>Faults the layer-lifetime result — completes <see cref="Result" /> with the exception.</summary>
  internal void SettleFault(Exception exception) => _tcs.TrySetException(exception);
}
