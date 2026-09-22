namespace Everlong.Nester.Presentation;

/// <summary>
///   A <see cref="PContentControl"/> hosting a tenant's body — the stage's
///   per-layer surface; the stage applies each layer's keyboard-focus mode
///   to this container.
/// </summary>
public sealed class ContentLayer : PContentControl
{
  /// <summary>Raised when the layer's content is replaced — the stage recomputes the layers' tab reachability.</summary>
  internal event Action? ContentReplaced;

  /// <inheritdoc />
  protected override void OnContentChanged(object oldContent, object newContent)
  {
    base.OnContentChanged(oldContent, newContent);
    ContentReplaced?.Invoke();
  }
}
