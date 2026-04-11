namespace Everlong.Nester.Intent;

/// <summary>
///   Pass-through handler — forwards every consultation to the next handler.
/// </summary>
public sealed class NoOpIntentHandler : IIntentHandler
{
  /// <summary>The singleton pass-through.</summary>
  public static readonly NoOpIntentHandler Instance = new();

  /// <inheritdoc />
  public ValueTask HandleAsync(IntentContext context, IntentDelegate next) => next(context);

  /// <summary> A no-op terminal.</summary>
  public static IntentDelegate NoOpTerminal { get; } = static _ => ValueTask.CompletedTask;
}
