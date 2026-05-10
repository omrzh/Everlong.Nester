using System.Runtime.CompilerServices;

namespace Everlong.Nester.Routing;

/// <summary>
///   The default argument carrier — record value equality, with an empty
///   value.
/// </summary>
public record Args : IArgs
{
  /// <summary>Represents an empty set of arguments.</summary>
  public static readonly Args Empty = new();
}

/// <summary>
///   A default <see cref="ITarget" /> implementation — an immutable target descriptor.
/// </summary>
public sealed record Target : ITarget
{
  /// <summary>Builds a target descriptor over the given type, arguments, and instance.</summary>
  public Target(Type type, IArgs? args = null, object? instance = null)
  {
    Type = type;
    Args = args;
    Instance = instance;
  }

  /// <inheritdoc />
  public Type Type { get; }

  /// <inheritdoc />
  public IArgs? Args { get; }

  /// <inheritdoc />
  public object? Instance { get; }

  /// <summary>Builds a target descriptor over the given type, arguments, and instance.</summary>
  public static Target Of(Type type, IArgs? args = null, object? instance = null) => new(type, args, instance);

  /// <summary>Builds a bare target descriptor over the given type.</summary>
  public static Target Of<T>() where T : IRoutable => new(typeof(T));

  /// <summary>Builds a parameterized target descriptor over the given type.</summary>
  public static Target Of<T>(IArgs args) where T : IParameterized, IRoutable => new(typeof(T), args);

  /// <summary>Converts a participant type to a target descriptor over it.</summary>
  public static implicit operator Target(Type type) => new(type);
}

/// <summary>
///   A concrete location descriptor — an immutable target chain,
///   outermost first, the content target last.  Never empty.
/// </summary>
[CollectionBuilder(typeof(LocatorBuilder), nameof(LocatorBuilder.Create))]
public class Locator : ILocator
{
  private readonly IReadOnlyList<ITarget> targets;

  /// <summary>Builds a descriptor over the given target chain.</summary>
  /// <param name="targets">The participant chain, outermost first.</param>
  public Locator(IReadOnlyList<ITarget> targets)
  {
    this.targets = targets;
  }

  /// <summary>Builds a single-target descriptor.</summary>
  /// <param name="target">The content target type.</param>
  /// <param name="args">The target's arguments, or <see langword="null" /> when none.</param>
  public Locator(Type target, IArgs? args = null)
  {
    this.targets = [Target.Of(target, args)];
  }

  /// <summary>Builds a descriptor over the given participant types, target descriptors, and participant instances.</summary>
  /// <param name="instances">The participant types, target descriptors, and participant instances.</param>
  public Locator(IEnumerable<object> instances)
  {
    var list = new List<ITarget>();
    foreach (var item in instances)
    {
      if (item is Type type)
      {
        list.Add(Target.Of(type));
      }
      else if (item is ITarget target)
      {
        // A foreign implementation is kept as declared — the runtime reads
        // only Type, Args and Instance, and never discriminates the type.
        list.Add(target);
      }
      else
      {
        list.Add(new Target(item.GetType(), null, item));
      }
    }
    targets = [.. list];
  }

  /// <inheritdoc />
  public IReadOnlyList<ITarget> Path => targets;

  /// <summary>Returns an enumerator over the target chain, outermost first.</summary>
  public IEnumerator<ITarget> GetEnumerator() => targets.GetEnumerator();
}

/// <summary>
///   The collection-expression entry for <see cref="Locator" /> — builds a
///   locator from a target list.
/// </summary>
public static class LocatorBuilder
{
  /// <summary>Builds a locator over the given targets.</summary>
  public static Locator Create(ReadOnlySpan<ITarget> targets) => new([.. targets]);
}
