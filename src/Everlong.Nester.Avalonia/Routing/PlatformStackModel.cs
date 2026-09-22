using Everlong.Nester.Presentation;
// NOTE: Single-source file — the WPF project compiles this exact file via
// <Compile Include> in Everlong.Nester.Wpf.csproj.  Edit it here only;
// never create a WPF-side copy (the two builds would drift).

namespace Everlong.Nester.Routing;

/// <summary>The platform's navigation model.</summary>
internal sealed class PlatformStackModel : RouterStack
{
  /// <inheritdoc />
  protected override IRoutingView View { get; } = new RoutingView();

  /// <inheritdoc />
  protected override Location CreateLocation(Type type, object instance, IArgs? args)
    => new PlatformLocation(type, args, instance);
}

/// <summary>
///   The platform's realized node — the platform control behind the base
///   visual slot and the mount child of its parent's layout body.
/// </summary>
internal sealed class PlatformLocation(Type type, IArgs? args, object instance)
  : Location(type, args, instance), IViewLocation<PControl>
{
  /// <summary>The assembled platform control — the strongly-typed face of the base visual slot, or <see langword="null"/> before assembly.</summary>
  public PControl? View { get => Presenter as PControl; set => Presenter = value; }

  /// <summary>The mount point inside this node's view — where the inner node's view mounts.</summary>
  public ILayoutBody<PControl>? Body { get; set; }

  /// <summary>Whether <see cref="Body" /> has been resolved — a settled "no body" answer is held too.</summary>
  internal bool BodyResolved { get; set; }
}
