namespace Everlong.Nester.Notice;

/// <summary>
///   Represents an object that is aware of pointer interactions (enter/leave).
/// </summary>
public interface IPointerAware
{
  /// <summary> Called when the pointer enters the view. </summary>
  void OnPointerEnter();

  /// <summary>
  ///   Called when the pointer leaves the view.
  /// </summary>
  void OnPointerLeave();
}
