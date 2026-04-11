namespace Everlong.Nester.Intent;

/// <summary>
///   Folds <see cref="IIntentHandler" /> lists into a single pipeline —
///   work before the next handler is the tunnel phase (outer to inner),
///   work after it the bubble phase (inner to outer).
/// </summary>
internal static class IntentPipelineHelper
{

  /// <summary>Folds the handlers into one pipeline — each handler decides whether to invoke the next.</summary>
  internal static IntentDelegate Build(IReadOnlyList<IIntentHandler> handlers)
  {
    return Fold(handlers.Select(h => (Func<IntentContext, IntentDelegate, ValueTask>)((ctx, next) => h.HandleAsync(ctx, next))).ToArray());
  }

  /// <summary>Folds handler-shaped stages into one pipeline — each stage decides whether to invoke the next.</summary>
  internal static IntentDelegate Fold(IReadOnlyList<Func<IntentContext, IntentDelegate, ValueTask>> stages)
  {
    IntentDelegate terminal = NoOpIntentHandler.NoOpTerminal;
    for (int i = stages.Count - 1; i >= 0; i--)
    {
      Func<IntentContext, IntentDelegate, ValueTask> stage = stages[i];
      IntentDelegate next = terminal;
      terminal = ctx => stage(ctx, next);
    }
    return terminal;
  }

}
