using Everlong.Nester.ComponentModel;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Tests.Dialog;

/// <summary>
///   Locks the dialog session contract the routing redesign builds on:
///   a session writes its result to the presentation's completion surface
///   (the write-back channel the presentation hands over at arrival); the
///   surface settles once (the first close wins); each show uses a fresh
///   session instance, so results never leak across shows; being shown
///   (arrival) and producing a result (close) are decoupled — a session
///   without a captured surface drops its write.
/// </summary>
public class SessionContractTests
{
  /// <summary>
  ///   A one-shot completion surface — mirrors the hosting overlay: the
  ///   first write settles the channel, later writes are ignored.
  /// </summary>
  private sealed class RecordingCompletion : IRouterCompletion
  {
    private readonly TaskCompletionSource<object?> _tcs = new();

    /// <summary>The first written result — the one-shot channel keeps it.</summary>
    public object? Captured { get; private set; }
    public int CompleteCount { get; private set; }

    /// <inheritdoc />
    public Task<object?> Result => _tcs.Task;

    public void Complete(object? result)
    {
      CompleteCount++;
      if (CompleteCount == 1)
      {
        Captured = result;
        _tcs.TrySetResult(result);
      }
    }
  }

  /// <summary>A string-result session with an exposed public close (mirrors a real confirm/close).</summary>
  private sealed class TestSession : DialogSessionBase<string>
  {
    public void UserConfirmed(string? value) => Close(value);
  }

  [Fact]
  public void Close_WritesResultToSurface_FirstWriteWins()
  {
    var session = new TestSession();
    var surface = new RecordingCompletion();
    session.Completion = surface;

    session.UserConfirmed("alpha");
    session.UserConfirmed("beta");   // the one-shot surface ignores the second write

    Assert.Equal("alpha", surface.Captured);
    Assert.Equal(2, surface.CompleteCount);
  }

  [Fact]
  public void FreshSession_Surface_IsIndependentAcrossInstances()
  {
    var first = new TestSession();
    var second = new TestSession();
    var firstSurface = new RecordingCompletion();
    var secondSurface = new RecordingCompletion();
    first.Completion = firstSurface;
    second.Completion = secondSurface;

    first.UserConfirmed("one");
    second.UserConfirmed("two");

    Assert.Equal("one", firstSurface.Captured);
    Assert.Equal("two", secondSurface.Captured);
  }

  [Fact]
  public void UnpresentedSession_CloseDropsTheWrite()
  {
    var session = new TestSession();

    // No presentation → no captured surface: the close writes nowhere and
    // never throws — arrival and result stay decoupled.
    session.UserConfirmed("done");
    Assert.Null(session.Completion);
  }
}
