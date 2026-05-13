using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Routing;

/// <summary>Pins the run-context construction seam — the router subclass supplies the convergence context flavor its landings run with.</summary>
public class RunContextSeamTests
{
  [Fact]
  public async Task RouterSubclassSuppliesItsConvergenceContextFlavor()
  {
    IConvergenceScene? revealed = null;
    var shell = new FakeShell();
    var router = new SeamedRouter(shell, new TestNavigationView(reveal: ctx =>
    {
      revealed = ctx;
      return Task.CompletedTask;
    }));

    await router.RouteAsync(new Request(typeof(PageAlpha), null));

    // The subclass flavor reaches the view seam — the same instance the
    // settle created through the router's factory.
    var context = Assert.IsType<RecordingConvergenceContext>(revealed);
    Assert.Equal(RoutingDirection.Route, context.Direction);
  }

  /// <summary>A router whose landings run the recording convergence context flavor.</summary>
  private sealed class SeamedRouter : RouterBase
  {
    internal SeamedRouter(FakeShell shell, TestNavigationView view)
      : base(shell.Services, new TestStackModel(view))
    {
    }

    /// <inheritdoc />
    protected internal override IConvergenceContext CreateConvergenceContext(TransactionContext context)
      => new RecordingConvergenceContext(context, Borrowed);
  }

  /// <summary>The seam's product — memberless; its type pins the factory path.</summary>
  private sealed class RecordingConvergenceContext(TransactionContext context, Location? borrowed)
    : ConvergenceContext(context, borrowed);
}
