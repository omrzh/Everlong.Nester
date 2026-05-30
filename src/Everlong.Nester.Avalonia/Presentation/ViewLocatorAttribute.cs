namespace Everlong.Nester.Presentation;

/// <summary>
///   Marks a class as a view locator trigger for Nester code generation.
/// </summary>
/// <remarks>
///   Mappings are declared — <see cref="MappingAttribute{TModel,TView}" />
///   on the locator and <see cref="ViewForAttribute{TViewModel}" /> on views.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class ViewLocatorAttribute : Attribute;
