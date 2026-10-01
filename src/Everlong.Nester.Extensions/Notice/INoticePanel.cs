namespace Everlong.Nester.Notice;

/// <summary>
///   The notice domain's platform surface — presents and dismisses entries
///   in their channel's region and owns every visual decision: where a
///   region anchors, how it is spaced and how an entry animates.
/// </summary>
/// <remarks>
///   Resolved from the tree through <see cref="NoticePanelModel" /> (a
///   registration in the application's template table), never injected; an
///   application supplies its own panel by mapping its own model.  All
///   members run on the UI thread.
/// </remarks>
public interface INoticePanel
{
  /// <summary>Adopts the panel model — the visual options the panel lays out by.</summary>
  void AttachModel(NoticePanelModel model);

  /// <summary>Presents an entry in its channel's region; completes when the entry is on screen.</summary>
  Task PresentAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token);

  /// <summary>Dismisses an entry from its channel's region; completes when the entry has left.</summary>
  Task DismissAsync(INoticeEntry entry, NoticeChannel channel, CancellationToken token);
}
