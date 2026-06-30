using Everlong.DI;
using NesterApp.Backdrop;

namespace NesterApp.Services;

/// <summary>The Avalonia backdrop surfaces.</summary>
[Singleton<IBackdropSurfaceFactory>]
public sealed class AvaloniaBackdropSurfaceFactory : IBackdropSurfaceFactory
{
  /// <inheritdoc />
  public object CreateField(BackdropLab lab) => new AuroraBackdrop(lab);

  /// <inheritdoc />
  public object CreatePanel(BackdropLab lab) => new BackdropDevPanel { DataContext = lab };
}
