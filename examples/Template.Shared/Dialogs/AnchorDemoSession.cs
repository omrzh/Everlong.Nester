using CommunityToolkit.Mvvm.ComponentModel;
using Everlong.Nester.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NesterApp.Properties;

namespace NesterApp.Dialogs;

public partial class AnchorDemoSession : DialogSessionBase<bool>
{
  /// <summary>Gets or sets the heading the demo view renders.</summary>
  [ObservableProperty] public partial string? Title { get; set; }

  public AnchorDemoSession()
  {
    Title = Lang.Labs.DemoDialogTitle;
  }

  [RelayCommand]
  private void Done() => Close(true);
}
