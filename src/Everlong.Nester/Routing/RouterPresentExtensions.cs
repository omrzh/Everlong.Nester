namespace Everlong.Nester.Routing;

/// <summary>
///   Generic presentation extensions over <see cref="IRouter" /> — route a
///   model chain into a derived router and await its settled result.
/// </summary>
public static class RouterPresentExtensions
{
  /// <summary>
  ///   Derives an overlay from the given parameters and routes the given model
  ///   chain into it, awaiting the chain's result — a dismissal yields
  ///   <see langword="null" />.
  /// </summary>
  /// <param name="router">The router to present through.</param>
  /// <param name="options">The derived router's creation parameters — its band, its position and its default layouts.</param>
  /// <param name="models">The chain's own participants, outermost first; the options' parents are completed in front of them.</param>
  /// <returns>The chain's settled result, or <see langword="null" /> on dismissal.</returns>
  public static async Task<object?> PresentOnDerivedAsync(this IRouter router, DeriveOptions options, params object[] models)
  {
    IRouter derived = router.Derive(options);
    if (derived is not { Role: RouterRole.Derived, Completion.Result.IsCompleted: false })
    {
      throw new InvalidOperationException("The derived router is invalid.");
    }

    await derived.RouteAsync(new Locator(models));
    return await derived.Completion;
  }
}
