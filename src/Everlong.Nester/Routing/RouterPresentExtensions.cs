namespace Everlong.Nester.Routing;

/// <summary>
///   Generic presentation extensions over <see cref="IRouter" /> — route a
///   model chain into a derived router and await its settled result.
/// </summary>
public static class RouterPresentExtensions
{
  /// <summary>
  ///   Routes the given model chain into a derived router, awaiting the
  ///   chain's result — a dismissal yields <see langword="null" />.
  /// </summary>
  /// <param name="router">The router to present through.</param>
  /// <param name="models">The participant models, outermost first.</param>
  /// <returns>The chain's settled result, or <see langword="null" /> on dismissal.</returns>
  public static async Task<object?> PresentOnDerivedAsync(this IRouter router, params object[] models)
  {
    IRouter derived = router.Derive(isEphemeral: true);
    if (derived is not { Role: RouterRole.Derived, Completion.Result.IsCompleted: false })
    {
      throw new InvalidOperationException("The derived router is invalid.");
    }

    await derived.RouteAsync(new Locator(models));
    return await derived.Completion;
  }
}
