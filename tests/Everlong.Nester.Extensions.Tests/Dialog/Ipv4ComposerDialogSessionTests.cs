using System.Net;
using Everlong.Nester.Dialog;
using Everlong.Nester.Routing;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Dialog;

public class Ipv4ComposerDialogSessionTests
{
  [Fact]
  public void Confirm_WithPaddedOctets_ReadsThemAsDecimal()
  {
    Ipv4ComposerDialogSession session = CreateSession();
    session.Part1 = "192";
    session.Part2 = "168";
    session.Part3 = "001";
    session.Part4 = "010";

    // IPAddress.Parse read the padded parts as octal: "010" landed as 8 while the dialog showed 010.
    Assert.Equal(IPAddress.Parse("192.168.1.10"), Confirm(session));
  }

  [Fact]
  public void Confirm_WithPaddedNine_CompletesInsteadOfThrowing()
  {
    Ipv4ComposerDialogSession session = CreateSession();
    session.Part1 = "192";
    session.Part2 = "168";
    session.Part3 = "1";
    session.Part4 = "009";

    // "009" is not a legal octal run, so IPAddress.Parse threw from inside the confirm command.
    Assert.Equal(IPAddress.Parse("192.168.1.9"), Confirm(session));
  }

  [Fact]
  public void Confirm_WithPlainOctets_CompletesWithTheAddress()
  {
    Ipv4ComposerDialogSession session = CreateSession();
    session.Part1 = "10";
    session.Part2 = "0";
    session.Part3 = "0";
    session.Part4 = "255";

    Assert.Equal(IPAddress.Parse("10.0.0.255"), Confirm(session));
  }

  [Fact]
  public void Confirm_WithEmptyPart_CompletesNothing()
  {
    Ipv4ComposerDialogSession session = CreateSession();
    session.Part1 = "10";
    session.Part2 = "0";
    session.Part3 = "0";
    session.Part4 = "";

    RecordingCompletion completion = new();
    session.Completion = completion;

    session.ConfirmCommand.Execute(null);

    Assert.Equal(0, completion.CompleteCount);
  }

  [Fact]
  public void Confirm_WithPartAboveTheByteRange_CompletesNothing()
  {
    Ipv4ComposerDialogSession session = CreateSession();
    session.Part1 = "256";
    session.Part2 = "0";
    session.Part3 = "0";
    session.Part4 = "1";

    RecordingCompletion completion = new();
    session.Completion = completion;

    session.ConfirmCommand.Execute(null);

    Assert.Equal(0, completion.CompleteCount);
  }

  private static Ipv4ComposerDialogSession CreateSession() => new()
  {
    ConfirmText = "Confirm",
    ClearText = "Clear",
    PingText = "Ping"
  };

  private static IPAddress Confirm(Ipv4ComposerDialogSession session)
  {
    RecordingCompletion completion = new();
    session.Completion = completion;

    session.ConfirmCommand.Execute(null);

    return Assert.IsType<IPAddress>(completion.Captured);
  }

  /// <summary>
  ///   A one-shot completion surface — the presentation's write-back channel, faked at unit level.
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
}
