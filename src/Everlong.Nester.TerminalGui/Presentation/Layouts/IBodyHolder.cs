using Terminal.Gui.ViewBase;

namespace Everlong.Nester.Presentation;

/// <summary>
///   A control that declares the mount point a chain node below it mounts into.
/// </summary>
/// <remarks>
///   The framework asks only this contract: a view that can be a non-terminal
///   chain node implements it, and the panel it returns stays in the control's
///   visual tree while the control is presented.
/// </remarks>
public interface IBodyHolder
{
  /// <summary>
  ///   Gets the <see cref="IBodyPanel"/> the inner content mounts into.
  /// </summary>
  /// <returns>The mount point this control exposes.</returns>
  IBodyPanel GetBodyPanel();
}

/// <summary>The platform body panel — <see cref="IBodyPanel{TView}" /> bound to the Terminal.Gui view type.</summary>
public interface IBodyPanel : IBodyPanel<View>;
