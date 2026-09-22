using Everlong.Nester.ComponentModel;
using System.ComponentModel;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Dialog;

/// <summary>
///   The <c>ConfirmAsync</c> extension wiring over the router: session
///   construction (texts, countdown-independent button state) and the
///   show-result passthrough.  The countdown race itself is user wiring
///   over the show task — it is not unit-tested against a fake router (the
///   fake cannot model the close signal the race waits on).
/// </summary>
public class ConfirmAsyncTests
{
  /// <summary>A static navigation-state surface — the fake never navigates.</summary>
#pragma warning disable CS0067 // Event is required by the interface but never raised in this stub.
  private sealed class StubStack : IRouterStack
  {
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    ILocation? IRouterStack.Location => null;

    public bool CanGoBack => false;

    public bool CanGoForward => false;

    public int Count => 0;

    public int? MaxDepth { get; set; }

    public ILocation? PeekPrevious() => null;

    public ILocation? PeekNext() => null;

    public IReadOnlyList<ILocation> BackStack() => [];

    public IReadOnlyList<ILocation> ForwardStack() => [];

    public IReadOnlyList<ILocation> Snapshot() => [];

    public bool TrimBackward() => false;

    public bool TrimForward() => false;
  }

  private sealed class CapturingRouter : IRouter
  {
    public DialogSessionBase? ShownSession { get; private set; }

    public IRouterStack Stack { get; } = new StubStack();

    public RouterRole Role { get; }

    public Task RouteAsync(ILocator location) => Task.CompletedTask;

    public Task<bool> JumpAsync(ILocation site) => Task.FromResult(false);

    public IRouter Derive(bool isEphemeral = false) => new CapturingDerived(this);

    public IRouterCompletion? Completion => null;

    /// <summary>
    ///   The derived router Derive hands back — a derived router whose
    ///   channel is the completion surface (Role/Creation/protocol
    ///   contract), with routing captured on the owner.
    /// </summary>
    private sealed class CapturingDerived(CapturingRouter owner) : IRouter
    {
      private readonly TaskCompletionSource<object?> _settlement =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

      public IRouterCompletion? Completion => new CompletionChannel(_settlement);

      public IRouterStack Stack => owner.Stack;

      public RouterRole Role => RouterRole.Derived;

      public Task RouteAsync(ILocator location)
      {
        owner.ShownSession = location.Path[^1].Instance as DialogSessionBase;
        // The presentation settles with true the moment it is routed — the
        // show-result passthrough this fake models (a real channel starts
        // pending, so the derived-router freshness guard accepts it).
        _settlement.TrySetResult(true);
        return Task.CompletedTask;
      }

      public Task<bool> JumpAsync(ILocation site) => Task.FromResult(false);

      public IRouter Derive(bool isEphemeral = false) => throw new NotSupportedException();

      /// <summary>The channel of the captured derived router — pending at hand-off, settled with the show result when routed.</summary>
      private sealed class CompletionChannel(TaskCompletionSource<object?> settlement) : IRouterCompletion
      {
        public Task<object?> Result => settlement.Task;

        public void Complete(object? result) => settlement.TrySetResult(result);
      }
    }
  }

  [Fact]
  public async Task ConfirmAsync_WithoutCountdown_BuildsSession_AndReturnsShowResult()
  {
    var router = new CapturingRouter();

    Task<bool> task = router.ConfirmAsync("proceed?",
      new ConfirmOptions { ConfirmText = "Confirm", CancelText = "Cancel" }, new FakeTimeProvider());
    var session = Assert.IsType<ConfirmDialogSession>(router.ShownSession);

    // No countdown: the confirm button starts enabled and no text suffix is shown.
    Assert.True(session.IsConfirmEnabled);
    Assert.Equal("Confirm", session.ConfirmText);
    Assert.Equal("proceed?", session.Message);

    // The show result passes through.
    Assert.True(await task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
  }
}
