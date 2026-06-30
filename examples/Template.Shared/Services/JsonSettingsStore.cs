using System.Text.Json;
using System.Text.Json.Serialization;
using Everlong.DI;
using Everlong.Settings;

namespace NesterApp.Services;

/// <summary>
///   A file-backed settings store that persists all values as a flat JSON dictionary
///   at <c>%AppData%\NesterApp\settings.json</c>.
/// </summary>
[Singleton<ISettingsStore>]
public sealed class JsonSettingsStore : SettingsStoreBase
{
  private readonly string _filePath;
  private Dictionary<string, string> _allEntries = [];

  public JsonSettingsStore()
  {
    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    _filePath = Path.Combine(appData, "NesterApp", "settings.json");
  }

  protected override IEnumerable<KeyValuePair<string, string>> LoadAll()
  {
    if (!File.Exists(_filePath))
    {
      return [];
    }

    try
    {
      string json = File.ReadAllText(_filePath);
      _allEntries = JsonSerializer.Deserialize(
                      json,
                      JsonSettingsStoreJsonContext.Default.DictionaryStringString)
                    ?? [];
    }
    catch
    {
      _allEntries = [];
    }

    return _allEntries;
  }

  protected override async Task PersistAsync(string key, string value, CancellationToken ct)
  {
    _allEntries[key] = value;

    string dir = Path.GetDirectoryName(_filePath)!;
    Directory.CreateDirectory(dir);

    string json = JsonSerializer.Serialize(
      _allEntries,
      JsonSettingsStoreJsonContext.Default.DictionaryStringString);
    await File.WriteAllTextAsync(_filePath, json, ct);
  }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class JsonSettingsStoreJsonContext : JsonSerializerContext;
