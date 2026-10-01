using Everlong.Nester.Layer;
using Everlong.Nester.Presentation;

namespace Everlong.Nester.Notice;

/// <summary>
///   The WPF notice tenant — rents the notice band, resolves the panel from
///   the tree and hands every show call to the <see cref="NoticeEngine" />.
/// </summary>
internal sealed class NoticeService(ILayerBroker broker, NoticeServiceOptions options)
  : NoticeServiceBase(broker, new NoticeEngine(), options)
{
  /// <summary>The panel's mount point — the bare container the resolved panel lands in.</summary>
  protected override object CreateHost() => new PGrid();

  /// <summary>
  ///   Resolves the panel from the mounted container (its template table is
  ///   reachable from there) and binds it to the engine.
  /// </summary>
  protected override void OnHostMounted(object host)
  {
    var container = (PGrid)host;
    PControl? view = ViewResolution.Build(container, Options.Panel);
    if (view is not INoticePanel panel)
    {
      var fallback = new NoticePanel();
      panel = fallback;
      view = fallback;
    }

    panel.AttachModel(Options.Panel);
    container.Children.Add(view);
    ((NoticeEngine)Engine).AttachPanel(panel);
  }
}
