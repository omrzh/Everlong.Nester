using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Presentation;

/// <summary>
///   Algorithm tests for <see cref="BodyPanelController{T}"/>.
///   Uses plain <see cref="object"/> instances — no UI framework or UI thread required.
/// </summary>
public class BodyPanelControllerTests
{
  private readonly object _pageA = new();
  private readonly object _pageB = new();
  private readonly object _pageC = new();
  private readonly object _pageD = new();

  // ── Basic Switch ──

  [Fact]
  public void Switch_EmptyPanel_AddsAndUpdatesActive()
  {
    BodyPanelController<object> ctrl = new();

    ctrl.Switch(_pageA);

    Assert.Equal(_pageA, ctrl.Active);
    Assert.Single(ctrl.Children, _pageA);
  }

  [Fact]
  public void Switch_ToNewPage_AddsNewAndUpdatesActive()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);

    ctrl.Switch(_pageB);

    Assert.Equal(_pageB, ctrl.Active);
    Assert.Equal(2, ctrl.Children.Count);
    Assert.Contains(_pageA, ctrl.Children);
    Assert.Contains(_pageB, ctrl.Children);
  }

  // ── Back navigation (cached page reuse) ──

  [Fact]
  public void Switch_BackToCachedPage_UpdatesActiveWithoutAdding()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);

    ctrl.Switch(_pageA);

    Assert.Equal(_pageA, ctrl.Active);
    Assert.Equal(2, ctrl.Children.Count);
  }

  // ── Refresh ──

  [Fact]
  public void Switch_SamePage_ActiveUnchangedAndChildrenUnaffected()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);

    ctrl.Switch(_pageA);

    Assert.Equal(_pageA, ctrl.Active);
    Assert.Single(ctrl.Children);
  }

  // ── Release ──

  [Fact]
  public void Release_NonActivePage_RemovesFromPanelAndActiveUnchanged()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageA);

    BodyPanelChangeSet<object> cs = ctrl.Release(_pageB);

    Assert.Single(cs.ToRemove, _pageB);
    Assert.Empty(cs.ToAdd);
    Assert.False(cs.IsRefresh);
    Assert.Equal(_pageA, ctrl.Active);
    Assert.Single(ctrl.Children, _pageA);
  }

  [Fact]
  public void Release_PageNotInPanel_IsIdempotentAndReturnsEmpty()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);

    BodyPanelChangeSet<object> cs = ctrl.Release(_pageB);

    Assert.Empty(cs.ToRemove);
    Assert.False(cs.IsRefresh);
    Assert.Single(ctrl.Children, _pageA);
  }

  [Fact]
  public void Release_EmptyPanel_IsIdempotentAndReturnsEmpty()
  {
    BodyPanelController<object> ctrl = new();

    BodyPanelChangeSet<object> cs = ctrl.Release(_pageA);

    Assert.Empty(cs.ToRemove);
    Assert.Empty(ctrl.Children);
  }

  // ── Multi-page coexistence ──

  [Fact]
  public void Switch_MultiplePages_OnlyUpdatesActive()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageC);

    ctrl.Switch(_pageB);

    Assert.Equal(_pageB, ctrl.Active);
    Assert.Equal(3, ctrl.Children.Count);
  }

  [Fact]
  public void Switch_MultiplePages_SwitchToNew_AddsAndUpdatesActive()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageC);

    ctrl.Switch(_pageD);

    Assert.Equal(_pageD, ctrl.Active);
    Assert.Equal(4, ctrl.Children.Count);
  }

  // ── Sequential Release ──

  [Fact]
  public void Release_Sequential_EachReleaseOnlyRemovesItsTarget()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageC);
    ctrl.Switch(_pageA);

    BodyPanelChangeSet<object> csB = ctrl.Release(_pageB);
    BodyPanelChangeSet<object> csC = ctrl.Release(_pageC);

    Assert.Single(csB.ToRemove, _pageB);
    Assert.Single(csC.ToRemove, _pageC);
    Assert.Single(ctrl.Children, _pageA);
    Assert.Equal(_pageA, ctrl.Active);
  }

  [Fact]
  public void Release_ThenSwitchToReleasedPage_ReAddsItToPanel()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageA);
    ctrl.Release(_pageB);

    ctrl.Switch(_pageB);

    Assert.Equal(_pageB, ctrl.Active);
    Assert.Equal(2, ctrl.Children.Count);
    Assert.Contains(_pageA, ctrl.Children);
    Assert.Contains(_pageB, ctrl.Children);
  }

  // ── Re-entry / boundary ──

  [Fact]
  public void Switch_CalledTwiceWithSameTarget_SecondCallIsNoOp()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);

    ctrl.Switch(_pageA);
    ctrl.Switch(_pageA);

    Assert.Equal(_pageA, ctrl.Active);
    Assert.Equal(2, ctrl.Children.Count);
  }

  [Fact]
  public void Switch_BackAndForthMultipleTimes_MaintainsCorrectState()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);

    ctrl.Switch(_pageA);

    Assert.Equal(_pageA, ctrl.Active);
    Assert.Equal(2, ctrl.Children.Count);
  }

  [Fact]
  public void Switch_NullTarget_ThrowsArgumentNullException()
  {
    BodyPanelController<object> ctrl = new();

    Assert.Throws<ArgumentNullException>(() => ctrl.Switch(null!));
  }

  [Fact]
  public void Release_NullChild_ThrowsArgumentNullException()
  {
    BodyPanelController<object> ctrl = new();

    Assert.Throws<ArgumentNullException>(() => ctrl.Release(null!));
  }

  // ── State invariants ──

  [Fact]
  public void Switch_ActiveUpdatedImmediately()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);

    Assert.Equal(_pageB, ctrl.Active);
  }

  [Fact]
  public void Release_ChildRemovedFromChildren_ImmediatelyAfterRelease()
  {
    BodyPanelController<object> ctrl = new();
    ctrl.Switch(_pageA);
    ctrl.Switch(_pageB);
    ctrl.Switch(_pageA);

    ctrl.Release(_pageB);

    Assert.DoesNotContain(_pageB, ctrl.Children);
    Assert.Equal(_pageA, ctrl.Active);
  }

  [Fact]
  public void EnsureAdded_ReturnsToAddForNewChild()
  {
    BodyPanelController<object> ctrl = new();

    BodyPanelChangeSet<object> cs = ctrl.EnsureAdded(_pageA);

    Assert.Single(cs.ToAdd, _pageA);
    Assert.Empty(cs.ToRemove);
    Assert.False(cs.IsRefresh);
    Assert.Null(ctrl.Active); // Switch never called
    Assert.Single(ctrl.Children, _pageA);
  }

  [Fact]
  public void Switch_RequiresNoUIThread_PureDataComputation()
  {
    BodyPanelController<object> ctrl = new();
    Exception? threadEx = null;

    Thread t = new(() =>
    {
      try
      { ctrl.Switch(_pageA); }
      catch (Exception ex) { threadEx = ex; }
    });
    t.Start();
    t.Join();

    Assert.Null(threadEx);
    Assert.Equal(_pageA, ctrl.Active);
    Assert.Single(ctrl.Children, _pageA);
  }
}
