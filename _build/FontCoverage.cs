using System.Buffers.Binary;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;

/// <summary>
///   Coverage verification for the examples' embedded CJK font.
/// </summary>
/// <remarks>
///   <c>examples/Template.Avalonia/Assets/Fonts/NotoSansSC-Regular.otf</c> is a coverage-limited
///   subset — 16.05 MB of the upstream face cut to 0.33 MB — embedded for the Browser and Android
///   hosts, which have no system CJK font to fall back on.  Nothing regenerates it during a build,
///   so a locale edit that introduces a character outside the subset would surface as tofu in those
///   two hosts and nowhere else.  This gate is that "nowhere else": every codepoint the example
///   sources use inside the font's territory has to be mapped by the shipped file.  The territory
///   is the same list <c>_build/font/subset-cjk-font.py</c> builds to, and the two move together.
/// </remarks>
partial class Build
{
  /// <summary>
  ///   The blocks the embedded font is the answer for.  A codepoint outside them — the tour's
  ///   emoji and symbol glyphs — is the platform's, which the desktop host wires an emoji fallback
  ///   for, so this gate has nothing to say about it.
  /// </summary>
  static readonly (int Low, int High)[] FontTerritory =
  [
    (0x20, 0x7E),
    (0xA0, 0xFF),
    (0x2000, 0x206F),
    (0x3000, 0x303F),
    (0x3040, 0x30FF),
    (0x3400, 0x4DBF),
    (0x4E00, 0x9FFF),
    (0xFF00, 0xFFEF),
  ];

  /// <summary>The sources whose text the embedded font has to render.</summary>
  static readonly string[] FontCoverageExtensions = [".cs", ".axaml", ".xaml", ".jsonc", ".json"];

  static readonly string[] FontCoverageExcludedDirectories = ["bin", "obj"];

  /// <summary>
  ///   Verifies that the examples' embedded CJK font maps every codepoint their sources use.
  /// </summary>
  Target VerifyFontCoverage => _ => _
      .Executes(() =>
      {
        var font = RootDirectory / "examples" / "Template.Avalonia" / "Assets" / "Fonts" /
                   "NotoSansSC-Regular.otf";
        if (!File.Exists(font))
          throw new InvalidOperationException($"The embedded CJK font is missing: {font}.");

        var mapped = ReadCmap(font);
        var used = new SortedDictionary<int, string>();

        foreach (var file in Directory.EnumerateFiles(RootDirectory / "examples", "*", SearchOption.AllDirectories))
        {
          if (InExcludedDirectory(file))
            continue;
          if (!FontCoverageExtensions.Contains(Path.GetExtension(file)))
            continue;

          var text = File.ReadAllText(file);
          foreach (var rune in text.EnumerateRunes().Where(r => r.Value >= 0x80 && InFontTerritory(r.Value)))
            used.TryAdd(rune.Value, Path.GetRelativePath(RootDirectory, file));
        }

        var missing = used.Where(entry => !mapped.Contains(entry.Key)).Select(entry => entry.Key).ToArray();
        if (missing.Length > 0)
          throw new InvalidOperationException(
              $"{missing.Length} codepoint(s) the examples use are not in {font.Name}: " +
              string.Join(", ", missing.Select(c => $"U+{c:X4} ({used[c]})")) +
              ". Rebuild the subset (`uv run --with fonttools python _build/font/subset-cjk-font.py`; " +
              "its header covers the case where the committed subset cannot supply the glyph) or " +
              "change the text.");

        Log.Information("The embedded CJK font maps all {Count} codepoints the examples use in its territory.",
                        used.Count);
      });

  static bool InFontTerritory(int codepoint) =>
      FontTerritory.Any(range => codepoint >= range.Low && codepoint <= range.High);

  static bool InExcludedDirectory(string path) =>
      path.Split(Path.DirectorySeparatorChar).Any(FontCoverageExcludedDirectories.Contains);

  /// <summary>
  ///   Reads the codepoints an OpenType font maps, from every cmap subtable it carries.
  /// </summary>
  static HashSet<int> ReadCmap(AbsolutePath font)
  {
    var data = File.ReadAllBytes(font);
    var cmapOffset = -1;
    var tableCount = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4));
    for (var i = 0; i < tableCount; i++)
    {
      var record = 12 + (i * 16);
      if (data[record] != 'c' || data[record + 1] != 'm' || data[record + 2] != 'a' || data[record + 3] != 'p')
        continue;

      cmapOffset = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(record + 8));
      break;
    }

    if (cmapOffset < 0)
      throw new InvalidOperationException($"No cmap table in {font}.");

    var codepoints = new HashSet<int>();
    var subtableCount = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(cmapOffset + 2));
    for (var i = 0; i < subtableCount; i++)
    {
      var subtable = cmapOffset + (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(cmapOffset + 4 + (i * 8) + 4));
      switch (BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(subtable)))
      {
        case 4:
          ReadFormat4(data, subtable, codepoints);
          break;
        case 12:
          ReadFormat12(data, subtable, codepoints);
          break;
      }
    }

    return codepoints;
  }

  static void ReadFormat4(byte[] data, int subtable, HashSet<int> codepoints)
  {
    int Read(int offset) => BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset));

    var segmentCount = Read(subtable + 6) / 2;
    var endCodes = subtable + 14;
    var startCodes = endCodes + (segmentCount * 2) + 2;
    var deltas = startCodes + (segmentCount * 2);
    var rangeOffsets = deltas + (segmentCount * 2);

    for (var segment = 0; segment < segmentCount; segment++)
    {
      var end = Read(endCodes + (segment * 2));
      var start = Read(startCodes + (segment * 2));
      if (start == 0xFFFF)
        continue;

      var delta = (short)Read(deltas + (segment * 2));
      var rangeOffset = Read(rangeOffsets + (segment * 2));
      for (var codepoint = start; codepoint <= end; codepoint++)
      {
        var glyph = rangeOffset == 0
            ? (codepoint + delta) & 0xFFFF
            : Read(rangeOffsets + (segment * 2) + rangeOffset + ((codepoint - start) * 2));
        if (glyph != 0)
          codepoints.Add(codepoint);
      }
    }
  }

  static void ReadFormat12(byte[] data, int subtable, HashSet<int> codepoints)
  {
    var groupCount = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(subtable + 12));
    for (var group = 0; group < groupCount; group++)
    {
      var record = subtable + 16 + (group * 12);
      var start = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(record));
      var end = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(record + 4));
      if (start > 0x10FFFF || start > end)
        continue;

      for (var codepoint = start; codepoint <= Math.Min(end, 0x10FFFF); codepoint++)
        codepoints.Add(codepoint);
    }
  }
}
