using Avalonia.Controls;
using Everlong.Nester.Presentation;
using NesterApp.Models;

namespace NesterApp.Views;

[ViewFor<TextMessage>]
public partial class TextView : UserControl
{
  public TextView()
  {
    InitializeComponent();
  }
}
