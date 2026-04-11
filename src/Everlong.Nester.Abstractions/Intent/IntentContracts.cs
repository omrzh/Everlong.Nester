namespace Everlong.Nester.Intent;

/// <summary>
///   Represents an intent to perform an action — a polymorphic marker with no members.
/// </summary>
public interface IIntent : IImpulse;

/// <summary>The outcome of an <see cref="IIntent" /> consultation.</summary>
public enum IntentResult
{
  /// <summary>The handler did not engage — the consultation continues.</summary>
  Pass,

  /// <summary>The handler performed the intent's business.</summary>
  Handled,

  /// <summary>The handler blocked the intent — it must not execute.</summary>
  Vetoed,
}

/// <summary>The shared consultation state of one <see cref="IIntent" /> dispatch.</summary>
public sealed class IntentContext(IIntent intent, object? sender)
{
  /// <summary>The intent under consultation.</summary>
  public IIntent Intent { get; } = intent;

  /// <summary>The originator of the intent, if any.</summary>
  public object? Sender { get; } = sender;

  /// <summary>The settled outcome of the consultation — write via <see cref="Veto" /> or <see cref="Handle" />.</summary>
  public IntentResult Result { get; private set; } = IntentResult.Pass;

  /// <summary>The handler that settled the outcome, if any.</summary>
  public object? HandledBy { get; private set; }

  /// <summary>Whether the consultation ended — the outcome is <see cref="IntentResult.Handled" /> or <see cref="IntentResult.Vetoed" />.</summary>
  public bool IsTerminated => Result is IntentResult.Handled or IntentResult.Vetoed;

  /// <summary>The cross-handler channel of the dispatch.</summary>
  public Dictionary<object, object?> Items => field ??= [];

  /// <summary>Vetoes the intent — it must not execute.</summary>
  /// <param name="by">The handler that vetoed, if any.</param>
  /// <exception cref="InvalidOperationException">The consultation already settled.</exception>
  public void Veto(object? by = null)
  {
    ThrowIfSettled();
    Result = IntentResult.Vetoed;
    if (by is not null)
      HandledBy = by;
  }

  /// <summary>Marks the intent as executed.</summary>
  /// <param name="by">The handler that executed it, if any.</param>
  /// <exception cref="InvalidOperationException">The consultation already settled.</exception>
  public void Handle(object? by = null)
  {
    ThrowIfSettled();
    Result = IntentResult.Handled;
    if (by is not null)
      HandledBy = by;
  }

  private void ThrowIfSettled()
  {
    if (!IsTerminated)
      return;
    throw new InvalidOperationException(
      $"The consultation already settled as {Result}" +
      (HandledBy is { } by ? $" by {by.GetType().Name}" : "") +
      $"; {Intent.GetType().Name} cannot be settled again.");
  }
}

/// <summary>The next handler in an <see cref="IIntent" /> pipeline.</summary>
public delegate ValueTask IntentDelegate(IntentContext context);

/// <summary>
///   A contract for handling an <see cref="IIntent" />.
/// </summary>
public interface IIntentHandler
{
  /// <summary>
  ///   Handles an <see cref="IIntent" /> — either settles
  ///   <see cref="IntentContext.Result" /> or invokes <paramref name="next" />.
  /// </summary>
  /// <remarks>
  ///   Settling does not prevent a subsequent <paramref name="next" /> call —
  ///   downstream handlers receive the settled outcome and must not settle
  ///   again; a second settlement throws
  ///   <see cref="InvalidOperationException" />.
  /// </remarks>
  /// <param name="context">The consultation state.</param>
  /// <param name="next">The next handler; a no-op at the end of the pipeline.</param>
  ValueTask HandleAsync(IntentContext context, IntentDelegate next);
}

/// <summary>
///   A contract for dispatching an <see cref="IIntent" /> for handling.
/// </summary>
public interface IIntentDispatcher
{
  /// <summary>
  ///   Dispatches an intent through the handler chain.
  /// </summary>
  /// <param name="sender">The originator of the intent, if any.</param>
  /// <param name="intent">The intent to dispatch.</param>
  /// <returns>
  ///   The consultation outcome — <see cref="IntentResult.Pass" /> when no
  ///   handler engaged, <see cref="IntentResult.Handled" /> when a handler
  ///   performed the intent, <see cref="IntentResult.Vetoed" /> when a
  ///   handler blocked it.
  /// </returns>
  ValueTask<IntentResult> DispatchIntent(object? sender, IIntent intent);
}
