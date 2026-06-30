using Avalonia.Controls;
using Everlong.Nester.Presentation;
using NesterApp.Models;

namespace NesterApp.Views;

[ViewFor<ImageMessage>]
public partial class ImageView : UserControl
{
  public ImageView()
  {
    InitializeComponent();
  }
}
