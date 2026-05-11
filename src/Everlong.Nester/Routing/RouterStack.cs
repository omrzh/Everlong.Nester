using Everlong.Nester.Presentation;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Routing;

/// <summary>
///   The router's stack model — the stacked layer of a base or a
///   derived router: its history stack, the participant tree it holds, the
///   root view that tree realizes into, and the transactions it computes.
///   A route pushes, back/forward traverse, and a back at the overlay's
///   foot closes a derived router.
/// </summary>
public abstract partial class RouterStack : IRouterStack
{
  private readonly TaskCompletionSource<object?> _tcs =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  /// <summary>
  ///   The container tree — the forest of participant nodes under a virtual
  ///   root, keyed by participant type.  A node's identity is
  ///   (type, args, instance); stack entries are paths through this tree.
  ///   Resolution consults the tree before the resolver: a hit reuses the
  ///   held node (state and view continue), a miss materializes and
  ///   attaches.  Held to the live set — dead nodes detach at the drain.
  /// </summary>
  private readonly Dictionary<Type, List<Location>> _tree = [];

  /// <summary>The tree's root — the forest's virtual root, keyed by participant type.</summary>
  internal Dictionary<Type, List<Location>> Tree => _tree;

  /// <summary>
  ///   Finds a held child of <paramref name="parent" /> that serves
  ///   <paramref name="spec" />, or <see langword="null" /> when none does.
  /// </summary>
  /// <remarks>
  ///   Only <paramref name="parent" />'s direct children are consulted;
  ///   <paramref name="parent" /> is <see langword="null" /> at the tree's
  ///   root.  A node's identity is <c>(type, args, instance)</c>: an
  ///   explicit instance matches by reference, a parameterized child by
  ///   equal arguments, and a rigid child serves any request.
  /// </remarks>
  internal Location? Match(ITarget spec, Location? parent)
    => Lookup(parent?.Children ?? _tree, spec);

  /// <summary>Attaches a node under <paramref name="parent" /> — the tree's root level when <paramref name="parent" /> is <see langword="null" />.</summary>
  internal void Attach(Location? parent, Location node)
  {
    node.Parent = parent;
    var children = parent?.Children ?? _tree;
    if (!children.TryGetValue(node.Type, out var list))
    {
      list = [];
      children[node.Type] = list;
    }

    list.Add(node);
  }

  /// <summary>Detaches a node from its holding level; empty levels collapse.</summary>
  internal void Detach(Location node)
  {
    var level = node.Parent is { } parent ? parent.Children : _tree;
    if (level.TryGetValue(node.Type, out var siblings))
    {
      siblings.Remove(node);
      if (siblings.Count == 0)
        level.Remove(node.Type);
    }
  }

  /// <summary>The single-level reuse lookup over a held children bucket.</summary>
  private static Location? Lookup(Dictionary<Type, List<Location>> children, ITarget spec)
  {
    if (!children.TryGetValue(spec.Type, out var list))
      return null;

    foreach (var child in list)
    {
      if (spec.Instance is not null)
      {
        if (ReferenceEquals(child.Instance, spec.Instance))
          return child;
      }
      else if (child.Instance is IParameterized && Equals(child.Args, spec.Args))
        return child;
    }

    if (spec.Instance is not null)
      return null;

    foreach (var child in list)
      if (child.Instance is not IParameterized)
        return child;
    return null;
  }

  /// <summary>The release ledger — dropped entries' nodes the drains settle; an instance still held elsewhere defers its nodes.</summary>
  private readonly HashSet<Location> _pendingRelease = [];

  /// <summary>The release ledger — dropped entries' nodes, drained by the convergence runs.  The drains own the settlement; the model only records.</summary>
  internal HashSet<Location> ReleaseLedger => _pendingRelease;

  /// <summary>Instance retention — the live (entry, node) holdings per instance, kept at every entry add and drop.  Keyed by reference: a participant that overrides <c>Equals</c>/<c>GetHashCode</c> must still count as its own instance.</summary>
  private readonly Dictionary<object, int> _pins = new(ReferenceEqualityComparer.Instance);

  /// <summary>Instance retention — the incremental live set the drains consult; an instance with no holdings is dead.</summary>
  internal Dictionary<object, int> InstancePins => _pins;

  /// <summary>Sync-transaction window depth — while a transaction computes, commits and publishes, the stack is being rewritten and trims are declined.</summary>
  private int _transactionGuard;

  /// <summary>Convergence runs in flight — counted per run, so overlapping runs keep the scope open until the last one settles.</summary>
  private int _convergenceRuns;

  /// <summary>Whether a convergence run is in flight — trimmed orphans wait for the last run's end-of-run drain.</summary>
  internal bool IsConvergenceRunning => _convergenceRuns > 0;



  private int? _maxDepth;

  /// <inheritdoc />
  public int? MaxDepth
  {
    get => _maxDepth;
    set => _maxDepth = value is null or >= 1
      ? value
      : throw new ArgumentOutOfRangeException(nameof(value), "The depth ceiling is at least one entry, or null for no ceiling.");
  }

  // ── the platform seam — the concrete node type and the root view the
  //    tree realizes into ───────────────────────────────────────────────────

  /// <summary>The root view this model's tree realizes into.</summary>
  protected internal abstract IRoutingView View { get; }

  /// <summary>Materializes a resolved participant into a location of the model's concrete node type.</summary>
  protected internal abstract Location CreateLocation(Type type, object instance, IArgs? args);

  /// <inheritdoc />
  public ILocation? PeekPrevious()
    => PreviousEntry is { Length: > 0 } previous ? previous[^1] : null;

  /// <inheritdoc />
  public ILocation? PeekNext()
    => NextEntry is { Length: > 0 } next ? next[^1] : null;

  /// <inheritdoc />
  public IReadOnlyList<ILocation> BackStack()
  {
    if (_index <= 0)
      return [];

    var result = new ILocation[_index];
    for (int i = _index - 1, j = 0; i >= 0; i--, j++)
      result[j] = _items[i][^1];
    return result;
  }

  /// <inheritdoc />
  public IReadOnlyList<ILocation> ForwardStack()
  {
    int count = _items.Count - _index - 1;
    if (count <= 0)
      return [];

    var result = new ILocation[count];
    for (int i = _index + 1, j = 0; i < _items.Count; i++, j++)
      result[j] = _items[i][^1];
    return result;
  }

  /// <inheritdoc />
  public IReadOnlyList<ILocation> Snapshot()
  {
    var result = new ILocation[_items.Count];
    for (int i = 0; i < _items.Count; i++)
      result[i] = _items[i][^1];
    return result;
  }

  internal Location[] CurrentChain => _index >= 0 ? _items[_index] : [];

  internal object? Current => CurrentChain is { Length: > 0 } chain ? chain[^1].Instance : null;

  /// <summary>The layer-lifetime signal — completes when this model's layer ends.</summary>
  internal Task<object?> Result => _tcs.Task;

  /// <summary>Whether the layer has ended.</summary>
  internal bool IsClosed => _tcs.Task.IsCompleted;

  /// <summary>Adopts the resolved chain — the commit's model-side mutation: the dropped entries unpin, the landed entry pins.</summary>
  internal void Commit(Location[] resolved)
  {
    foreach (var dropped in Push(resolved))
      QueueRelease(dropped);
    PinChain(resolved);
  }

  // ── stack operation feature — the -ed window capability ─────────────────

  /// <summary>Opens the sync transaction window — trims are declined while it is open.</summary>
  internal void BeginTransactionWindow() => _transactionGuard++;

  /// <summary>Closes the sync transaction window.</summary>
  internal void EndTransactionWindow() => _transactionGuard--;

  /// <summary>Opens a run's scope — the run counts in flight until it settles.</summary>
  internal void BeginConvergenceRun() => _convergenceRuns++;

  /// <summary>Closes a run's scope — overlapping runs keep the scope open until the last one settles.</summary>
  internal void EndConvergenceRun() => _convergenceRuns--;

  /// <inheritdoc />
  public bool TrimBackward() => TrimAdjacent(backward: true);

  /// <inheritdoc />
  public bool TrimForward() => TrimAdjacent(backward: false);

  /// <summary>
  ///   Removes the adjacent entry — a history-only mutation; the current
  ///   chain is untouched, so no convergence runs.  A bound main-thread
  ///   dispatcher is required — without one the trim runs on the caller's
  ///   thread.  Declined (<see langword="false" />) while a sync
  ///   transaction is in flight or the layer is closing.  A trimmed orphan is
  ///   released by the drain — at the in-flight run's end, or immediately
  ///   when idle.
  /// </summary>
  private bool TrimAdjacent(bool backward)
  {
    if (MainDispatcher.TryGet(out var dispatcher))
      dispatcher.VerifyAccess();

    if (_transactionGuard != 0 || IsClosed)
      return false;

    Location[]? removed = backward ? RemoveBackwardEntry() : RemoveForwardEntry();
    if (removed is not { Length: > 0 })
      return false;

    QueueRelease(removed);
    TrailTrimmed?.Invoke();
    RaiseNavigated();
    return true;
  }

  /// <summary>Notifies that a trim fed the release ledger — the router drains now outside a run, or the run's endpoint holds it.</summary>
  internal event Action? TrailTrimmed;
}
