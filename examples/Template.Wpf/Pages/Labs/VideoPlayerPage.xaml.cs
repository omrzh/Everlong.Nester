using System.Windows.Controls;

namespace NesterApp.Pages.Labs;

// No [ViewFor<VideoPlayerViewModel>] here — the mapping is declared centrally on ViewLocator
// with [Mapping<VideoPlayerViewModel, VideoPlayerPage>] in ViewLocators.cs.
public partial class VideoPlayerPage : UserControl
{
  public VideoPlayerPage()
  {
    InitializeComponent();
  }
}
