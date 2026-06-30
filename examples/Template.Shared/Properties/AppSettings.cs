using System.ComponentModel.DataAnnotations;
using Everlong.DI;
using Everlong.Settings;
using Microsoft.Extensions.DependencyInjection;
using NesterApp.Services;

namespace NesterApp.Properties;

/// <summary>
///   The app's settings root: one <c>[Setting]</c> property per persisted
///   value, grouped into <c>[Section]</c>s.  Values are stored through
///   <c>ISettingsStore</c> (see <c>Services/JsonSettingsStore.cs</c>) and read
///   anywhere as <c>AppSettings.Default.X</c>.
/// </summary>
/// <remarks>
///   Your seam: add your own <c>[Setting]</c> properties and sections, and
///   bind them in XAML.  Validation runs before coercion — raw value, then
///   <c>[Range]</c>, then the <c>[Coercion]</c> method, then the store.
/// </remarks>
[Settings]
public partial class AppSettings : IInjectable
{
  [Setting("en")]
  public partial string Language { get; set; }

  [Setting(AppTheme.System)]
  public partial AppTheme Theme { get; set; }

  [Setting(true)]
  public partial bool AutoSave { get; set; }

  // [Coercion] tells the generator to emit `private static partial int CoerceFontSize(int value)`.
  // Implement the partial method below to transform the value after Range validation passes.
  // Order: raw value → Range clamp → Coercion method → stored.
  [Setting(14)]
  [Range(10, 24)]
  [Coercion]
  public partial int FontSize { get; set; }

  [Setting]
  public partial int WindowWidth { get; set; }

  [Setting]
  public partial int WindowHeight { get; set; }

  [Setting]
  public partial int WindowLeft { get; set; }

  [Setting]
  public partial int WindowTop { get; set; }

  [Setting]
  public partial string[] Tags { get; set; }

  [Section]
  public partial AccountSettings Account { get; }

  [Section]
  public partial SecuritySettings Security { get; }

  [Section]
  public partial FeatureStates Features { get; }

  public virtual void Inject(IServiceProvider services)
  {
    (this as ISettingsRoot).Initialize(services.GetRequiredService<ISettingsStore>());
    // Apply the persisted choice once the store is live.  A host without a
    // theme service (the TerminalGui head) registers none — nothing to do.
    services.GetService<IThemeService>()?.Apply(Theme);
  }


  // Coercion example: snap FontSize to the nearest even number so the UI uses
  // consistent grid-aligned text metrics (8pt grid: 10, 12, 14, 16, 18, 20, 22, 24).
  private static partial int CoerceFontSize(int value)
  {
    return value % 2 == 0 ? value : value + 1;
  }

  private static partial string[] GetTagsDefault()
  {
    return ["C#", "Java", "Javascript", "Go"];
  }
}
