using Everlong.Nester.Messaging;
using Xunit;

namespace Everlong.Nester.Tests.Messaging;

public class MessageRecipientTests
{
  private sealed record Alpha(int Value) : IMessage;

  private sealed record Beta : IMessage;

  private sealed record Gamma : IMessage;

  private abstract record BaseMessage : IMessage;

  private sealed record LeafMessage : BaseMessage;

  private interface IMarkerMessage : IMessage;

  private sealed record MarkerMessage : IMarkerMessage;

  private sealed record M1 : IMessage;

  private sealed record M2 : IMessage;

  private sealed record M3 : IMessage;

  private sealed record M4 : IMessage;

  private sealed record M5 : IMessage;

  private sealed record M6 : IMessage;

  private sealed class AlphaRecipient : IMessageRecipient<Alpha>
  {
    public List<int> Values { get; } = [];

    public void Receive(Alpha message) => Values.Add(message.Value);
  }

  private sealed class FilteredRecipient : IMessageRecipient<Alpha>
  {
    public int Calls { get; private set; }

    public bool CanReceive(Alpha message) => message.Value > 10;

    public void Receive(Alpha message) => Calls++;
  }

  private sealed class BaseRecipient : IMessageRecipient<BaseMessage>
  {
    public int Calls { get; private set; }

    public void Receive(BaseMessage message) => Calls++;
  }

  private sealed class MarkerRecipient : IMessageRecipient<IMarkerMessage>
  {
    public int Calls { get; private set; }

    public void Receive(IMarkerMessage message) => Calls++;
  }

  private sealed class PairRecipient : IMessageRecipient<Alpha, Beta>
  {
    public int AlphaCalls { get; private set; }

    public int BetaCalls { get; private set; }

    public void Receive(Alpha message) => AlphaCalls++;

    public void Receive(Beta message) => BetaCalls++;
  }

  private sealed class SixRecipient : IMessageRecipient<M1, M2, M3, M4, M5, M6>
  {
    public int Calls { get; private set; }

    public void Receive(M1 message) => Calls++;

    public void Receive(M2 message) => Calls++;

    public void Receive(M3 message) => Calls++;

    public void Receive(M4 message) => Calls++;

    public void Receive(M5 message) => Calls++;

    public void Receive(M6 message) => Calls++;
  }

  /// <summary>Two arity interfaces on one type — resolved by explicit implementation.</summary>
  private sealed class MultiArityRecipient : IMessageRecipient<Alpha>, IMessageRecipient<Beta>
  {
    public int Calls { get; private set; }

    public void Receive(Alpha message) => Calls++;

    public void Receive(Beta message) => Calls++;

    bool IMessageRecipient.CanReceive(IMessage message) => message is Alpha or Beta;

    void IMessageRecipient.Receive(IMessage message)
    {
      switch (message)
      {
        case Alpha alpha:
          Receive(alpha);
          break;
        case Beta beta:
          Receive(beta);
          break;
      }
    }
  }

  private static IMessageRecipient AsRecipient(object recipient) => (IMessageRecipient)recipient;

  [Fact]
  public void Arity1_AcceptsMatchingType_DeclinesOthers()
  {
    var recipient = AsRecipient(new AlphaRecipient());

    Assert.True(recipient.CanReceive(new Alpha(1)));
    Assert.False(recipient.CanReceive(new Beta()));
  }

  [Fact]
  public void Arity1_ReceivesTheMatchingType()
  {
    var typed = new AlphaRecipient();
    IMessageRecipient recipient = typed;

    recipient.Receive(new Alpha(42));

    Assert.Equal([42], typed.Values);
  }

  [Fact]
  public void Arity1_OverrideCanReceive_FiltersByPayload()
  {
    var typed = new FilteredRecipient();
    IMessageRecipient recipient = typed;

    Assert.False(recipient.CanReceive(new Alpha(10)));
    Assert.True(recipient.CanReceive(new Alpha(11)));
    Assert.Equal(0, typed.Calls);
  }

  [Fact]
  public void Arity1_AbstractMessageType_AcceptsDerived()
  {
    var typed = new BaseRecipient();
    IMessageRecipient recipient = typed;

    Assert.True(recipient.CanReceive(new LeafMessage()));

    recipient.Receive(new LeafMessage());

    Assert.Equal(1, typed.Calls);
  }

  [Fact]
  public void Arity1_InterfaceMessageType_AcceptsImplementation()
  {
    var typed = new MarkerRecipient();
    IMessageRecipient recipient = typed;

    Assert.True(recipient.CanReceive(new MarkerMessage()));

    recipient.Receive(new MarkerMessage());

    Assert.Equal(1, typed.Calls);
  }

  [Fact]
  public void Arity2_DispatchesToTheMatchingTypedMember()
  {
    var typed = new PairRecipient();
    IMessageRecipient recipient = typed;

    Assert.True(recipient.CanReceive(new Alpha(1)));
    Assert.True(recipient.CanReceive(new Beta()));
    Assert.False(recipient.CanReceive(new Gamma()));

    recipient.Receive(new Alpha(1));
    recipient.Receive(new Beta());

    Assert.Equal(1, typed.AlphaCalls);
    Assert.Equal(1, typed.BetaCalls);
  }

  [Fact]
  public void Arity2_UnacceptedMessage_Throws()
  {
    IMessageRecipient recipient = new PairRecipient();

    Assert.Throws<InvalidOperationException>(() => recipient.Receive(new Gamma()));
  }

  [Fact]
  public void Arity6_AcceptsEveryDeclaredType()
  {
    var typed = new SixRecipient();
    IMessageRecipient recipient = typed;
    IMessage[] messages = [new M1(), new M2(), new M3(), new M4(), new M5(), new M6()];

    foreach (IMessage message in messages)
    {
      Assert.True(recipient.CanReceive(message));
      recipient.Receive(message);
    }

    Assert.Equal(6, typed.Calls);
  }

  [Fact]
  public void MultipleArityInterfaces_ResolveThroughExplicitImplementation()
  {
    var typed = new MultiArityRecipient();
    IMessageRecipient recipient = typed;

    Assert.True(recipient.CanReceive(new Alpha(1)));
    Assert.True(recipient.CanReceive(new Beta()));
    Assert.False(recipient.CanReceive(new Gamma()));

    recipient.Receive(new Alpha(1));
    recipient.Receive(new Beta());

    Assert.Equal(2, typed.Calls);
  }
}
