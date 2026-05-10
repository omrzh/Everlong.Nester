using System.ComponentModel;

namespace Everlong.Nester.Routing;

/// <summary>
///   The default convergence run — a landed navigation's sites and signal.
///   The run's chains derive from the arrival and departure sites; the
///   moving sides and the borrowed site ride along.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public class ConvergenceContext : IConvergenceContext
{
  private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
  private readonly Location? _arrival;
  private readonly Location? _departure;
  private readonly IReadOnlyList<Location> _arrivings;
  private readonly IReadOnlyList<Location> _departings;

  private List<Location>? _arrivalChain;
  private List<Location>? _departureChain;

  /// <summary>Converts a committed transaction — the run adopts the decision's sites and the transaction's signal.</summary>
  protected internal ConvergenceContext(TransactionContext context, Location? borrowed = null)
  {
    _arrival = context.Arrival;
    _departure = context.DepartureSite;
    _arrivings = context.Arrivings;
    _departings = context.Departings;
    Arrival = _arrival;
    Departure = _departure;
    IsElevated = context.IsDerived;
    Borrowed = borrowed;
    Direction = context.Direction;
    Cts = context.Cts;
    Features = context.Features;
  }

  /// <inheritdoc />
  public ILocation? Arrival { get; }

  /// <inheritdoc />
  public ILocation? Departure { get; }

  /// <inheritdoc />
  public IReadOnlyList<Location> Chain => _arrival is null ? DepartureChain : ArrivalChain;

  /// <inheritdoc />
  public IReadOnlyList<ILocation> Arrivings => _arrivings;

  /// <inheritdoc />
  public IReadOnlyList<ILocation> Departings => _departings;

  /// <inheritdoc />
  public IReadOnlyList<Location> ArrivingNodes => _arrivings;

  /// <inheritdoc />
  public IReadOnlyList<Location> DepartingNodes => _departings;

  /// <summary>The presented site this overlay presentation borrowed, or <see langword="null" /> for the base router.</summary>
  public Location? Borrowed { get; }

  /// <inheritdoc />
  public RoutingDirection Direction { get; }

  /// <inheritdoc />
  public bool IsElevated { get; }

  /// <inheritdoc />
  public CancellationToken Lifetime => Cts.Token;

  /// <summary>The transfer's lifetime source — cancelled when its convergence ends, when a newer truth lands, or when its transaction dies uncommitted.</summary>
  internal CancellationTokenSource Cts { get; }

  /// <inheritdoc />
  public IFeatureCollection Features { get; }

  /// <inheritdoc />
  public Task Completion => _completion.Task;

  /// <inheritdoc />
  public void End()
  {
    _completion.TrySetResult();
    Cts.Cancel();
  }

  /// <summary>
  ///   The run's entering and leaving chains — the arrival and departure
  ///   lineages below their shared prefix, outermost first.
  /// </summary>
  /// <summary>The arrival lineage, outermost first, the site last; empty without a site.</summary>
  private List<Location> ArrivalChain => _arrivalChain ??= (_arrival?.Ancestors() ?? []);

  /// <summary>The departure lineage, outermost first, the site last; empty without a site.</summary>
  private List<Location> DepartureChain => _departureChain ??= (_departure?.Ancestors() ?? []);
}
