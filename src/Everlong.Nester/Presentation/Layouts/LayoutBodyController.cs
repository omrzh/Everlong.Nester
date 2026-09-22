namespace Everlong.Nester.Presentation;

/// <summary>
///   Tracks the set of children registered in a panel-based layout body and
///   computes the minimal set of add / remove / show / hide operations
///   required for each composition step.  Has zero dependency on any UI
///   framework; the platform-specific panel mutations are performed by the
///   panel after inspecting the returned <see cref="LayoutBodyChangeSet{T}"/>.
/// </summary>
/// <typeparam name="T">The platform control type.</typeparam>
public sealed class LayoutBodyController<T> where T : class
{
  private readonly List<T> _children = [];

  /// <summary>All views currently registered in the panel (visible or hidden).</summary>
  public IReadOnlyList<T> Children => _children;

  /// <summary>The view currently shown to the user, or <see langword="null"/> before any Switch.</summary>
  public T? Active { get; private set; }

  /// <summary>
  ///   Makes <paramref name="target"/> the active child in this panel.
  ///   Only updates the <see cref="Active"/> pointer; visible/hidden state is managed
  ///   externally, keeping both arriving and departing pages visible during the
  ///   transition animation.  If <paramref name="target"/> is not yet in the panel
  ///   it is added first.
  /// </summary>
  /// <param name="target">The child to mark as active. Must not be <see langword="null"/>.</param>
  public void Switch(T target)
  {
    ArgumentNullException.ThrowIfNull(target);

    if (ReferenceEquals(target, Active))
      return;

    EnsureAdded(target);
    Active = target;
  }

  /// <summary>
  ///   Ensures <paramref name="child" /> is added to the panel. Safe to call
  ///   if the child is already present (idempotent).
  /// </summary>
  public LayoutBodyChangeSet<T> EnsureAdded(T child)
  {
    ArgumentNullException.ThrowIfNull(child);

    if (HasChild(child))
    {
      return LayoutBodyChangeSet<T>.CreateEmpty();
    }

    _children.Add(child);
    return new LayoutBodyChangeSet<T>(
      ToAdd: [child],
      ToRemove: [],
      ToShow: [],
      ToHide: [],
      IsRefresh: false);
  }

  /// <summary>
  ///   Removes <paramref name="child"/> from the panel when its stack entry is released.
  ///   Safe to call if the view is not in the panel (idempotent).
  ///   If <paramref name="child"/> is currently active, the active reference is cleared before removal.
  /// </summary>
  public LayoutBodyChangeSet<T> Release(T child)
  {
    ArgumentNullException.ThrowIfNull(child);

    if (ReferenceEquals(child, Active))
    {
      Active = null;
    }

    if (!RemoveChild(child))
    {
      return LayoutBodyChangeSet<T>.CreateEmpty();
    }

    return new LayoutBodyChangeSet<T>(
      ToAdd: [],
      ToRemove: [child],
      ToShow: [],
      ToHide: [],
      IsRefresh: false);
  }

  private bool HasChild(T child)
  {
    foreach (T c in _children)
    {
      if (ReferenceEquals(c, child))
        return true;
    }

    return false;
  }

  private bool RemoveChild(T child)
  {
    for (int i = 0; i < _children.Count; i++)
    {
      if (ReferenceEquals(_children[i], child))
      {
        _children.RemoveAt(i);
        return true;
      }
    }

    return false;
  }
}

/// <summary>
///   Describes what the panel must do to apply a single <see cref="LayoutBodyController{T}"/>
///   operation. All lists are snapshots — they are unaffected by subsequent controller operations.
/// </summary>
public readonly record struct LayoutBodyChangeSet<T>(
  IReadOnlyList<T> ToAdd,
  IReadOnlyList<T> ToRemove,
  IReadOnlyList<T> ToShow,
  IReadOnlyList<T> ToHide,
  bool IsRefresh)
{
  /// <summary>Creates a refresh changeset (all lists empty, IsRefresh = true).</summary>
  public static LayoutBodyChangeSet<T> CreateRefresh()
    => new([], [], [], [], IsRefresh: true);

  /// <summary>Creates an empty no-op changeset (all lists empty, IsRefresh = false).</summary>
  public static LayoutBodyChangeSet<T> CreateEmpty()
    => new([], [], [], [], IsRefresh: false);
}
