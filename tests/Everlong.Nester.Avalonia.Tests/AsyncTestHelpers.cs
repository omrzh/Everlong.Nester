namespace Everlong.Nester.Tests;

/// <summary>
///   The shared async polling wait sequence — one copy instead of five
///   near-identical private ones (visual/engine updates are event-driven;
///   tests poll until the condition holds).
/// </summary>
internal static class AsyncTestHelpers
{
  /// <summary>Polls until the condition holds or the timeout elapses (throws).</summary>
  public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 2000)
  {
    long deadline = Environment.TickCount64 + timeoutMs;
    while (!condition())
    {
      if (Environment.TickCount64 > deadline)
        throw new TimeoutException("Condition was not met within the timeout.");
      await Task.Delay(10);
    }
  }
}
