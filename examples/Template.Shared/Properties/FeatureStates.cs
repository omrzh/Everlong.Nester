using Everlong.Settings;

namespace NesterApp.Properties;

[Section]
public partial class FeatureStates
{
  // Not used yet
  [Setting]
  public partial bool IsExperimentalFeatureEnabled { get; set; }
}
