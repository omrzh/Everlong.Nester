using Everlong.DI;
using NesterApp.Backdrop;

namespace NesterApp.Services;

/// <summary>The WPF backdrop surfaces.</summary>
[Singleton<IBackdropSurfaceFactory>]
public sealed class WpfBackdropSurfaceFactory : IBackdropSurfaceFactory
{
  /// <inheritdoc />
  public object CreateField(BackdropLab lab) => new AuroraBackdrop(lab);

  /// <inheritdoc />
  public object CreatePanel(BackdropLab lab) => new BackdropDevPanel { DataContext = lab };
}
