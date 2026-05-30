// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Presentation;

/// <summary>
///   Marks the view type that maps to a ViewModel type — the explicit
///   view-to-viewmodel declaration for view-locator code generation.
/// </summary>
/// <typeparam name="TModel">The ViewModel type.</typeparam>
/// <typeparam name="TView">The view type that presents the ViewModel.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class MappingAttribute<TModel, TView> : Attribute where TView : class;

/// <summary>
///   Declares the view model a view presents.
/// </summary>
/// <typeparam name="TViewModel">The ViewModel type this view presents.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ViewForAttribute<TViewModel> : Attribute where TViewModel : notnull;
