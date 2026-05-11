namespace Everlong.Nester.Routing;

partial class RouterStack
{
  // ── the history stack — a list of entries with a cursor ──────────────────

  private readonly List<Location[]> _items = [];

  /// <summary>
  ///   The cursor's index into <see cref="_items" /> — <c>-1</c> when the
  ///   stack is empty.  Invariant: <c>-1</c> when empty, else within
  ///   <c>[0, Count)</c>; every entry mutation adjusts it in the same
  ///   operation.
  /// </summary>
  private int _index = -1;

  /// <summary>The entry immediately behind the current position, or <see langword="null"/> when none — a pure read.</summary>
  internal Location[]? PreviousEntry => _index > 0 ? _items[_index - 1] : null;

  /// <summary>The entry immediately ahead of the current position, or <see langword="null"/> when none — a pure read.</summary>
  internal Location[]? NextEntry => _index >= 0 && _index + 1 < _items.Count ? _items[_index + 1] : null;

  /// <summary>
  ///   Appends an entry as the new current, clearing the forward trail and
  ///   evicting the oldest entries beyond <see cref="MaxDepth" />.  Returns
  ///   the dropped entries — the cleared forward trail, then the evicted
  ///   prefix — for the commit's release accounting.
  /// </summary>
  private IReadOnlyList<Location[]> Push(Location[] item)
  {
    // The push rewrites the future — everything ahead of the cursor goes.
    int keep = _index + 1;
    List<Location[]>? dropped = null;
    if (keep < _items.Count)
    {
      dropped = new List<Location[]>(_items.Count - keep);
      for (int i = keep; i < _items.Count; i++)
        dropped.Add(_items[i]);
      _items.RemoveRange(keep, _items.Count - keep);
    }

    _items.Add(item);
    _index = _items.Count - 1;

    // The ceiling converges here — the cursor just landed on the end, so
    // the evicted prefix always sits below it.
    if (MaxDepth is { } cap && _items.Count > cap)
    {
      int overflow = _items.Count - cap;
      dropped ??= new List<Location[]>(overflow);
      for (int i = 0; i < overflow; i++)
        dropped.Add(_items[i]);
      _items.RemoveRange(0, overflow);
      _index -= overflow;
    }
    return dropped ?? (IReadOnlyList<Location[]>)Array.Empty<Location[]>();
  }

  /// <summary>The live entry whose chain's content terminal is the given site, or <see langword="null" /> when none presents it.</summary>
  internal Location[]? FindEntryByTerminal(ILocation site)
  {
    foreach (Location[] entry in _items)
      if (entry is { Length: > 0 } && ReferenceEquals(entry[^1], site))
        return entry;
    return null;
  }

  /// <summary>Moves to the previous entry.  Returns whether it moved.</summary>
  internal bool MoveBack()
  {
    if (_index <= 0)
      return false;
    _index--;
    return true;
  }

  /// <summary>Moves to the next entry.  Returns whether it moved.</summary>
  internal bool MoveForward()
  {
    if (_index < 0 || _index + 1 >= _items.Count)
      return false;
    _index++;
    return true;
  }

  /// <summary>All entries, oldest first.</summary>
  internal IReadOnlyList<Location[]> Entries => _items;

  /// <summary>
  ///   Removes the entry immediately behind the current position, if any.
  ///   Idempotent: when no entry is behind the current position, the call
  ///   is a no-op.
  /// </summary>
  /// <returns>The removed entry, or <see langword="null"/> when none.</returns>
  private Location[]? RemoveBackwardEntry()
  {
    if (_index <= 0)
      return null;
    Location[] value = _items[_index - 1];
    _items.RemoveAt(_index - 1);
    _index--;
    return value;
  }

  /// <summary>
  ///   Removes the entry immediately ahead of the current position, if any.
  ///   Idempotent: when no entry is ahead of the current position, the call
  ///   is a no-op.
  /// </summary>
  /// <returns>The removed entry, or <see langword="null"/> when none.</returns>
  private Location[]? RemoveForwardEntry()
  {
    int next = _index + 1;
    if (_index < 0 || next >= _items.Count)
      return null;
    Location[] value = _items[next];
    _items.RemoveAt(next);
    return value;
  }
}
