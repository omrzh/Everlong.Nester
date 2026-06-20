using System.Collections.Immutable;
using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Views;

public abstract partial class DataTemplateGeneratorBase
{
  private static DiagnosticInfo CreateDiagnostic(
    DiagnosticDescriptor descriptor,
    LocationInfo? location,
    params object[] args)
  {
    return new DiagnosticInfo(
      descriptor,
      location,
      ImmutableDictionary<string, string?>.Empty,
      args.Select(static arg => arg.ToString()).ToImmutableArray());
  }

  private static DiagnosticInfo CreateDiagnostic(
    DiagnosticDescriptor descriptor,
    LocationInfo? location,
    ImmutableDictionary<string, string?> properties,
    params object[] args)
  {
    return new DiagnosticInfo(
      descriptor,
      location,
      properties,
      args.Select(static arg => arg.ToString()).ToImmutableArray());
  }
}
