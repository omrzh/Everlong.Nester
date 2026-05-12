namespace Everlong.Nester.Routing;

/// <summary>
///   The completion channel of a derived router — the public write end that
///   routes to the owning router's close sequence.
/// </summary>
internal sealed class ResultChannel : IRouterCompletion
{
  private readonly RouterBase _router;
  private readonly RouterStack _model;

  internal ResultChannel(RouterBase router)
  {
    _router = router;
    _model = router.Model;
  }

  /// <inheritdoc />
  public void Complete(object? result) => _router.RunDerivedClose(result);

  /// <summary>The router's result — settles when the router closes.</summary>
  public Task<object?> Result => _model.Result;
}
