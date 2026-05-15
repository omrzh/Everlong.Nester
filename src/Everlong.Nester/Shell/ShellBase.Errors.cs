using System.Runtime.ExceptionServices;
using Everlong.Nester.Hosting;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Shell;

partial class ShellBase
{
  /// <summary>
  ///   Routes an error through the shell's error pipeline: the Director, the
  ///   AppLifetime's error handler, then the terminal hook
  ///   (<see cref="OnUnhandledError"/>).  Fire-and-forget by contract — the
  ///   reporting site only reports, it never inspects the outcome.
  /// </summary>
  public void ReportError(Exception exception)
  {
    try
    {
      if (Director is { } director && director.HandleError(exception))
        return;
      if (AppLifetime.Current?.ErrorHandler is { } handler && handler.HandleError(exception))
        return;
    }
    catch
    {
      // A handler threw on the error path — the chain is broken; escalate
      // the original exception to the terminal hook (the error path must
      // never crash on the error path).
    }

    OnUnhandledError(exception);
  }

  /// <summary>
  ///   The last-resort hook — invoked when neither the Director nor the
  ///   host-level error handler consumed the exception.  Override to choose
  ///   the app's terminal behavior (log, collect, crash).  The default
  ///   rethrows through the UI dispatcher's unhandled-exception path.
  /// </summary>
  protected virtual void OnUnhandledError(Exception exception) => RethrowOnUiThread(exception);

  /// <summary>
  ///   Surfaces the exception through the UI dispatcher's unhandled-exception
  ///   path; without a bound dispatcher, rethrows synchronously.
  /// </summary>
  private static void RethrowOnUiThread(Exception exception)
  {
    if (!MainDispatcher.TryGet(out var dispatcher))
    {
      // No UI context at all (no bound dispatcher): a
      // synchronous rethrow is the only surface — it escapes ReportError's
      // handler-chain guard and faults the caller.
      ExceptionDispatchInfo.Capture(exception).Throw();
      return;
    }

    // Always post — even when already on the UI thread: the dispatcher
    // reports the callback exception to its unhandled-exception path (a
    // synchronous rethrow here would land in ReportError's guard and be
    // swallowed).
    dispatcher.SurfaceUnhandled(exception);
  }
}
