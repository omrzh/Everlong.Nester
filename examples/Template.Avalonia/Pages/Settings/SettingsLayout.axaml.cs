using Avalonia.Controls;
using Everlong.Nester.Presentation;

namespace NesterApp.Pages.Settings;

[ViewFor<SettingsLayoutModel>]
public partial class SettingsLayout : UserControl, ILayoutControl, ISceneTransition
{
  public SettingsLayout()
  {
    InitializeComponent();
  }

  public ILayoutBody GetLayoutBody() => Body;

  public async Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
  {
    // The layout is the arriving head — its frame stays put; the inner
    // page choreographs itself with a context scoped past the layout.
    context.ShowArriving();
    if (context.NextDirectorAfter(this) is { } inner)
      await inner.AnimateEnterAsync(context.ScopedFrom(this), token);
  }

  public async Task AnimateExitAsync(TransitionContext context, CancellationToken token)
  {
    if (context.NextDirectorAfter(this) is { } inner)
      await inner.AnimateExitAsync(context, token);
  }
}
