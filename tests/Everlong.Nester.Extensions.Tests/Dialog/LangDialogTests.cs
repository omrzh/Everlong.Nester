using Everlong.Globalization;
using Everlong.Nester.Extensions.Properties;
using Xunit;

namespace Everlong.Nester.Tests.Dialog;

public class LangDialogTests : IDisposable
{
  public void Dispose()
    => Lang.Provider.Use(Lang.Locales.En);

  // ── Provider switching ─────────────────────────────────────────────────────

  [Fact]
  public void Lang_UseNullProvider_DialogStringsFallBackToEnglish()
  {
    Lang.Provider.Use(NullStringProvider.Instance);
    Assert.Equal("Alert", Lang.Dialog.Alert.Title);
    Assert.Equal("OK", Lang.Dialog.Alert.ButtonText);
  }

  [Fact]
  public void Lang_UseCustomProvider_ChangesDialogStrings()
  {
    var translations = new Dictionary<string, string>
    {
      ["Everlong.Nester.Dialog.Alert.Title"] = "提示",
      ["Everlong.Nester.Dialog.Alert.ButtonText"] = "确认",
      ["Everlong.Nester.Dialog.Confirm.Title"] = "确认",
      ["Everlong.Nester.Dialog.Confirm.ConfirmText"] = "是",
      ["Everlong.Nester.Dialog.Confirm.CancelText"] = "否"
    };
    Lang.Provider.Use(new KeyedProvider(translations));

    Assert.Equal("提示", Lang.Dialog.Alert.Title);
    Assert.Equal("确认", Lang.Dialog.Alert.ButtonText);
    Assert.Equal("确认", Lang.Dialog.Confirm.Title);
    Assert.Equal("是", Lang.Dialog.Confirm.ConfirmText);
    Assert.Equal("否", Lang.Dialog.Confirm.CancelText);
  }

  // ── Fallback ───────────────────────────────────────────────────────────────

  [Fact]
  public void Lang_PartialProvider_MissingKeyReturnsFallback()
  {
    Lang.Provider.Use(new KeyedProvider(new Dictionary<string, string>
    {
      ["Everlong.Nester.Dialog.Alert.Title"] = "提示"
    }));

    Assert.Equal("提示", Lang.Dialog.Alert.Title);
    Assert.Equal("OK", Lang.Dialog.Alert.ButtonText);  // not in provider → English fallback
  }

  // ── Live read: switching provider changes values instantly ─────────────────

  [Fact]
  public void Lang_SwitchProvider_UpdatesLiveValues()
  {
    Lang.Provider.Use(NullStringProvider.Instance);
    Assert.Equal("Alert", Lang.Dialog.Alert.Title);

    Lang.Provider.Use(new KeyedProvider(new Dictionary<string, string>
    {
      ["Everlong.Nester.Dialog.Alert.Title"] = "警告"
    }));

    Assert.Equal("警告", Lang.Dialog.Alert.Title);
  }

  // ── Default locale (en) ────────────────────────────────────────────────────

  [Fact]
  public void Lang_DefaultLocale_ReturnsEnglishStrings()
  {
    Assert.Equal("Alert", Lang.Dialog.Alert.Title);
    Assert.Equal("OK", Lang.Dialog.Alert.ButtonText);
    Assert.Equal("Confirm", Lang.Dialog.Confirm.Title);
    Assert.Equal("Yes", Lang.Dialog.Confirm.ConfirmText);
    Assert.Equal("Cancel", Lang.Dialog.Confirm.CancelText);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private sealed class KeyedProvider(IReadOnlyDictionary<string, string> data) : IStringProvider
  {
    public string GetString(string key, string fallback = "")
      => data.TryGetValue(key, out var v) ? v : fallback;
  }
}
