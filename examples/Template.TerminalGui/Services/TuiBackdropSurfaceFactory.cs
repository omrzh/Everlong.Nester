using Everlong.DI;
using NesterApp.Backdrop;

namespace NesterApp.Services;

/// <summary>
///   The Terminal.Gui surface has no backdrop — the domain is registered (the
///   shared shell resolves it) but never mounted, so these throw if reached.
/// </summary>
[Singleton<IBackdropSurfaceFactory>]
internal sealed class TuiBackdropSurfaceFactory : IBackdropSurfaceFactory
{
  /// <inheritdoc />
  public object CreateField(BackdropLab lab)
    => throw new NotSupportedException("The terminal surface has no backdrop field.");

  /// <inheritdoc />
  public object CreatePanel(BackdropLab lab)
    => throw new NotSupportedException("The terminal surface has no backdrop panel.");
}
