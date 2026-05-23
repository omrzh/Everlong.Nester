using Everlong.Nester.Routing;

namespace Everlong.Nester.Dialog;

/// <summary>
///   The default dimmer — a backdrop with light dismissal controlled by
///   <see cref="LightDismiss" />.
/// </summary>
public sealed class DefaultDimmerModel : ITarget
{
  /// <summary>Whether clicking the backdrop dismisses.</summary>
  public bool LightDismiss { get; init; } = true;

  /// <summary>Whether a refused dismissal attempt shakes.</summary>
  public bool ShakeVetoedDismissal { get; init; } = true;

  /// <inheritdoc />
  Type ITarget.Type => typeof(DefaultDimmerModel);

  /// <inheritdoc />
  IArgs? ITarget.Args => null;

  /// <inheritdoc />
  object? ITarget.Instance => this;
}
