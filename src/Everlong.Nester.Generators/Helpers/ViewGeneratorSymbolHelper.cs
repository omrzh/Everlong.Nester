using Everlong.Nester.Generators.Constants;
using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Helpers;

internal static class ViewGeneratorSymbolHelper
{
  public static bool IsMappingNamespace(INamespaceSymbol namespaceSymbol)
  {
    return namespaceSymbol.ToDisplayString() == Ns.NesterPresentation;
  }

  /// <summary>True when the type declares itself the view of some view model with <c>[ViewFor&lt;T&gt;]</c>.</summary>
  public static bool HasViewFor(INamedTypeSymbol type)
  {
    foreach (var attribute in type.GetAttributes())
    {
      if (attribute.AttributeClass?.OriginalDefinition is { } original
          && $"{original.ContainingNamespace.ToDisplayString()}.{original.MetadataName}" == Attributes.ViewForFull)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Why generated code cannot instantiate <paramref name="view" />, or <see langword="null" /> when it can.
  /// The locator names the view type, so an unbound type parameter anywhere in its enclosing chain, a
  /// static class, and anything narrower than <c>internal</c> are all out of reach.
  /// </summary>
  public static string? CannotInstantiateReason(INamedTypeSymbol view)
  {
    if (view.IsStatic)
      return "it is static";

    // A nested type reports IsGenericType for its enclosing type's parameters too, so its own arity
    // is what separates "generic" from "nested in a generic type".
    if (view.Arity > 0)
      return "it is generic";

    for (INamedTypeSymbol? type = view; type is not null; type = type.ContainingType)
    {
      if (type.IsGenericType)
        return "it is nested in a generic type";

      if (type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
      {
        return ReferenceEquals(type, view)
                 ? "it is not accessible to the generated locator"
                 : "it is nested in a type that is not accessible to the generated locator";
      }
    }

    return null;
  }
}
