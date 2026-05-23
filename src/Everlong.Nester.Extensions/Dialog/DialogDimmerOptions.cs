namespace Everlong.Nester.Dialog;

/// <summary>
///   The dimmer state a dialog's options carry.
/// </summary>
public interface IDialogDimmerOptions
{
  /// <summary>Whether the dialog refuses dismissal from outside its own content.</summary>
  bool IsMandatory { get; }

  /// <summary>Whether a refused dismissal attempt shakes the dialog.</summary>
  bool ShakeVetoedDismissal { get; }
}

/// <summary>
///   The dimmer half of a dialog's options — declared once, so an options type
///   carries only what it adds.
/// </summary>
public abstract class DialogOptions : IDialogDimmerOptions
{
  /// <inheritdoc />
  public bool IsMandatory { get; init; }

  /// <inheritdoc />
  public bool ShakeVetoedDismissal { get; init; } = true;
}

public static partial class DialogSessionExtensions
{
  extension(IDialogDimmerOptions options)
  {
    /// <summary>
    ///   Derives the dimmer the options' dismissal state calls for — the state
    ///   `IsMandatory` inverts into `LightDismiss` here, and nowhere else.
    /// </summary>
    public DefaultDimmerModel ToDimmer()
      => new()
      {
        LightDismiss = !options.IsMandatory,
        ShakeVetoedDismissal = options.ShakeVetoedDismissal
      };
  }
}
