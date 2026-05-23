using Everlong.Nester.ComponentModel;
using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Everlong.Nester.Dialog;

public static partial class DialogSessionExtensions
{
  extension(IRouter router)
  {
    /// <summary>
    ///   Creates and shows a wait/progress dialog, returning the session as a disposable scope.
    ///   Dispose (or <c>using</c>) closes the dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="options">
    ///   Optional session configuration.
    /// </param>
    public WaitDialogSession CreateWait(string title,
                                        WaitDialogOptions? options = null)
    {
      options ??= new WaitDialogOptions();
      WaitDialogSession waitSession = new()
      {
        Title = title,
        StatusText = options.Message,
        Maximum = options.Maximum,
        Token = options.CancellationToken
      };

      _ = router.ShowAsync(waitSession, options.ToDimmer());
      return waitSession;
    }
  }
}

/// <summary>
///   Represents progress reporting capabilities for wait dialogs.
/// </summary>
public interface IWaitProgress : IProgress<double>, IProgress<string>;

/// <summary>
///   Represents a wait/progress dialog session.
/// </summary>
public partial class WaitDialogSession : DialogSessionBase<object?>, IWaitProgress, IDisposable
{
  /// <summary>Creates a session with an empty status and no reported progress.</summary>
  public WaitDialogSession() { }

  /// <summary>
  ///   Gets or sets the caption shown above the progress bar.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  [ObservableProperty] public partial string? StatusText { get; set; }

  /// <summary>
  ///   Gets the cancellation token scoped to this session.
  ///   Pass to async operations to propagate cancellation.
  /// </summary>
  public CancellationToken Token { get; init; }

  /// <summary>
  ///   Gets the most recent exception that occurred during a <see cref="RunAsync" /> call, if any.
  /// </summary>
  public Exception? LastException { get; private set; }

  /// <summary>
  ///   Gets a value indicating whether the last <see cref="RunAsync" /> call completed without errors.
  /// </summary>
  public bool IsSucceeded { get; private set; }

  /// <summary>
  ///   Gets a value indicating whether the last <see cref="RunAsync" /> call was canceled.
  /// </summary>
  public bool IsCanceled { get; private set; }

  /// <summary>
  ///   Gets a value indicating whether the last <see cref="RunAsync" /> call faulted.
  /// </summary>
  public bool IsFaulted => LastException != null;

  /// <summary>
  ///   Gets or sets the current numeric progress value.
  /// </summary>
  /// <remarks>
  ///   Ranges from 0.0 to <see cref="Maximum" />. Setting this property also clears
  ///   <see cref="IsIndeterminate" />.
  /// </remarks>
  [ObservableProperty] public partial double Progress { get; set; }

  partial void OnProgressChanged(double value)
  {
    // Reported progress is determinate by definition — the remark on Progress
    // states it, and the bar has to agree with the caption next to it.
    IsIndeterminate = false;
  }

  /// <summary>
  ///   Gets or sets whether the progress bar is in indeterminate mode.
  /// </summary>
  [ObservableProperty] public partial bool IsIndeterminate { get; set; } = true;

  /// <summary>
  ///   Gets or sets the maximum value for <see cref="Progress" />. Defaults to <c>1.0</c>.
  /// </summary>
  [ObservableProperty] public partial double Maximum { get; set; } = 1.0d;

  /// <inheritdoc />
  public void Dispose()
  {
    Close();
  }

  void IProgress<double>.Report(double value)
  {
    IsIndeterminate = false;
    Progress = value;
  }

  void IProgress<string>.Report(string value)
  {
    StatusText = value;
  }

  /// <summary>
  ///   Reports numeric progress. Sets <see cref="IsIndeterminate" /> to <see langword="false" />.
  /// </summary>
  public void Report(double value)
  {
    ((IProgress<double>)this).Report(value);
  }

  /// <summary>
  ///   Reports a progress message by updating <see cref="StatusText" />.
  /// </summary>
  public void Report(string value)
  {
    ((IProgress<string>)this).Report(value);
  }

  /// <summary>
  ///   Advances <see cref="Progress" /> by <paramref name="step" />, clamped to <see cref="Maximum" />.
  ///   Clears <see cref="IsIndeterminate" /> on first call.
  /// </summary>
  /// <param name="step">Amount to advance. Defaults to <c>1.0</c>.</param>
  public void Advance(double step = 1.0)
  {
    IsIndeterminate = false;
    Progress = Math.Min(Progress + step, Maximum);
  }

  /// <summary>
  ///   Returns an <see cref="IProgress{T}" /> adapter that translates <typeparamref name="TProgress" />
  ///   values into dialog state updates via <paramref name="mapper" />.
  /// </summary>
  /// <typeparam name="TProgress">The custom progress payload type.</typeparam>
  /// <param name="mapper">Callback that maps a payload value to dialog state.</param>
  public IProgress<TProgress> AsProgress<TProgress>(Action<WaitDialogSession, TProgress> mapper)
  {
    ArgumentNullException.ThrowIfNull(mapper);
    return new DelegatingProgress<TProgress>(value => mapper(this, value));
  }

  /// <summary>
  ///   Runs <paramref name="work" /> and captures the outcome in
  ///   <see cref="IsSucceeded" />, <see cref="IsCanceled" />, and <see cref="LastException" />.
  /// </summary>
  /// <param name="work">The asynchronous work to perform.</param>
  /// <param name="background">
  ///   When <see langword="true" />, executes <paramref name="work" /> on a background thread.
  /// </param>
  public async Task RunAsync(
    Func<IWaitProgress, CancellationToken, Task> work,
    bool background = true)
  {
    ArgumentNullException.ThrowIfNull(work);
    try
    {
      if (background)
      {
        await Task.Run(() => work(this, Token), Token);
      }
      else
      {
        await work(this, Token);
      }

      LastException = null;
      IsSucceeded = true;
    }
    catch (OperationCanceledException) when (Token.IsCancellationRequested)
    {
      IsSucceeded = false;
      IsCanceled = true;
    }
    catch (Exception ex)
    {
      LastException = ex;
      IsSucceeded = false;
    }
  }

  /// <summary>
  ///   Runs <paramref name="work" /> and returns its result.
  ///   Outcome is captured in <see cref="IsSucceeded" />, <see cref="IsCanceled" />, and
  ///   <see cref="LastException" />.
  /// </summary>
  /// <typeparam name="TResult">The type of the result produced by <paramref name="work" />.</typeparam>
  /// <param name="work">The asynchronous work to perform.</param>
  /// <param name="background">
  ///   When <see langword="true" />, executes <paramref name="work" /> on a background thread.
  /// </param>
  /// <returns>The work result, or <see langword="default" /> if canceled or faulted.</returns>
  public async Task<TResult?> RunAsync<TResult>(
    Func<IWaitProgress, CancellationToken, Task<TResult>> work,
    bool background = true)
  {
    ArgumentNullException.ThrowIfNull(work);
    TResult? result = default;
    try
    {
      result = background
                 ? await Task.Run(() => work(this, Token), Token)
                 : await work(this, Token);

      LastException = null;
      IsSucceeded = true;
    }
    catch (OperationCanceledException) when (Token.IsCancellationRequested)
    {
      IsSucceeded = false;
      IsCanceled = true;
    }
    catch (Exception ex)
    {
      LastException = ex;
      IsSucceeded = false;
    }

    return result;
  }

  private sealed class DelegatingProgress<T>(Action<T> handler) : IProgress<T>
  {
    void IProgress<T>.Report(T value)
    {
      handler(value);
    }
  }
}

/// <summary>
///   Configuration options for <see cref="DialogSessionExtensions.CreateWait" />.
/// </summary>
public sealed class WaitDialogOptions : DialogOptions
{
  // A progress dialog is not escapable by a backdrop tap — it is disposed by
  // the caller that started the work, not by the user.
  /// <summary>Creates options for a dialog that refuses external dismissal.</summary>
  public WaitDialogOptions()
  {
    IsMandatory = true;
  }

  /// <summary>Gets the initial message displayed in the wait dialog.</summary>
  public string Message { get; init; } = "";

  /// <summary>Gets the maximum progress value. Defaults to <c>1.0</c>.</summary>
  public double Maximum { get; init; } = 1.0d;

  /// <summary>Gets the cancellation token scoped to the wait operation.</summary>
  public CancellationToken CancellationToken { get; init; }
}
