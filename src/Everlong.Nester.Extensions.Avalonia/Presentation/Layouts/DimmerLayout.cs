using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Everlong.Nester.Dialog;
using Everlong.Nester.Intent;
using Everlong.Nester.Routing;
using Everlong.Nester.Shell;
namespace Everlong.Nester.Presentation;

/// <summary>
///   The dimmer of a dialog: hosts the layer's body, paints the scrim and
///   arbitrates dismissal.
/// </summary>
public class DimmerLayout : PContentControl, IBodyHolder, IIntentHandler, ISceneTransition, IArriving
{
  private const string BackdropKey = "Nester.Dimmer.Backdrop";

  private static readonly IBrush FallbackBackdropBrush
    = new SolidColorBrush(PColor.Parse("#40000000"));

  private readonly BodyPanel _body = new();

  /// <summary>
  ///   Initializes a new instance of the <see cref="DimmerLayout" /> class.
  /// </summary>
  public DimmerLayout()
  {
    var backdrop = new Border { IsHitTestVisible = true };
    backdrop[!Border.BackgroundProperty] = new DynamicResourceExtension(BackdropKey);
    backdrop.Tapped += OnBackdropTapped;

    var panel = new Panel();
    panel.Children.Add(backdrop);
    panel.Children.Add(_body);

    Content = panel;
    Background = Brushes.Transparent;

    // The backdrop color is theme-owned (Themes/NesterExtendedTheme.axaml). A
    // host that merges no theme leaves the dynamic resource unresolved and the
    // Border unpainted — an unpainted Border is never hit-tested, so the
    // light-dismiss tap dies silently. When the theme resource is absent,
    // paint the classic backdrop color instead.
    AttachedToVisualTree += (_, _) =>
    {
      if (!TryGetResource(BackdropKey, ThemeVariant.Default, out _))
      {
        backdrop.ClearValue(Border.BackgroundProperty);
        backdrop.Background = FallbackBackdropBrush;
      }
    };
  }

  /// <inheritdoc />
  public Task OnArrivingAsync(IRoutingContext context)
  {
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  public IBodyPanel GetBodyPanel() => _body;

  private bool _isDispatching;
  private bool _isShaking;

  private async void OnBackdropTapped(object? sender, TappedEventArgs e)
  {
    if (DataContext is not DefaultDimmerModel dimmer
        || this.GetShell() is not { } shell)
    {
      return;
    }

    // Non-light-dismiss: the tap itself is the refusal — shake, no intent.
    if (!dimmer.LightDismiss)
    {
      if (dimmer.ShakeVetoedDismissal)
      {
        await ShakeOnceAsync();
      }
      return;
    }

    if (_isDispatching)
    {
      return;
    }

    _isDispatching = true;
    try
    {
      // A light-dismiss tap translates into a BackIntent; the shake feedback
      // for a vetoed dismissal comes from the observation in HandleAsync.
      await shell.DispatchIntent(this, new BackIntent());
    }
    finally
    {
      _isDispatching = false;
    }
  }

  /// <summary>
  ///   The dimmer is the dismissal arbiter: a non-light-dismiss chain
  ///   vetoes external backs (the system back key — the backdrop's own tap
  ///   never dispatches in that mode), then observes the hosted derived router's settled
  ///   outcome for every other BackIntent — a vetoed dismissal (navigation
  ///   guard) shakes the hosted body.
  /// </summary>
  public async ValueTask HandleAsync(IntentContext context, IntentDelegate next)
  {
    if (context.Intent is BackIntent
        && !ReferenceEquals(context.Sender, this)
        && DataContext is DefaultDimmerModel { LightDismiss: false } dimmer)
    {
      context.Veto(this);
      if (dimmer.ShakeVetoedDismissal)
      {
        _ = ShakeOnceAsync();
      }

      return;
    }

    await next(context);
    if (context.Intent is BackIntent
        && context.Result == IntentResult.Vetoed
        && DataContext is DefaultDimmerModel { ShakeVetoedDismissal: true })
    {
      _ = ShakeOnceAsync();
    }
  }

  private async Task ShakeOnceAsync()
  {
    if (_isShaking)
    {
      return;
    }
    _isShaking = true;
    try
    {
      await DialogShake.ShakeAsync(_body);
    }
    catch
    {
      // Feedback must not break the intent chain.
    }
    finally
    {
      _isShaking = false;
    }
  }

  /// <inheritdoc />
  public Task AnimateEnterAsync(TransitionContext context, CancellationToken token)
    => this.PassThroughAsync(context, token);

  /// <inheritdoc />
  public Task AnimateExitAsync(TransitionContext context, CancellationToken token)
    => this.PassExitAsync(context, token);
}
