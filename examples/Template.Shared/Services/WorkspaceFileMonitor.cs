using System.Collections.Concurrent;
using Everlong.DI;
using Everlong.Nester.Hosting;
using Everlong.Nester.Messaging;
using Microsoft.Extensions.Logging;

namespace NesterApp.Services;

/// <summary>Which marker resolved the workspace root.</summary>
/// <remarks>
///   Read back by the palette footer, so the resolution rule is visible in the
///   running app instead of living only in this file.
/// </remarks>
public enum WorkspaceRootSource
{
  /// <summary>A <c>.git</c> directory (or file) was found while ascending.</summary>
  GitRoot,

  /// <summary>A <c>*.slnx</c> / <c>*.sln</c> / <c>*.csproj</c> was found while ascending.</summary>
  ProjectDir,

  /// <summary>No marker was found — the user's Downloads folder stands in.</summary>
  Downloads,

  /// <summary>No marker and no Downloads folder — the process's current directory stands in.</summary>
  CurrentDirectory,
}

/// <summary>How a watched file changed.</summary>
public enum WorkspaceChangeKind
{
  /// <summary>The file appeared.</summary>
  Created,

  /// <summary>An existing file's content or timestamp moved.</summary>
  Changed,

  /// <summary>The file is gone.</summary>
  Deleted,

  /// <summary>The file arrived under a new name.</summary>
  Renamed,
}

/// <summary>One coalesced change, as a path relative to the workspace root.</summary>
/// <param name="Path">The file that changed, <c>/</c>-separated.</param>
/// <param name="Kind">How it changed.</param>
public sealed record WorkspaceChange(string Path, WorkspaceChangeKind Kind);

/// <summary>The watcher's fact — a batch of workspace changes just happened.</summary>
/// <param name="Changes">This batch, already coalesced by the debounce window.</param>
public sealed record WorkspaceChangedMessage(IReadOnlyList<WorkspaceChange> Changes) : IMessage;

/// <summary>
///   The workspace domain's tenant: it watches one folder and announces what
///   changed.  The root is RESOLVED, not configured — the nearest ancestor
///   carrying a project marker wins, so a development run lands on the
///   repository while a freshly generated project still lands on something
///   alive.
/// </summary>
/// <remarks>
///   <para>
///     The shutting-down half of the contract is <see cref="IHostLifetime" />:
///     the watcher awaits the host's <c>Stopping</c> token and releases the OS
///     handle itself, so the domain never reaches for the shell or the app
///     lifetime — the same service compiles into every platform and stays
///     usable outside a shell's container.
///   </para>
///   <para>
///     Facts leave through <see cref="IMessageHub" />; the watcher publishes on
///     its own thread, and a recipient that touches a surface hops itself
///     (<c>docs/design/messaging.md</c>).
///   </para>
///   <para>
///     Your seam: where the workspace is, what is noise (the ignore list) and
///     how loud a burst is (the debounce window).  <see cref="Start" /> is
///     driven by the Director — desktop windows only, once the shell is
///     assembled.
///   </para>
/// </remarks>
[Singleton]
public sealed class WorkspaceFileMonitor
{
  /// <summary>How far up from the current directory a marker is looked for.</summary>
  private const int MaxAscend = 8;

  /// <summary>The quiet window a burst is folded into before it is announced.</summary>
  private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(250);

  /// <summary>The ceiling on that window — a sustained writer would otherwise reset it forever.</summary>
  private static readonly TimeSpan MaxBatchDelay = TimeSpan.FromSeconds(2);

  /// <summary>Directory segments that are never part of the workspace's meaning.</summary>
  private static readonly string[] IgnoredSegments =
    [".git", "bin", "obj", "node_modules", ".vs", ".idea", "nester-topology"];

  /// <summary>What a project root looks like when it was never initialized as a repository.</summary>
  private static readonly string[] ProjectMarkers = ["*.slnx", "*.sln", "*.csproj"];

  private readonly IHostLifetime _lifetime;
  private readonly IMessageHub _hub;
  private readonly ILogger<WorkspaceFileMonitor> _logger;

  /// <summary>The batch being folded — keyed by path, so a burst on one file stays one entry.</summary>
  private readonly ConcurrentDictionary<string, WorkspaceChange> _pending = new(StringComparer.OrdinalIgnoreCase);

  private readonly object _gate = new();
  private FileSystemWatcher? _watcher;
  private Timer? _debounce;
  private DateTime _firstPendingUtc;
  private int _started;

  /// <summary>Creates the watcher over the host's lifetime signals.</summary>
  public WorkspaceFileMonitor(IHostLifetime lifetime, IMessageHub hub, ILogger<WorkspaceFileMonitor> logger)
  {
    _lifetime = lifetime;
    _hub = hub;
    _logger = logger;
  }

  /// <summary>The absolute workspace root currently observed.</summary>
  public string Root { get; private set; } = string.Empty;

  /// <summary>Which rule resolved <see cref="Root" />.</summary>
  public WorkspaceRootSource RootSource { get; private set; }

  /// <summary>Whether the OS handle is currently held — the domain's on/off fact.</summary>
  public bool IsWatching => _watcher is not null;

  /// <summary>
  ///   Starts watching — idempotent.  The first call resolves the root, takes
  ///   the OS handle and observes the host's stop signal.
  /// </summary>
  public void Start()
  {
    if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
      return;

    (Root, RootSource) = ResolveRoot();
    _logger.LogInformation("Workspace root resolved: {Root} ({Source})", Root, RootSource);

    _debounce = new Timer(OnDebounceElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    Attach();
    _ = ObserveStoppingAsync();
  }

  /// <summary>
  ///   The workspace's files, relative and <c>/</c>-separated, newest first —
  ///   the list a palette opens with (the watcher only keeps it current).
  /// </summary>
  /// <param name="max">The cap on how many files are returned.</param>
  public IReadOnlyList<string> Snapshot(int max = 20_000)
  {
    if (Root.Length == 0)
      return [];

    try
    {
      return
      [
        .. Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories)
                    .Where(path => !IsIgnored(Relative(path)))
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .Take(max)
                    .Select(Relative)
      ];
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      // A tree being written to while it is walked is normal, not fatal.
      _logger.LogWarning(e, "Could not enumerate the workspace root {Root}.", Root);
      return [];
    }
  }

  // ── The host's stop signal — the domain's only shutdown path ───────────────

  private async Task ObserveStoppingAsync()
  {
    try
    {
      await Task.Delay(Timeout.Infinite, _lifetime.Stopping);
    }
    catch (OperationCanceledException)
    {
      // The stop signal — the one way out of this wait.
    }

    Stop();
  }

  /// <summary>Releases the OS handle and drops the batch that was not announced.</summary>
  private void Stop()
  {
    lock (_gate)
    {
      _debounce?.Dispose();
      _debounce = null;
      _pending.Clear();
    }

    Detach();
    _logger.LogInformation("Workspace watcher stopped: {Root}", Root);
  }

  // ── Watching ──────────────────────────────────────────────────────────────

  private void Attach()
  {
    if (!Directory.Exists(Root))
      return;

    try
    {
      var watcher = new FileSystemWatcher(Root)
      {
        IncludeSubdirectories = true,
        // A repository root overflows the default buffer easily; the Error
        // event below is what a smaller tree asks for.
        InternalBufferSize = 64 * 1024,
        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
      };

      watcher.Created += OnChanged;
      watcher.Changed += OnChanged;
      watcher.Deleted += OnChanged;
      watcher.Renamed += OnRenamed;
      watcher.Error += OnWatcherError;
      watcher.EnableRaisingEvents = true;

      _watcher = watcher;
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
    {
      _logger.LogWarning(e, "Could not watch the workspace root {Root}.", Root);
    }
  }

  private void Detach()
  {
    var watcher = Interlocked.Exchange(ref _watcher, null);
    if (watcher is null)
      return;

    try
    {
      watcher.EnableRaisingEvents = false;
      watcher.Dispose();
    }
    catch (Exception e)
    {
      _logger.LogWarning(e, "Disposing the workspace watcher failed.");
    }
  }

  private void OnChanged(object sender, FileSystemEventArgs e)
    => Queue(e.FullPath,
             e.ChangeType switch
             {
               WatcherChangeTypes.Created => WorkspaceChangeKind.Created,
               WatcherChangeTypes.Deleted => WorkspaceChangeKind.Deleted,
               _ => WorkspaceChangeKind.Changed,
             });

  private void OnRenamed(object sender, RenamedEventArgs e)
  {
    // Two facts, not one: the old name is gone and a new one arrived — a list
    // can move the entry instead of guessing which half happened.
    Queue(e.OldFullPath, WorkspaceChangeKind.Deleted);
    Queue(e.FullPath, WorkspaceChangeKind.Renamed);
  }

  private void OnWatcherError(object sender, ErrorEventArgs e)
  {
    // An overflow loses events, not the watcher: reattach from scratch, but
    // only if the host is still alive (a teardown eats the last events anyway).
    _logger.LogWarning(e.GetException(), "The workspace watcher lost events; reattaching.");
    _ = Task.Run(async () =>
    {
      await Task.Delay(Debounce);
      if (_lifetime.Stopping.IsCancellationRequested)
        return;

      Detach();
      Attach();
    });
  }

  // ── The debounce window ───────────────────────────────────────────────────

  private void Queue(string fullPath, WorkspaceChangeKind kind)
  {
    if (Root.Length == 0)
      return;

    string relative = Relative(fullPath);
    if (IsIgnored(relative))
      return;

    _pending[relative] = new WorkspaceChange(relative, kind);

    lock (_gate)
    {
      var now = DateTime.UtcNow;
      if (_firstPendingUtc == default)
        _firstPendingUtc = now;

      var delay = now - _firstPendingUtc > MaxBatchDelay ? TimeSpan.Zero : Debounce;
      _debounce?.Change(delay, Timeout.InfiniteTimeSpan);
    }
  }

  private void OnDebounceElapsed(object? state)
  {
    if (_lifetime.Stopping.IsCancellationRequested)
      return;

    WorkspaceChange[] batch;
    lock (_gate)
    {
      _firstPendingUtc = default;
      if (_pending.IsEmpty)
        return;

      batch = [.. _pending.Values.OrderBy(change => change.Path, StringComparer.OrdinalIgnoreCase)];
      _pending.Clear();
    }

    try
    {
      // Published from the watcher's thread on purpose — the hub broadcasts on
      // the publisher's thread and says nothing about the UI thread.
      _hub.Publish(new WorkspaceChangedMessage(batch));
    }
    catch (Exception e)
    {
      _logger.LogWarning(e, "A workspace change recipient failed.");
    }
  }

  // ── Resolving the root ────────────────────────────────────────────────────

  private static (string Root, WorkspaceRootSource Source) ResolveRoot()
  {
    var start = new DirectoryInfo(Environment.CurrentDirectory);

    // Two ascents, not one: a `bin/Debug/net10.0` run meets a project file
    // first, and the repository above it is the truer workspace.
    if (Ascend(start, IsRepositoryRoot) is { } repository)
      return (repository.FullName, WorkspaceRootSource.GitRoot);

    if (Ascend(start, IsProjectDirectory) is { } project)
      return (project.FullName, WorkspaceRootSource.ProjectDir);

    // Nothing above looks like a project (a published app started anywhere):
    // a folder that is guaranteed to exist and to keep changing beats a tree
    // that renders empty.
    string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    return Directory.Exists(downloads)
             ? (downloads, WorkspaceRootSource.Downloads)
             : (start.FullName, WorkspaceRootSource.CurrentDirectory);
  }

  private static DirectoryInfo? Ascend(DirectoryInfo start, Func<DirectoryInfo, bool> matches)
  {
    var depth = 0;
    for (var dir = start; dir is not null && depth < MaxAscend; dir = dir.Parent, depth++)
    {
      if (matches(dir))
        return dir;
    }

    return null;
  }

  private static bool IsRepositoryRoot(DirectoryInfo dir)
  {
    string marker = Path.Combine(dir.FullName, ".git");

    // A linked worktree or a submodule keeps `.git` as a FILE — both shapes
    // mark a root.
    return Directory.Exists(marker) || File.Exists(marker);
  }

  private static bool IsProjectDirectory(DirectoryInfo dir)
  {
    try
    {
      return ProjectMarkers.Any(pattern => dir.EnumerateFiles(pattern).Any());
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
    {
      return false;
    }
  }

  // ── Paths the workspace does not mean ─────────────────────────────────────

  private string Relative(string fullPath)
    => Path.GetRelativePath(Root, fullPath).Replace(Path.DirectorySeparatorChar, '/');

  private static bool IsIgnored(string relativePath)
  {
    foreach (string segment in relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries))
    {
      if (IgnoredSegments.Contains(segment, StringComparer.OrdinalIgnoreCase))
        return true;
    }

    return false;
  }
}
