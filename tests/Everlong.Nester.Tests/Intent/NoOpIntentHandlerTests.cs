using Everlong.Nester.Intent;
using Xunit;

namespace Everlong.Nester.Tests.Intent;

/// <summary>
///   Pins the pass-through handler and its terminal: the handler forwards the
///   consultation to the next handler without settling it, and the terminal
///   ends the pipeline without touching it.
/// </summary>
public class NoOpIntentHandlerTests
{
  private sealed class TestIntent : IIntent { }

  private static IntentContext NewContext()
    => new(new TestIntent(), null);

  [Fact]
  public async Task Instance_HandleAsync_ForwardsTheSameContextToNext()
  {
    var context = NewContext();
    IntentContext? forwarded = null;

    await NoOpIntentHandler.Instance.HandleAsync(context, ctx =>
    {
      forwarded = ctx;
      return ValueTask.CompletedTask;
    });

    Assert.Same(context, forwarded);
  }

  [Fact]
  public async Task Instance_HandleAsync_LeavesTheConsultationUnsettled()
  {
    var context = NewContext();

    await NoOpIntentHandler.Instance.HandleAsync(context, _ => ValueTask.CompletedTask);

    Assert.Equal(IntentResult.Pass, context.Result);
    Assert.False(context.IsTerminated);
  }

  [Fact]
  public async Task NoOpTerminal_CompletesWithoutSettling()
  {
    var context = NewContext();

    await NoOpIntentHandler.NoOpTerminal(context);

    Assert.Equal(IntentResult.Pass, context.Result);
    Assert.False(context.IsTerminated);
  }
}
