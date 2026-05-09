using Xunit;
using Everlong.Nester.Activation;

namespace Everlong.Nester.Tests.Activation;

/// <summary>
///   Default args→activation-intent heuristics: absolute URIs, existing paths,
///   and the file-path shapes that must never be misread as URIs.
/// </summary>
public sealed class DefaultActivationArgsConverterTests
{
  private readonly DefaultActivationArgsConverter _converter = new();

  [Fact]
  public void Convert_AbsoluteUri_YieldsUriActivationIntent()
  {
    var intents = _converter.Convert(["https://example.com/post/42"]);

    var uri = Assert.Single(intents.OfType<UriActivationIntent>());
    Assert.Equal(new Uri("https://example.com/post/42"), uri.Uri);
  }

  [Fact]
  public void Convert_ExistingFilePath_YieldsFileActivationIntent()
  {
    string path = Path.Combine(Path.GetTempPath(), $"nester-args-{Guid.NewGuid():N}.txt");
    File.WriteAllText(path, "x");
    try
    {
      var intents = _converter.Convert([path]);

      var file = Assert.Single(intents.OfType<FileActivationIntent>());
      Assert.Equal([path], file.Files);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Fact]
  public void Convert_NonexistentWindowsDrivePath_StaysUnrecognized()
  {
    // "C:\..." parses as a URI (scheme "c:") — the file-path shape guard must win.
    var intents = _converter.Convert([@"C:\definitely\not\here.txt"]);

    Assert.Empty(intents);
  }

  [Fact]
  public void Convert_MixedArgs_ProducesUriThenFile()
  {
    string dir = Directory.CreateTempSubdirectory("nester-args-").FullName;
    try
    {
      var intents = _converter.Convert(["https://example.com", dir, "plain-word"]);

      Assert.IsType<UriActivationIntent>(intents[0]);
      var file = Assert.IsType<FileActivationIntent>(intents[1]);
      Assert.Equal([dir], file.Files);
      Assert.Equal(2, intents.Count);
    }
    finally
    {
      Directory.Delete(dir, recursive: true);
    }
  }

  [Fact]
  public void Convert_EmptyOrBlankArgs_YieldsNothing()
  {
    Assert.Empty(_converter.Convert([]));
    Assert.Empty(_converter.Convert(["", "   "]));
  }
}
