using Avalonia.Controls;
using Everlong.Nester.Presentation;
using NesterApp.Models;

namespace NesterApp.Views;

[ViewFor<AlertMessage>]
public partial class AlertView : UserControl
{
  public AlertView()
  {
    InitializeComponent();
  }
}
