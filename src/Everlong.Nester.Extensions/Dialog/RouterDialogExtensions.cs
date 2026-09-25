using Everlong.Nester.Layer;
using Everlong.Nester.Routing;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Extension methods that present a model through a router.
/// </summary>
public static partial class RouterDialogExtensions
{
  /// <summary>
  ///   Presents the model on a derived router under a default dimmer, awaiting
  ///   its result — a dismissal yields <see langword="null" />.
  /// </summary>
  public static async Task<object?> ShowAsync(this IRouter router, object model)
    => await router.ShowAsync(model, new DefaultDimmerModel());

  /// <summary>
  ///   Presents the model on a derived router under the given dimmer,
  ///   awaiting its result — a dismissal yields <see langword="null" />.
  /// </summary>
  public static async Task<object?> ShowAsync(this IRouter router, object model, DefaultDimmerModel dimmer)
    => await router.PresentOnDerivedAsync(
         new DeriveOptions { Band = KnownLayers.Dialog, Parents = [dimmer] }, model);

  /// <summary>
  ///   Presents a model and awaits its result, returning it cast
  ///   to <typeparamref name="TResult" />.
  /// </summary>
  public static async Task<TResult?> ShowAsync<TResult>(this IRouter router, object model)
    => await router.ShowAsync(model) is TResult typedResult ? typedResult : default;

  /// <summary>
  ///   Presents a model under the given dimmer and awaits its result,
  ///   returning it cast to <typeparamref name="TResult" />.
  /// </summary>
  public static async Task<TResult?> ShowAsync<TResult>(this IRouter router, object model, DefaultDimmerModel dimmer)
    => await router.ShowAsync(model, dimmer) is TResult typedResult ? typedResult : default;
}
