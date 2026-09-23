using Everlong.Nester.Messaging;
using Everlong.Nester.Hosting;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Hosting;

/// <summary>
///   The session-ending orchestration: guard collection, the prompt gate, the
///   confirmed actions, and the nested-loop re-entrancy refusal.
/// </summary>
public class SessionEndingTests
{
  private sealed class FakeMessageBox(Func<string, bool>? answer = null) : IMessageBox
  {
    public List<string> Prompts { get; } = [];

    public bool Confirm(string title, string message)
    {
      Prompts.Add(message);
      return answer?.Invoke(message) ?? true;
    }
  }

  private sealed class GuardRecipient(string title, string body, Action? confirmed = null) : IMessageRecipient
  {
    public bool CanReceive(IMessage message) => message is SessionEndingMessage;

    public void Receive(IMessage message)
    {
      if (message is SessionEndingMessage sessionEnding)
        sessionEnding.Guard(title, body, confirmed ?? (() => { }));
    }
  }

  [Fact]
  public void Run_NoGuards_Allows()
  {
    var hub = new MessageHub();

    Assert.True(SessionEndingArbitration.Run(hub, new FakeMessageBox()));
  }

  [Fact]
  public void Run_PromptsGuardsInRegistrationOrder()
  {
    var hub = new MessageHub();
    var box = new FakeMessageBox();
    using var first = hub.Register(new GuardRecipient("A", "first"));
    using var second = hub.Register(new GuardRecipient("B", "second"));

    Assert.True(SessionEndingArbitration.Run(hub, box));
    Assert.Equal(new[] { "first", "second" }, box.Prompts);
  }

  [Fact]
  public void Run_DeclinedPrompt_ShortCircuitsAndRunsNoConfirmed()
  {
    var hub = new MessageHub();
    var box = new FakeMessageBox(message => message != "second");
    int confirmed = 0;
    using var first = hub.Register(new GuardRecipient("A", "first", () => confirmed++));
    using var second = hub.Register(new GuardRecipient("B", "second", () => confirmed++));

    Assert.False(SessionEndingArbitration.Run(hub, box));
    Assert.Equal(new[] { "first", "second" }, box.Prompts);
    Assert.Equal(0, confirmed);
  }

  [Fact]
  public void Run_AllConfirmed_RunsEveryConfirmed()
  {
    var hub = new MessageHub();
    var box = new FakeMessageBox();
    int confirmed = 0;
    using var first = hub.Register(new GuardRecipient("A", "first", () => confirmed++));
    using var second = hub.Register(new GuardRecipient("B", "second", () => confirmed++));

    Assert.True(SessionEndingArbitration.Run(hub, box));
    Assert.Equal(2, confirmed);
  }

  [Fact]
  public void Run_ThrowingConfirmed_DoesNotStopTheOthers()
  {
    var hub = new MessageHub();
    var box = new FakeMessageBox();
    bool secondRan = false;
    using var first = hub.Register(
      new GuardRecipient("A", "first", () => throw new InvalidOperationException("boom")));
    using var second = hub.Register(new GuardRecipient("B", "second", () => secondRan = true));

    Assert.True(SessionEndingArbitration.Run(hub, box));
    Assert.True(secondRan);
  }

  [Fact]
  public void Run_ReentrantCall_IsRefused()
  {
    var hub = new MessageHub();
    var box = new FakeMessageBox();
    bool? inner = null;
    using var guard = hub.Register(
      new GuardRecipient("A", "first", () => inner = SessionEndingArbitration.Run(hub, box)));

    Assert.True(SessionEndingArbitration.Run(hub, box));
    Assert.False(inner);
  }
}
