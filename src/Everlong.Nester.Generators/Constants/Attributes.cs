namespace Everlong.Nester.Generators.Constants;

internal static class Attributes
{
  // Attributes
  internal const string Category = Ns.Nester;

  internal const string Routable = "RoutableAttribute";
  internal const string RoutableFull = $"{Ns.NesterRouting}.{Routable}";
  internal const string RoutableGeneric = "RoutableAttribute`1";
  internal const string RoutableGenericFull = $"{Ns.NesterRouting}.{RoutableGeneric}";
  internal const string Layout1Prefix = "LayoutAttribute";
  internal const string Layout1Full = $"{Ns.NesterRouting}.{Layout1Prefix}`1";

  internal const string ViewFor = "ViewForAttribute`1";
  internal const string ViewForFull = $"{Ns.NesterPresentation}.{ViewFor}";

  internal const string InjectFull = $"{Ns.NesterDi}.InjectAttribute";
  internal const string EditorBrowsable = "global::System.ComponentModel.EditorBrowsable";

  internal const string ViewLocator = "ViewLocatorAttribute";
  internal const string ViewLocatorFull = $"{Ns.NesterPresentation}.{ViewLocator}";

  internal const string WpfViewLocator = "WpfViewLocatorAttribute";
  internal const string WpfViewLocatorFull = $"{Ns.NesterPresentation}.{WpfViewLocator}";

  internal const string Mapping = "MappingAttribute";

  internal const string SingletonFull = $"{Ns.NesterDi}.SingletonAttribute";
  internal const string SingletonGenericFull = $"{Ns.NesterDi}.SingletonAttribute`1";
  internal const string TransientFull = $"{Ns.NesterDi}.TransientAttribute";
  internal const string TransientGenericFull = $"{Ns.NesterDi}.TransientAttribute`1";
  internal const string ScopedFull = $"{Ns.NesterDi}.ScopedAttribute";
  internal const string ScopedGenericFull = $"{Ns.NesterDi}.ScopedAttribute`1";

  internal const string ServiceRegistrar = "ServiceRegistrarAttribute";
  internal const string ServiceRegistrarFull = $"{Ns.NesterDi}.{ServiceRegistrar}";

}
