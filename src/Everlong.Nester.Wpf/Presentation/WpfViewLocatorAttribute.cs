namespace Everlong.Nester.Presentation;

/// <summary>
/// An attribute that triggers Roslyn generators to automatically create an <see cref="IViewLocator"/>
/// implementation for managing view-model mappings in WPF applications.
/// </summary>
/// <remarks>
///   Mappings are declared — <see cref="MappingAttribute{TModel,TView}" />
///   on the locator and <see cref="ViewForAttribute{TViewModel}" /> on views.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class WpfViewLocatorAttribute : Attribute
{
}
