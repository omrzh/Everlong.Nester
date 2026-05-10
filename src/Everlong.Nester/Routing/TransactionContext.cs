using System.ComponentModel;

namespace Everlong.Nester.Routing;

/// <summary>
///   The pipeline context of one navigation transaction — the sync transaction
///   pipeline's shared state.  Created at the request's intake (unresolved,
///   carrying the route request), filled with the decision by the transaction
///   phases, and frozen at the commit.
/// </summary>
/// <remarks>
///   Two-phase lifecycle: the sync pipeline's user hook may veto the transaction,
///   and its request may be replaced while the request window is open — the
///   window closes when the request is expanded into its plan; once committed,
///   the decision is immutable.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public class TransactionContext
{
  internal TransactionContext(RoutingDirection direction, ILocator? location = null, ILocation? site = null)
  {
    Direction = direction;
    Locator = location;
    Site = site;
    Context = new ReadOnlyRoutingContext(this);
  }

  /// <summary>The operation this transaction records.</summary>
  public RoutingDirection Direction { get; }

  // ── The relay — the landing and the commit ─────────────────────────────────────

  /// <summary>The transaction's landing — completes <see langword="true" /> when the transaction lands (its commit and notifications run) or a jump arrives at the current site, <see langword="false" /> when it settles without landing (consumed / vetoed / a jump's site has no live entry); faults on a pre-commit exception.</summary>
  internal TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

  /// <summary>Whether the transaction has passed its commit — the decision is frozen.  A faulted transaction has not.</summary>
  public bool IsCommitted => Result.Task is { IsCompletedSuccessfully: true, Result: true };

  /// <summary>The run a landed transaction carries — its convergence's observation surface; <see langword="null" /> until the transaction lands.</summary>
  internal IConvergenceContext? Convergence { get; set; }

  // ── The decision — the resolved chain and its sites ────────────────────────────

  /// <summary>The resolved chain — participants outermost first, the content target last.  Empty before the commit.</summary>
  internal Location[] Resolved { get; set; } = [];

  /// <summary>The site this transaction lands on — the resolved chain's content terminal; null when the transaction closes the presented layer.</summary>
  internal Location? Arrival => Direction == RoutingDirection.Close ? null : TailOf(Resolved);

  /// <summary>The presented terminal when this transaction's decision ran — the site its transition departs from; null when nothing was presented.</summary>
  internal Location? DepartureSite { get; private set; }

  /// <summary>The arriving side of this transaction's decision — the nodes from its first difference down, which replay their arrival.</summary>
  internal IReadOnlyList<Location> Arrivings { get; private set; } = [];

  /// <summary>The departing side of this transaction's decision — the old nodes from that same position down; a node on both sides is re-engaged.</summary>
  internal IReadOnlyList<Location> Departings { get; private set; } = [];

  /// <summary>The nodes this transaction materialized — released when it settles without committing.</summary>
  internal List<Location> Materialized => _materialized ??= [];

  /// <summary>Whether the transaction materialized any node — the close-out's guard.</summary>
  internal bool HasMaterialized => _materialized is { Count: > 0 };

  private List<Location>? _materialized;

  // ── The environment — scoped resources every direction shares ──────────────────

  private CancellationTokenSource? _cts;

  /// <summary>The lifetime source this transaction's observation surfaces expose, created on first read.</summary>
  internal CancellationTokenSource Cts => _cts ??= new();

  /// <summary>Cancels the lifetime token this transaction handed out — a token nobody read was never created.</summary>
  internal void CancelAbort()
  {
    _cts?.Cancel();
  }

  private FeatureCollection? _features;

  internal FeatureCollection Features => _features ??= [];

  internal IRoutingContext Context { get; }

  /// <summary>Whether the transaction's router is a derived router.</summary>
  internal bool IsDerived { get; set; }

  #region Route

  /// <summary>
  ///   The navigable input this transaction routes — the request the
  ///   expansion resolves; its description becomes the resolved spec when the
  ///   request window closes.
  /// </summary>
  public ILocator? Locator { get; private set; }

  /// <summary>Replaces the request this transaction routes.</summary>
  /// <remarks>
  ///   Valid only while the request window is open: the window closes when the
  ///   request is expanded into its plan, and a committed transaction is
  ///   frozen outright.  The replacement is the request that resolves and
  ///   lands.
  /// </remarks>
  /// <exception cref="InvalidOperationException">The request window is closed.</exception>
  public void UpdateLocator(ILocator location)
  {
    if (IsCommitted)
      throw new InvalidOperationException("A committed transaction's location is frozen.");
    if (ExpandedLocator is not null)
      throw new InvalidOperationException(
        "The transaction's request window is closed — the request has been expanded.");
    Locator = location;
  }

  /// <summary>The resolved spec this transaction interprets — the frozen <see cref="ILocator.Path" /> of <see cref="Locator" />.</summary>
  internal IReadOnlyList<ITarget>? ExpandedLocator { get; set; }

  /// <summary>The participants this transaction delivers — the fresh parameterized nodes and the absorbed revisions, with the arguments they were requested with.</summary>
  internal List<(Location Node, IArgs? Requested)>? Settled { get; set; }

  /// <summary>The nodes this transaction created — the structural walk's fresh participants with their parents; the commit attaches them.</summary>
  internal List<(Location? Parent, Location Node)>? Created { get; set; }

  /// <summary>Whether the route absorbs in place — the revised nodes replay their arrival and the chain is not pushed.</summary>
  internal bool Absorbed { get; set; }

  #endregion

  #region Jump

  /// <summary>The realized site a jump request targets, or <see langword="null" /> for other directions.</summary>
  internal ILocation? Site { get; }

  #endregion

  // ── Decision helpers ──────────────────────────────────────────────────────────

  /// <summary>The chain's content terminal, or <see langword="null" /> for an empty chain.</summary>
  private static Location? TailOf(Location[] chain)
    => chain is { Length: > 0 } c ? c[^1] : null;

  /// <summary>Records the resolved chain, the departing chain's content terminal, and the two moving sides taken from <paramref name="arrivalFrom" /> and <paramref name="departureFrom" /> down.</summary>
  internal void Land(Location[] resolved, Location[] departure, int arrivalFrom, int departureFrom)
  {
    Resolved = resolved;
    DepartureSite = TailOf(departure);
    Arrivings = Slice(resolved, arrivalFrom);
    Departings = Slice(departure, departureFrom);
  }

  /// <summary>The chain from <paramref name="from" /> down — empty at or past the target.</summary>
  private static Location[] Slice(Location[] chain, int from)
    => from >= chain.Length ? [] : chain[from..];

  /// <summary>The shared prefix — how many leading nodes both chains hold by reference; the notify phase and the fills share this comparison.</summary>
  internal static int SharedPrefix(Location[]? a, Location[] b)
  {
    int shared = 0;
    int min = Math.Min(a?.Length ?? 0, b.Length);
    while (shared < min && ReferenceEquals(a![shared], b[shared]))
      shared++;
    return shared;
  }

  // ── Platform observation surface ─────────────────────────────────────────────────

  /// <summary>The resolved chain — participants outermost first, the content target last.</summary>
  public IReadOnlyList<Location> Chain => Resolved;

  /// <summary>The observation surface handed to lifecycle callbacks.</summary>
  public IRoutingContext RoutingContext => Context;

  /// <summary>The read-only projection handed to lifecycle callbacks.</summary>
  internal sealed class ReadOnlyRoutingContext(TransactionContext context) : IRoutingContext
  {
    /// <inheritdoc />
    public IReadOnlyList<ILocation> Arrivings => context.Arrivings;

    /// <inheritdoc />
    public IReadOnlyList<ILocation> Departings => context.Departings;

    /// <inheritdoc />
    public ILocation? Arrival => context.Arrival;

    /// <inheritdoc />
    public RoutingDirection Direction => context.Direction;

    /// <inheritdoc />
    public bool IsElevated => context.IsDerived;

    /// <inheritdoc />
    public CancellationToken Lifetime => context.Cts.Token;

    /// <inheritdoc />
    public IFeatureCollection Features => context.Features;

    /// <inheritdoc />
    public ILocation? Departure => context.DepartureSite;
  }
}
