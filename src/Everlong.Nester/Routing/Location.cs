using System.ComponentModel;

namespace Everlong.Nester.Routing;

/// <summary>
///   A realized chain node — a participant type, the arguments it serves,
///   its resolved instance, and the visual the platform assembled.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class Location(Type type, IArgs? args, object instance, object? presenter = null) : ILocation
{
  /// <summary>The participant type.</summary>
  public Type Type { get; } = type;

  /// <summary>The arguments the node serves — a parameterized node's identity, read back after delivery; <see langword="null" /> when the node serves none.</summary>
  public IArgs? Args { get; set; } = args;

  /// <summary>The resolved participant instance.</summary>
  public object Instance { get; } = instance;

  /// <summary>The platform-assembled view, filled by the platform's assembly stage.</summary>
  public object? Presenter { get; set; } = presenter;

  /// <summary>The child nodes of the container tree, keyed by participant type.</summary>
  public Dictionary<Type, List<Location>> Children { get; } = [];

  /// <summary>The parent node in the container tree, or <see langword="null" /> at the root.</summary>
  public Location? Parent { get; internal set; }

  /// <inheritdoc />
  ILocation? ILocation.Parent => Parent;

  /// <inheritdoc />
  IReadOnlyList<ILocation> ILocation.Trail => field ??= Ancestors();

  /// <summary>The path to this node, outermost first, this node last.</summary>
  internal List<Location> Ancestors()
  {
    List<Location> path = [];
    for (Location? node = this; node is not null; node = node.Parent)
      path.Add(node);
    path.Reverse();
    return path;
  }
}
