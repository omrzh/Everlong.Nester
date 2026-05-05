using Everlong.Nester.Messaging;
using Xunit;

namespace Everlong.Nester.Tests.Messaging;

public class MessageHubTests
{
  private sealed record Ping(int Value) : IMessage;

  private sealed record Pong : IMessage;

  private sealed class Recipient(Func<IMessage, bool>? accepts = null, Action<IMessage>? onReceive = null) : IMessageRecipient
  {
    public int Calls { get; private set; }

    public IMessage? Last { get; private set; }

    public bool CanReceive(IMessage message) => accepts?.Invoke(message) ?? true;

    public void Receive(IMessage message)
    {
      Calls++;
      Last = message;
      onReceive?.Invoke(message);
    }
  }

  private sealed class ThrowingRecipient : IMessageRecipient
  {
    public bool CanReceive(IMessage message) => true;

    public void Receive(IMessage message) => throw new InvalidOperationException("receive failed");
  }

  private sealed class ThrowingPredicate : IMessageRecipient
  {
    public bool CanReceive(IMessage message) => throw new InvalidOperationException("predicate failed");

    public void Receive(IMessage message) => throw new InvalidOperationException("must not be reached");
  }

  /// <summary>Value-equal recipients — registration must stay reference-identified.</summary>
  private sealed class EqualRecipient : IMessageRecipient
  {
    public int Calls { get; private set; }

    public bool CanReceive(IMessage message) => true;

    public void Receive(IMessage message) => Calls++;

    public override bool Equals(object? obj) => obj is EqualRecipient;

    public override int GetHashCode() => 0;
  }

  [Fact]
  public void Publish_DeliversToAcceptingRecipientsInRegistrationOrder()
  {
    var hub = new MessageHub();
    var order = new List<int>();
    var first = new Recipient(onReceive: _ => order.Add(1));
    var second = new Recipient(onReceive: _ => order.Add(2));
    hub.Register(first);
    hub.Register(second);

    hub.Publish(new Ping(7));

    Assert.Equal([1, 2], order);
    Assert.Same(first.Last, second.Last);
    Assert.Equal(new Ping(7), first.Last);
  }

  [Fact]
  public void Publish_SkipsRecipientsThatDecline()
  {
    var hub = new MessageHub();
    var declining = new Recipient(_ => false);
    var accepting = new Recipient();
    hub.Register(declining);
    hub.Register(accepting);

    hub.Publish(new Ping(1));

    Assert.Equal(0, declining.Calls);
    Assert.Equal(1, accepting.Calls);
  }

  [Fact]
  public void Publish_WithNoRecipients_IsNoOp()
  {
    var hub = new MessageHub();

    hub.Publish(new Ping(1));
  }

  [Fact]
  public void Publish_Null_Throws()
  {
    var hub = new MessageHub();

    Assert.Throws<ArgumentNullException>(() => hub.Publish(null!));
  }

  [Fact]
  public void Register_Null_Throws()
  {
    var hub = new MessageHub();

    Assert.Throws<ArgumentNullException>(() => hub.Register(null!));
  }

  [Fact]
  public void Register_SameInstanceTwice_Throws()
  {
    var hub = new MessageHub();
    var recipient = new Recipient();
    hub.Register(recipient);

    Assert.Throws<InvalidOperationException>(() => hub.Register(recipient));
  }

  [Fact]
  public void Register_DistinctEqualInstances_RegistersBoth()
  {
    var hub = new MessageHub();
    var first = new EqualRecipient();
    var second = new EqualRecipient();

    hub.Register(first);
    hub.Register(second);
    hub.Publish(new Ping(1));

    Assert.Equal(1, first.Calls);
    Assert.Equal(1, second.Calls);
  }

  [Fact]
  public void Unregister_UsesReferenceIdentity()
  {
    var hub = new MessageHub();
    var first = new EqualRecipient();
    var second = new EqualRecipient();
    hub.Register(first);
    hub.Register(second);

    hub.Unregister(first);
    hub.Publish(new Ping(1));

    Assert.Equal(0, first.Calls);
    Assert.Equal(1, second.Calls);
  }

  [Fact]
  public void Unregister_AbsentRecipient_IsNoOp()
  {
    var hub = new MessageHub();
    var registered = new Recipient();
    hub.Register(registered);

    hub.Unregister(new Recipient());
    hub.Publish(new Ping(1));

    Assert.Equal(1, registered.Calls);
  }

  [Fact]
  public void DisposeToken_UnregistersRecipient()
  {
    var hub = new MessageHub();
    var recipient = new Recipient();
    IDisposable token = hub.Register(recipient);

    token.Dispose();
    hub.Publish(new Ping(1));

    Assert.Equal(0, recipient.Calls);
  }

  [Fact]
  public void DisposeToken_Twice_IsIdempotent()
  {
    var hub = new MessageHub();
    var first = new Recipient();
    var second = new Recipient();
    hub.Register(first);
    IDisposable token = hub.Register(second);

    token.Dispose();
    token.Dispose();

    hub.Register(new Recipient()); // a stale token must not evict anyone else
    hub.Publish(new Ping(1));

    Assert.Equal(0, second.Calls);
  }

  [Fact]
  public void Publish_WhenReceiveThrows_IsolatesAndAggregates()
  {
    var hub = new MessageHub();
    var survivor = new Recipient();
    hub.Register(new ThrowingRecipient());
    hub.Register(survivor);

    var exception = Assert.Throws<MessageReceiveException>(() => hub.Publish(new Ping(1)));

    Assert.Single(exception.InnerExceptions);
    Assert.Equal(1, survivor.Calls);
  }

  [Fact]
  public void Publish_WhenCanReceiveThrows_AbortsAndPropagates()
  {
    var hub = new MessageHub();
    var survivor = new Recipient();
    hub.Register(new ThrowingPredicate());
    hub.Register(survivor);

    var exception = Assert.Throws<InvalidOperationException>(() => hub.Publish(new Ping(1)));

    Assert.Equal("predicate failed", exception.Message);
    Assert.Equal(0, survivor.Calls);
  }

  [Fact]
  public void Publish_NestedBroadcast_SettlesInnerFirst()
  {
    var hub = new MessageHub();
    var order = new List<string>();
    var outer = new Recipient(message => message is Ping, onReceive: _ =>
    {
      order.Add("outer-before");
      hub.Publish(new Pong());
      order.Add("outer-after");
    });
    var inner = new Recipient(message => message is Pong, onReceive: _ => order.Add("inner"));
    hub.Register(outer);
    hub.Register(inner);

    hub.Publish(new Ping(1));

    Assert.Equal(["outer-before", "inner", "outer-after"], order);
  }

  [Fact]
  public void Register_DuringDispatch_TakesEffectOnTheNextBroadcast()
  {
    var hub = new MessageHub();
    var late = new Recipient();
    var registered = false;
    var early = new Recipient(onReceive: _ =>
    {
      if (registered)
        return;

      registered = true;
      hub.Register(late);
    });
    hub.Register(early);

    hub.Publish(new Ping(1));
    Assert.Equal(0, late.Calls);

    hub.Publish(new Ping(2));
    Assert.Equal(1, late.Calls);
  }

  [Fact]
  public void Unregister_DuringDispatch_KeepsTheCurrentRoster()
  {
    var hub = new MessageHub();
    var later = new Recipient();
    var earlier = new Recipient(onReceive: _ => hub.Unregister(later));
    hub.Register(earlier);
    hub.Register(later);

    hub.Publish(new Ping(1));
    Assert.Equal(1, later.Calls); // the roster is fixed when the broadcast starts

    hub.Publish(new Ping(2));
    Assert.Equal(1, later.Calls);
  }

  [Fact]
  public void HasRecipients_ReflectsRegistrations()
  {
    var hub = new MessageHub();
    Assert.False(hub.HasRecipients);

    IDisposable token = hub.Register(new Recipient());
    Assert.True(hub.HasRecipients);

    token.Dispose();
    Assert.False(hub.HasRecipients);
  }

  [Fact]
  public void Publish_ConcurrentRegistrationsAndBroadcasts_DoesNotThrow()
  {
    var hub = new MessageHub();
    var recipient = new Recipient();
    hub.Register(recipient);

    Parallel.For(0, 200, i =>
    {
      IDisposable token = hub.Register(new Recipient());
      hub.Publish(new Ping(i));
      token.Dispose();
    });

    Assert.True(recipient.Calls > 0);
  }
}
