using Everlong.Nester.Intent;
using Xunit;

namespace Everlong.Nester.Tests.Intent;

public class IntentContextTests
{
  private sealed class TestIntent : IIntent { }

  private static IntentContext NewContext(object? sender = null)
    => new(new TestIntent(), sender);

  [Fact]
  public void Handle_SettlesTheConsultation()
  {
    var ctx = NewContext();
    var by = new object();

    ctx.Handle(by);

    Assert.Equal(IntentResult.Handled, ctx.Result);
    Assert.Same(by, ctx.HandledBy);
    Assert.True(ctx.IsTerminated);
  }

  [Fact]
  public void Veto_SettlesTheConsultation()
  {
    var ctx = NewContext();

    ctx.Veto();

    Assert.Equal(IntentResult.Vetoed, ctx.Result);
    Assert.True(ctx.IsTerminated);
  }

  [Fact]
  public void Handle_AfterSettlement_Throws()
  {
    var ctx = NewContext();
    ctx.Handle(new object());

    Assert.Throws<InvalidOperationException>(() => ctx.Handle(new object()));

    Assert.Equal(IntentResult.Handled, ctx.Result);
  }

  [Fact]
  public void Veto_AfterSettlement_Throws()
  {
    var ctx = NewContext();
    ctx.Handle();

    Assert.Throws<InvalidOperationException>(() => ctx.Veto());
  }

  [Fact]
  public void Handle_AfterVeto_Throws()
  {
    var ctx = NewContext();
    ctx.Veto();

    Assert.Throws<InvalidOperationException>(() => ctx.Handle());
  }

  [Fact]
  public void Veto_AfterVeto_Throws()
  {
    var ctx = NewContext();
    ctx.Veto();

    Assert.Throws<InvalidOperationException>(() => ctx.Veto());
  }

  [Fact]
  public void FailedSettlement_KeepsTheOriginalOutcome()
  {
    var ctx = NewContext();
    var first = new object();
    ctx.Handle(first);

    Assert.Throws<InvalidOperationException>(() => ctx.Veto(new object()));

    Assert.Equal(IntentResult.Handled, ctx.Result);
    Assert.Same(first, ctx.HandledBy);
  }

  [Fact]
  public void Items_RemainWritableAfterSettlement()
  {
    var ctx = NewContext();
    ctx.Handle();

    ctx.Items["note"] = "observed";

    Assert.Equal("observed", ctx.Items["note"]);
  }
}
