using System.ComponentModel.DataAnnotations;
using Everlong.Settings;

namespace NesterApp.Properties;

[Section]
public partial class SecuritySettings : ISettingsSection
{
  [Setting(true)]
  public partial bool RequireTwoFactor { get; set; }

  [Setting(true)]
  public partial bool AutoLockOnInactivity { get; set; }

  [Setting(30)]
  [Range(5, 120)]
  public partial int SessionTimeoutMinutes { get; set; }
}
