using Everlong.Nester.Generators.Models;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Helpers;

/// <summary>Reads the declaration shape a generated half has to restate for a declared type.</summary>
internal static class TypeShapeHelper
{
  /// <summary>The type's own shape — name, kind, record/static-ness and type parameter names.</summary>
  public static TypeShapeInfo ShapeOf(INamedTypeSymbol symbol)
    => new(symbol.Name,
           symbol.TypeKind,
           symbol.IsRecord,
           symbol.IsStatic,
           symbol.TypeParameters.Select(static parameter => parameter.Name).ToEquatableArray());

  /// <summary>Every type enclosing the symbol, innermost first — the order the halves are wrapped in.</summary>
  public static EquatableArray<TypeShapeInfo> GetContainingTypes(INamedTypeSymbol symbol)
  {
    var results = new List<TypeShapeInfo>();
    var current = symbol.ContainingType;
    while (current != null)
    {
      results.Add(ShapeOf(current));
      current = current.ContainingType;
    }

    return results.ToEquatableArray();
  }
}
