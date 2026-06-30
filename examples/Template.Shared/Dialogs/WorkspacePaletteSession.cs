using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.ComponentModel;
using Everlong.Nester.Intent;
using Everlong.Nester.Messaging;
using Everlong.Nester.Notice;
using Everlong.Nester.Routing;
using Everlong.Nester.Threading;
using NesterApp.Properties;
using NesterApp.Services;

namespace NesterApp.Dialogs;

/// <summary>The gesture's intent: present the workspace palette.</summary>
public sealed record ShowWorkspacePaletteIntent : IIntent;

/// <summary>
///   The workspace palette (Ctrl+Shift+P) — a derived-router presentation over
///   the watched workspace: type to filter, arrow keys to move, Enter opens the
///   file in the OS's default application.  The gesture reaches it as an intent
///   and the Director answers by presenting this session, so a dismissal is an
///   ordinary <see langword="null" /> result.
/// </summary>
/// <remarks>
///   <para>
///     A session is a model like any other — it takes the chain's injection,
///     subscribes to the hub while it is alive, and releases that subscription
///     with the chain (<see cref="IReleasable" />).  Its state lives here, never
///     on the view.
///   </para>
///   <para>
///     The watcher publishes from its own thread, so this recipient is the one
///     that hops (<see cref="MainDispatcher" />): the model that owns a bound
///     list is the only place that knows a surface exists.
///   </para>
///   <para>
///     Your seam: what the palette searches and what Enter does.  A different
///     result type is one edit away — <see cref="DialogSessionBase{TResult}" />
///     writes whatever the caller awaits.
///   </para>
/// </remarks>
public partial class WorkspacePaletteSession : DialogSessionBase<string>, IMessageRecipient, IReleasable
{
  /// <summary>How many rows the palette renders — a repository root holds more than a palette needs.</summary>
  private const int MaxMatches = 200;

  /// <summary>How many files the workspace is asked for.</summary>
  private const int MaxFiles = 20_000;

  [Inject] private partial WorkspaceFileMonitor Monitor { get; }

  [Inject] private partial IMessageHub Hub { get; }

  /// <summary>Gets or sets the heading the palette view renders.</summary>
  [ObservableProperty] public partial string? Title { get; set; }

  [Inject] private partial IWorkspaceFileOpener Opener { get; }

  [Inject] private partial INoticeService Notices { get; }

  private readonly List<string> _files = [];
  private IDisposable? _subscription;

  /// <summary>The text the rows are matched against.</summary>
  [ObservableProperty]
  public partial string Filter { get; set; } = string.Empty;

  /// <summary>The row the keyboard is on.</summary>
  [ObservableProperty]
  public partial int SelectedIndex { get; set; }

  /// <summary>The rows the filter left, newest change first.</summary>
  public ObservableCollection<string> Matches { get; } = [];

  /// <summary>The workspace root, exactly as resolved.</summary>
  public string Root => Monitor.Root;

  /// <summary>The footer's status line — the resolution rule, made visible.</summary>
  public string Watching => Lang.Workspace.FormatWatching(_files.Count, SourceLabel(Monitor.RootSource));

  /// <summary>Whether the filter left nothing — the view's empty state.</summary>
  public bool IsEmpty => Matches.Count == 0;

  /// <inheritdoc />
  public override Task OnArrivedAsync(IRoutingContext context)
  {
    Title = Lang.Workspace.Title;
    _files.AddRange(Monitor.Snapshot(MaxFiles));
    _subscription = Hub.Register(this);
    Refresh();
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  void IReleasable.Release()
  {
    _subscription?.Dispose();
    _subscription = null;
  }

  /// <inheritdoc />
  bool IMessageRecipient.CanReceive(IMessage message) => message is WorkspaceChangedMessage;

  /// <inheritdoc />
  void IMessageRecipient.Receive(IMessage message)
  {
    if (message is not WorkspaceChangedMessage changed)
      return;

    // The watcher's thread is not this list's thread — the recipient hops.
    MainDispatcher.TryPost(() => Apply(changed));
  }

  /// <summary>Moves the selection down — the view turns ↓ into this request.</summary>
  [RelayCommand]
  private void Next() => Move(+1);

  /// <summary>Moves the selection up — the view turns ↑ into this request.</summary>
  [RelayCommand]
  private void Previous() => Move(-1);

  /// <summary>
  ///   Opens the selected file — the view turns Enter into this request.  The
  ///   settled result is the file that was opened; the caller decides what that
  ///   means (the Director ignores it — a palette is a destination, not a
  ///   navigation).
  /// </summary>
  [RelayCommand]
  private async Task Open()
  {
    if (SelectedIndex < 0 || SelectedIndex >= Matches.Count)
      return;

    string relative = Matches[SelectedIndex];

    // The palette settles BEFORE the file is opened: the shell hand-off is the
    // editor's wait, not the overlay's, and a slow association (the OS picking
    // an application, then DDE/COM with it) must not hold the surface on screen.
    Close(relative);

    if (!await Opener.OpenAsync(Path.Combine(Monitor.Root, relative)))
    {
      // Fire-and-forget about the outcome, honest about the failure: the
      // palette is already gone, so the report is all that is left to give.
      Notices.Toast(Lang.Workspace.FormatOpenFailed(relative), ToastLevel.Warning);
    }
  }

  partial void OnFilterChanged(string value) => Refresh();

  private void Apply(WorkspaceChangedMessage changed)
  {
    foreach (WorkspaceChange change in changed.Changes)
    {
      // The list is ordered by recency, and the newest change is the one just
      // announced — so the entry moves to the head instead of a full resort.
      _files.Remove(change.Path);
      if (change.Kind is not WorkspaceChangeKind.Deleted)
        _files.Insert(0, change.Path);
    }

    Refresh();
    OnPropertyChanged(nameof(Watching));
  }

  private void Refresh()
  {
    IEnumerable<string> matched = _files;
    if (Filter.Length > 0)
      matched = matched.Where(path => path.Contains(Filter, StringComparison.OrdinalIgnoreCase));

    Matches.Clear();
    foreach (string path in matched.Take(MaxMatches))
      Matches.Add(path);

    SelectedIndex = Matches.Count > 0 ? 0 : -1;
    OnPropertyChanged(nameof(IsEmpty));
  }

  private void Move(int delta)
  {
    if (Matches.Count == 0)
      return;

    SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, Matches.Count - 1);
  }

  private static string SourceLabel(WorkspaceRootSource source) => source switch
  {
    WorkspaceRootSource.GitRoot => Lang.Workspace.SourceGit,
    WorkspaceRootSource.ProjectDir => Lang.Workspace.SourceProject,
    WorkspaceRootSource.Downloads => Lang.Workspace.SourceDownloads,
    _ => Lang.Workspace.SourceCurrent,
  };
}
