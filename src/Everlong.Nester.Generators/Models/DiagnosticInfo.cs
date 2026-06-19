using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Models;


/// <summary>
/// A model for a serializeable diagnostic info.
/// </summary>
/// <param name="Descriptor">The wrapped <see cref="DiagnosticDescriptor"/> instance.</param>
/// <param name="SyntaxTree">The tree to use as location for the diagnostic, if available.</param>
/// <param name="TextSpan">The span to use as location for the diagnostic.</param>
/// <param name="Arguments">The diagnostic arguments.</param>
internal sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo? Location,
    ImmutableDictionary<string, string?> Properties,
    EquatableArray<string> Arguments)
{
  // Properties is intentionally excluded from equality comparison: it is only used
  // to pass additional data to Roslyn's Diagnostic.Create() and carries no semantic
  // meaning for incremental caching decisions. Including it would break cache hits
  // because ImmutableDictionary<TKey,TValue> uses reference equality.
  public bool Equals(DiagnosticInfo? other)
    => other is not null
       && Descriptor.Equals(other.Descriptor)
       && Location == other.Location
       && Arguments.Equals(other.Arguments);

  public override int GetHashCode()
  {
    HashCode hash = default;
    hash.Add(Descriptor);
    hash.Add(Location);
    hash.Add(Arguments);
    return hash.ToHashCode();
  }

  /// <summary>
  /// Creates a new <see cref="Diagnostic"/> instance with the state from this model.
  /// </summary>
  /// <returns>A new <see cref="Diagnostic"/> instance with the state from this model.</returns>
  public Diagnostic ToDiagnostic()
  {
    if (Location is not null)
    {
      return Diagnostic.Create(Descriptor, Location.ToLocation(), Properties, Arguments.ToArray());
    }

    return Diagnostic.Create(Descriptor, null, Properties, Arguments.ToArray());
  }

  /// <summary>
  /// Creates a new <see cref="DiagnosticInfo"/> instance with the specified parameters.
  /// </summary>
  /// <param name="descriptor">The input <see cref="DiagnosticDescriptor"/> for the diagnostics to create.</param>
  /// <param name="symbol">The source <see cref="ISymbol"/> to attach the diagnostics to.</param>
  /// <param name="args">The optional arguments for the formatted message to include.</param>
  /// <returns>A new <see cref="DiagnosticInfo"/> instance with the specified parameters.</returns>
  public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
  {
    Location location = symbol.Locations.First();

    return new(descriptor, LocationInfo.CreateFrom(location), ImmutableDictionary<string, string?>.Empty, args.Select(static arg => arg.ToString()).ToImmutableArray());
  }

  /// <summary>
  /// Creates a new <see cref="DiagnosticInfo"/> instance with the specified parameters.
  /// </summary>
  /// <param name="descriptor">The input <see cref="DiagnosticDescriptor"/> for the diagnostics to create.</param>
  /// <param name="node">The source <see cref="SyntaxNode"/> to attach the diagnostics to.</param>
  /// <param name="args">The optional arguments for the formatted message to include.</param>
  /// <returns>A new <see cref="DiagnosticInfo"/> instance with the specified parameters.</returns>
  public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxNode node, params object[] args)
  {
    Location location = node.GetLocation();

    return new(descriptor, LocationInfo.CreateFrom(location), ImmutableDictionary<string, string?>.Empty, args.Select(static arg => arg.ToString()).ToImmutableArray());
  }

  public static DiagnosticInfo Create(
    DiagnosticDescriptor descriptor,
    LocationInfo? location,
    params object[] args)
    => new(descriptor,
           location,
           ImmutableDictionary<string, string?>.Empty,
           args.Select(static arg => arg.ToString()).ToImmutableArray());

  public static DiagnosticInfo Create(
    DiagnosticDescriptor descriptor,
    ISymbol symbol,
    ImmutableDictionary<string, string?> properties,
    params object[] args)
  {
    Location location = symbol.Locations.First();

    return new(
      descriptor,
      LocationInfo.CreateFrom(location),
      properties,
      args.Select(static arg => arg.ToString()).ToImmutableArray());
  }
}
