using Everlong.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Dialog;

public class NumpadDialogSessionTests
{
  /// <summary>
  ///   A one-shot completion surface — the presentation's write-back
  ///   channel, faked at unit level (the real overlay is a
  ///   close-stack model on the router's ledger).
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

  [Fact]
  public void Confirm_AfterArithmeticInput_CompletesWithComputedValue()
  {
    NumpadDialogSession session = new()
    {
      ConfirmText = "Confirm",
      Min = -100m,
      Max = 100m
    };

    Append(session, "1", "+", "2", "*", "3");
    Assert.Equal("1+2*3", session.InputText);
    Assert.True(session.CanConfirm);
    Assert.Equal("= 7", session.ResultHint);

    var recording = new RecordingCompletion();
    session.Completion = recording;
    session.ConfirmCommand.Execute(null);

    Assert.Equal(7m, Assert.IsType<decimal>(recording.Captured));
  }

  [Fact]
  public void Confirm_WhenResultOutOfRange_DoesNotCloseAndShowsWarningToast()
  {
    RecordingNoticeService toast = new();
    ServiceProvider provider = new ServiceCollection()
      .AddSingleton<INoticeService>(toast)
      .BuildServiceProvider();

    NumpadDialogSession session = new()
    {
      ConfirmText = "Confirm",
      Min = 0m,
      Max = 5m
    };
    ((IInjectable)session).Inject(provider);
    Append(session, "1", "+", "2", "*", "3");

    var recording = new RecordingCompletion();
    session.Completion = recording;
    Assert.True(session.CanConfirm);

    session.ConfirmCommand.Execute(null);

    Assert.Equal(0, recording.CompleteCount);
    Assert.Contains(toast.Entries, e => e.Level == ToastLevel.Warning && e.Message.Length > 0);
  }

  [Fact]
  public void CanConfirm_WhenExpressionEndsWithOperator_ReturnsFalse()
  {
    NumpadDialogSession session = new()
    {
      ConfirmText = "Confirm"
    };

    Append(session, "1", "+");

    Assert.False(session.CanConfirm);
    Assert.Equal("= ?", session.ResultHint);
  }

  [Fact]
  public void SmartConfirm_WithExpression_EvaluatesOnly()
  {
    NumpadDialogSession session = new()
    {
      ConfirmText = "Confirm",
      Min = -100m,
      Max = 100m
    };

    Append(session, "1", "+", "2");
    Assert.Equal("1+2", session.InputText);

    var recording = new RecordingCompletion();
    session.Completion = recording;

    session.SmartConfirmCommand.Execute(null);

    // Should evaluate to 3
    Assert.Equal("3", session.InputText);

    // Should NOT close the dialog
    Assert.Equal(0, recording.CompleteCount);
  }

  [Fact]
  public void SmartConfirm_WithNumber_Confirms()
  {
    NumpadDialogSession session = new()
    {
      ConfirmText = "Confirm",
      Min = 0m,
      Max = 100m
    };

    session.InputText = "5";

    var recording = new RecordingCompletion();
    session.Completion = recording;

    session.SmartConfirmCommand.Execute(null);

    // Should confirm and close
    Assert.Equal(5m, Assert.IsType<decimal>(recording.Captured));
  }

  private static void Append(NumpadDialogSession session, params string[] keys)
  {
    foreach (string key in keys)
    {
      session.AppendKeyCommand.Execute(key);
    }
  }

  private sealed class RecordingNoticeService : INoticeService
  {
    public List<(string Message, ToastLevel Level)> Entries { get; } = [];

    public void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null)
    {
      Entries.Add((message, level));
    }

    public void Show(string message, string? actionText = null, TimeSpan? duration = null) { }

    public Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null)
      => Task.FromResult(SnackbarResult.Dismissed);

    public void Notify(string title, string message, NotificationLevel level = NotificationLevel.Information,
                       TimeSpan? duration = null, string? actionText = null)
    { }

    public Task<NotificationResult> NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Information,
                                                TimeSpan? duration = null, string? actionText = null)
      => Task.FromResult(NotificationResult.Dismissed);
  }
}
