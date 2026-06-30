using Everlong.Settings;

namespace NesterApp.Properties;

[Section]
public partial class AccountSettings
{
  [Setting("demo@nester.app")]
  public partial string Email { get; set; }

  [Setting("Nester Demo")]
  public partial string DisplayName { get; set; }

  [Setting(true)]
  public partial bool AllowProfileDiscovery { get; set; }

  [Setting(false)]
  public partial bool HideActivityDetails { get; set; }
}
