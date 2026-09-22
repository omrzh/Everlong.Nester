using Terminal.Gui.ViewBase;

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

/// <summary>The platform layout body — <see cref="ILayoutBody{TView}" /> bound to the Terminal.Gui view type.</summary>
public interface ILayoutBody : ILayoutBody<View>;
