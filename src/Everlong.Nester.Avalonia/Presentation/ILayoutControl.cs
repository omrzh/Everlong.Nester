// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).
namespace Everlong.Nester.Presentation;


/// <summary>
///   Interface for controls that can host other controls.
/// </summary>
public interface ILayoutControl
{
  /// <summary>
  ///   Gets the <see cref="ILayoutBody"/> hosting the inner content.
  /// </summary>
  /// <returns>The <see cref="ILayoutBody"/> declared in the layout's visual tree.</returns>
  ILayoutBody GetLayoutBody();
}

/// <summary>The platform layout body — <see cref="ILayoutBody{TView}" /> bound to the platform control type.</summary>
public interface ILayoutBody : ILayoutBody<PlatformControl>;
