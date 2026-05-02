namespace Everlong.Nester.Presentation;

/// <summary>A layout body child — the view it mounts into the body.</summary>
public interface IViewLocation<TView>
{
  /// <summary>The view this child mounts into its hosting body, or <see langword="null"/> before it is assembled.</summary>
  TView? View { get; set; }
}

/// <summary>The visual container of a chain segment: owns a child list, an active child, and visibility.</summary>
public interface ILayoutBody<TView>
{
  /// <summary>The child nodes, in mount order.</summary>
  IReadOnlyList<IViewLocation<TView>> Children { get; }

  /// <summary>The currently active child, or <see langword="null"/> when none is active.</summary>
  IViewLocation<TView>? ActiveChild { get; }

  /// <summary>Whether this body is currently attached to the visual tree.</summary>
  bool IsAttachedToVisualTree { get; }

  /// <summary>Marks the active child of this body.</summary>
  void SetActiveChild(IViewLocation<TView> node);

  /// <summary>Adds a child node to this body.</summary>
  void Add(IViewLocation<TView> node);

  /// <summary>Removes a child node from this body.</summary>
  void Remove(IViewLocation<TView> node);

  /// <summary>Sets the body's visibility.</summary>
  void SetVisible(bool isVisible);

  /// <summary>Hides every child except the active one.</summary>
  void SettleActive();
}
