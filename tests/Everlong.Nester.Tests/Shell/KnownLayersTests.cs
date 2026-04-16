using Everlong.Nester.Layer;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The band table's structural contract: the named bands are disjoint and
///   ordered, and <see cref="KnownLayers.Flying"/> stays above them all.
/// </summary>
public class KnownLayersTests
{
  [Fact]
  public void Bands_AreDisjointAndOrdered()
  {
    LayerBand[] bands =
    [
      KnownLayers.Backdrop,
      KnownLayers.Navigation,
      KnownLayers.Floating,
      KnownLayers.Dialog,
      KnownLayers.Notice,
      KnownLayers.DevTool,
    ];

    for (int i = 1; i < bands.Length; i++)
      Assert.True(bands[i - 1].Ceiling < bands[i].Floor,
        $"{bands[i - 1].Floor}..{bands[i - 1].Ceiling} overlaps {bands[i].Floor}..{bands[i].Ceiling}");
    Assert.True(bands[^1].Ceiling < KnownLayers.Flying.Floor,
      "The last band must sit below Flying.");
  }

  [Fact]
  public void Band_ContainsIsClosedOnBothEnds()
  {
    var band = new LayerBand(10, 12);

    Assert.False(band.Contains(9));
    Assert.True(band.Contains(10));
    Assert.True(band.Contains(12));
    Assert.False(band.Contains(13));
    Assert.Equal(3, band.Height);
  }

  [Fact]
  public void Band_At_IsASingleZBand()
  {
    Assert.Equal(new LayerBand(42, 42), LayerBand.At(42));
    Assert.Equal(1, LayerBand.At(42).Height);
  }
}
