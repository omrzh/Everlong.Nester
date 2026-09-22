using Everlong.Nester.Dialog;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Dialog;

/// <summary>
///   The options-to-dimmer dispatch the dialog sugar is built on: the state an
///   options POCO carries is what the presented dimmer runs with, and the
///   dispatch is public so a project's own sugar derives the same dimmer.
/// </summary>
public class DialogDimmerOptionsTests
{
  private sealed class Options : DialogOptions
  {
    public string? Message { get; init; }
  }

  [Fact]
  public void ToDimmer_InvertsMandatoryIntoLightDismiss()
  {
    DefaultDimmerModel byDefault = new Options().ToDimmer();
    Assert.True(byDefault.LightDismiss);
    Assert.True(byDefault.ShakeVetoedDismissal);

    DefaultDimmerModel mandatory = new Options { IsMandatory = true }.ToDimmer();
    Assert.False(mandatory.LightDismiss);
  }

  [Fact]
  public void ToDimmer_CarriesTheShakeSetting()
  {
    DefaultDimmerModel dimmer = new Options { ShakeVetoedDismissal = false }.ToDimmer();

    Assert.True(dimmer.LightDismiss);
    Assert.False(dimmer.ShakeVetoedDismissal);
  }
}
