using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Everlong.Nester.Intent;
using Everlong.Nester.Threading;

namespace Everlong.Nester.Activation;

/// <summary>
///   Base class for single-instance activation agents that negotiate leader
///   election over a named-pipe transport.
/// </summary>
/// <remarks>
///   Desktop-only. The lease, transport, and handshake protocol are shared by
///   every desktop platform; window-activation behavior is defined by
///   <see cref="ResolveLeaderWindowHandle"/>,
///   <see cref="ActivateLeaderWindow"/>, and <see cref="ActivateFollowerWindow"/>.
/// </remarks>
public abstract partial class DesktopActivationAgent : ActivationAgentBase, IDesktopActivationAgent
{
  private readonly Mutex? _mutex;
  private readonly string _channelName;
  private readonly TimeSpan _negotiateTimeout;
  private readonly bool _negotiationEnabled;
  private CancellationTokenSource? _serverCts;
  private Task? _serverTask;
  private bool _disposed;

  /// <summary>
  ///   Acquires the leader lease (named mutex derived from the process path)
  ///   and prepares the named-pipe channel — unless negotiation is disabled:
  ///   then this process is its own primary instance (no lease, no pipe).
  /// </summary>
  /// <param name="leaseName">The process-wide lease name for leader
  ///   election and channel naming. When <see langword="null" />, a stable
  ///   name derived from the current process executable path is used.</param>
  /// <param name="enableNegotiation">Whether cross-process negotiation is
  ///   enabled (leader lease + named pipe).  Disabled: the agent keeps its
  ///   platform identity (args conversion, foreground hooks) without claiming
  ///   single-instance.</param>
  /// <param name="negotiateTimeout">The maximum time the follower blocks
  ///   waiting for the leader's decision, including any user interaction on
  ///   the leader side. When <see langword="null" />, defaults to 8 seconds.</param>
  protected DesktopActivationAgent(
    string? leaseName = null,
    bool enableNegotiation = false,
    TimeSpan? negotiateTimeout = null)
  {
    _negotiationEnabled = enableNegotiation;
    _negotiateTimeout = negotiateTimeout ?? TimeSpan.FromSeconds(8);
    leaseName ??= BuildLeaseName();
    (_mutex, IsLeader, _channelName) = enableNegotiation
      ? AcquireLease(leaseName)
      : (null, true, string.Empty);
  }

  /// <inheritdoc />
  public bool IsLeader { get; }

  /// <summary>Builds a stable, process-path-derived lease name for leader election.</summary>
  private static string BuildLeaseName()
  {
    string processPath = Environment.ProcessPath ?? AppDomain.CurrentDomain.FriendlyName;
    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(processPath));
    return $"nester_{System.Convert.ToHexString(hash[..8]).ToLowerInvariant()}";
  }

  /// <inheritdoc />
  public override void Bind(IActivationChannel channel)
  {
    base.Bind(channel);
    StartListening();
  }

  /// <inheritdoc />
  public override async ValueTask<bool> UnbindAsync(IActivationChannel channel)
  {
    bool current = await base.UnbindAsync(channel);
    if (current)
      await StopListeningAsync();
    return current;
  }

  /// <summary>Starts the named-pipe server (leader only; no-op otherwise) — the handshake dispatches through the bound channel.</summary>
  private void StartListening()
  {
    if (!_negotiationEnabled || !IsLeader || _serverTask is not null)
      return;

    _serverCts = new CancellationTokenSource();
    _serverTask = ListenAsync(_channelName, _serverCts.Token);
  }

  /// <summary>Stops the named-pipe server; a later <see cref="Bind" /> starts a fresh one.</summary>
  private async ValueTask StopListeningAsync()
  {
    if (_serverTask is not { } server)
      return;

    _serverTask = null;
    var cts = _serverCts;
    _serverCts = null;
    if (cts is not null)
    {
      await cts.CancelAsync();
      cts.Dispose();
    }

    try
    { await server.ConfigureAwait(false); }
    catch (OperationCanceledException) { }
  }

  /// <inheritdoc />
  public bool TryNegotiate(IReadOnlyList<string> args)
  {
    if (IsLeader)
      return false;

    return NegotiateSync(_channelName, args, _negotiateTimeout);
  }

  /// <summary>Stops the negotiation server and releases the leader lease.</summary>
  public override async ValueTask DisposeAsync()
  {
    if (_disposed)
      return;
    _disposed = true;

    await StopListeningAsync();

    if (_mutex != null)
    {
      try
      { _mutex.ReleaseMutex(); }
      catch { }
      _mutex.Dispose();
    }
  }

  #region Window-activation hooks

  /// <summary>
  ///   Returns the window handle published in the handshake Ack (leader side).
  /// </summary>
  /// <remarks>
  ///   Called on the UI thread. Returns 0 when the platform exposes no window handle.
  /// </remarks>
  protected virtual long ResolveLeaderWindowHandle()
    => Channel?.HostHandle ?? 0;

  /// <summary>
  ///   Brings the leader's main window to the foreground (follower side).
  /// </summary>
  protected virtual void ActivateLeaderWindow(long mainWindowHandle) { }

  /// <summary>
  ///   Brings the follower process to the foreground after a declined activation (leader side).
  /// </summary>
  protected virtual void ActivateFollowerWindow(int sourcePid) { }

  #endregion

  #region Lease

  private static (Mutex? mutex, bool isLeader, string channelName) AcquireLease(string leaseName)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(leaseName);
    var mutex = new Mutex(false, leaseName);
    bool acquired;
    try
    { acquired = mutex.WaitOne(0); }
    catch (AbandonedMutexException) { acquired = true; }

    var channel = BuildChannelName(leaseName);
    if (acquired)
      return (mutex, true, channel);

    mutex.Dispose();
    return (null, false, channel);
  }

  private static string BuildChannelName(string leaseName)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(leaseName));
    return $"nester_activation_{System.Convert.ToHexString(hash[..8]).ToLowerInvariant()}";
  }

  #endregion

  #region Transport

  /// <summary>Serves the named-pipe handshake loop until canceled.</summary>
  public async Task ListenAsync(
    string channelName,
    CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      try
      {
        await using var server = new NamedPipeServerStream(
          channelName,
          PipeDirection.InOut,
          maxNumberOfServerInstances: 1,
          PipeTransmissionMode.Byte,
          PipeOptions.Asynchronous);

        await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
        await ProcessHandshakeAsync(server, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException) { break; }
      catch (IOException) { }
      catch (JsonException) { }
    }
  }


  private async Task ProcessHandshakeAsync(
    Stream stream,
    CancellationToken cancellationToken)
  {
    Debug.WriteLine("ProcessHandshakeAsync...");
    using var reader =
      new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
    using var writer =
      new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
      { AutoFlush = true };

    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
    Debug.WriteLine($"args from follower: {line}");
    if (string.IsNullOrWhiteSpace(line))
      return;

    var argsMsg = JsonSerializer.Deserialize(line, PipeJsonContext.Default.PipeArgsMessage);
    if (argsMsg is null)
      return;

    long handle = 0;
    IntentResult decision = IntentResult.Pass;
    IReadOnlyList<IActivationIntent> converted = [];
    try
    {
      // The follower's activation content — converted up front so the
      // negotiation intent can show the user what accepting would handle.
      converted = Convert(argsMsg.Args);

      // The negotiation decision and the converted-intent dispatch are UI
      // work (the host chain is UI): hop to the main thread via the
      // thread facade — the agent never touches the host's static surface.
      await MainDispatcher.InvokeAsync(async () =>
      {
        handle = ResolveLeaderWindowHandle();
        var intent = new NegotiateActivationIntent(argsMsg.Args, argsMsg.SourcePid, converted);
        if (Channel is { } channel)
        {
          decision = await channel.DispatchAsync(intent);
        }
        else
        {
          // No channel bound yet — park; a later FlushAsync drains it.
          await ForwardAsync(intent);
        }
      }).ConfigureAwait(false);
    }
    catch (Exception e)
    {
      Console.WriteLine($"[Exception in Dispatching to UI]: {e.Message}");
    }

    var ack = new PipeAckMessage(Environment.ProcessId, handle);
    await writer.WriteLineAsync(JsonSerializer.Serialize(ack, PipeJsonContext.Default.PipeAckMessage))
      .ConfigureAwait(false);

    var ackLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
    if (string.IsNullOrWhiteSpace(ackLine))
      return;

    bool accepted = decision == IntentResult.Handled;
    if (accepted)
    {
      // The negotiation was accepted: the follower's activation content is
      // handled here — converted intents go through the host chain (the
      // runtime activation path), so files/deep links really open in the
      // leader.  The follower receives its result only after the handling
      // completed ("accepted" = "handled").
      foreach (var intent in converted)
      {
        await MainDispatcher.InvokeAsync(async () =>
        {
          if (Channel is { } channel)
          {
            await channel.DispatchAsync(intent);
          }
        }).ConfigureAwait(false);
      }
    }

    await writer.WriteLineAsync(
        JsonSerializer.Serialize(new PipeResultMessage(accepted), PipeJsonContext.Default.PipeResultMessage))
      .ConfigureAwait(false);

    if (!accepted)
    {
      // The follower proceeds on its own — bring its window to the front
      // while we hold the foreground permission (only the leader has it).
      await MainDispatcher.InvokeAsync(() => ActivateFollowerWindow(argsMsg.SourcePid)).ConfigureAwait(false);
    }
  }

  /// <summary>Synchronously negotiates with the leader over the named pipe.</summary>
  public bool NegotiateSync(string channelName, IReadOnlyList<string> args, TimeSpan connectTimeout)
  {
    try
    {
      using var client = new NamedPipeClientStream(".", channelName, PipeDirection.InOut);
      client.Connect((int)connectTimeout.TotalMilliseconds);

      using var reader =
        new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
      using var writer =
        new StreamWriter(client, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
        { AutoFlush = true };

      var argsMsg = new PipeArgsMessage([.. args], Environment.ProcessId);
      writer.WriteLine(JsonSerializer.Serialize(argsMsg, PipeJsonContext.Default.PipeArgsMessage));

      var ackLine = reader.ReadLine();
      if (string.IsNullOrWhiteSpace(ackLine))
        return false;

      var ackMsg = JsonSerializer.Deserialize(ackLine, PipeJsonContext.Default.PipeAckMessage);
      if (ackMsg is null)
        return false;

      ActivateLeaderWindow(ackMsg.MainWindowHandle);

      writer.WriteLine("ACK");

      var resultLine = reader.ReadLine();
      if (string.IsNullOrWhiteSpace(resultLine))
        return false;

      var result = JsonSerializer.Deserialize(resultLine, PipeJsonContext.Default.PipeResultMessage);
      return result?.Accepted ?? false;
    }
    catch (TimeoutException) { return false; }
    catch (IOException) { return false; }
    catch (JsonException) { return false; }
  }

  internal sealed record PipeArgsMessage(string[] Args, int SourcePid);

  internal sealed record PipeAckMessage(int LeaderPid, long MainWindowHandle);

  internal sealed record PipeResultMessage(bool Accepted);

  [JsonSerializable(typeof(PipeArgsMessage))]
  [JsonSerializable(typeof(PipeAckMessage))]
  [JsonSerializable(typeof(PipeResultMessage))]
  internal sealed partial class PipeJsonContext : JsonSerializerContext;

  #endregion
}
