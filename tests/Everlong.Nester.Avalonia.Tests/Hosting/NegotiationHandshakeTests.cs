using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Everlong.Nester.Activation;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The negotiation loop: a follower's args are converted by the leader's
///   agent; once the leader accepts, the converted intents are dispatched
///   into the main-shell chain ("accepted" = "handled") before the follower
///   receives its result — the follower's files/deep links really open in
///   the leader.
/// </summary>
[SupportedOSPlatform("windows")]
[Collection("RealShell")]
public sealed class NegotiationHandshakeTests
{
  /// <summary>Accepts every intent; records what the chain delivered (the startup dispatch's empty ShellActivationIntent is framework plumbing — not recorded).</summary>
  private sealed class RecordingDirector : IShellDirector
  {
    public List<IIntent> Intents { get; } = [];

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      if (context.Intent is not ShellActivationIntent)
      {
        Intents.Add(context.Intent);
      }

      context.Handle();
      return ValueTask.CompletedTask;
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  [AvaloniaFact]
  public async Task AcceptedNegotiation_FollowerActivationContent_IsHandledByLeaderChain()
  {
    var leaseName = "nester-test-" + Guid.NewGuid().ToString("N");
    var impl = NewImpl();
    var agent = new WindowsActivationAgent(
      leaseName: leaseName,
      enableNegotiation: true);
    try
    {

      // Fake visual stack (same as RealShell): every view model maps to a tagged ContentControl.
      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      // The agent rides the shell's container (the harness mirrors the
      // template's bridge); the shell binds it at startup — the leader's
      // handshake dispatch goes through the shell chain.
      var shell = new TestShell<RecordingDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      shell.Start();
      impl.SetMainShell(shell);   // the main-shell declaration is explicit now
      await shell.Lifetime.Startup;

      // The follower negotiates on its own thread: the leader's decision and
      // the converted-intent handling run on the test thread (main
      // dispatcher), which stays free while NegotiateSync blocks.
      string[] followerArgs = ["https://example.com/deep?x=1"];
      bool accepted = await Task.Run(
        () => agent.NegotiateSync(BuildChannelName(leaseName), followerArgs, TimeSpan.FromSeconds(10)));

      Assert.True(accepted, "the leader must accept the follower");

      // The chain received the negotiation intent (with the converted
      // content) first, then the converted activation intent itself — the
      // follower's deep link was really handled.  (The startup dispatch's
      // empty ShellActivationIntent and the test process's own args
      // conversion — FileActivationIntent — are framework/harness noise.)
      var director = (RecordingDirector)shell.Director!;
      var negotiationTraffic = director.Intents
        .Where(i => i is not ShellActivationIntent && i is not FileActivationIntent)
        .ToList();
      Assert.Equal(
        [typeof(NegotiateActivationIntent), typeof(UriActivationIntent)],
        negotiationTraffic.Select(i => i.GetType()));

      var negotiate = Assert.IsType<NegotiateActivationIntent>(negotiationTraffic[0]);
      Assert.Equal(followerArgs, negotiate.Args);
      Assert.Equal(
        [typeof(UriActivationIntent)],
        negotiate.Converted.Select(i => i.GetType()));
      Assert.Equal(
        new Uri("https://example.com/deep?x=1"),
        Assert.IsType<UriActivationIntent>(negotiate.Converted[0]).Uri);

      Assert.IsType<UriActivationIntent>(negotiationTraffic[1]);
    }
    finally
    {
      await agent.DisposeAsync();
      AppLifetime.ResetForTesting();
      await DisposeImpl(impl);
    }
  }

  [AvaloniaFact]
  public async Task RejectedNegotiation_FollowerContent_IsNotHandledByLeader()
  {
    var leaseName = "nester-test-" + Guid.NewGuid().ToString("N");
    var impl = NewImpl();
    var agent = new WindowsActivationAgent(
      leaseName: leaseName,
      enableNegotiation: true);
    try
    {

      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      var shell = new TestShell<DecliningDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      shell.Start();
      impl.SetMainShell(shell);   // the main-shell declaration is explicit now
      await shell.Lifetime.Startup;

      bool accepted = await Task.Run(
        () => agent.NegotiateSync(BuildChannelName(leaseName), ["https://example.com/deep"], TimeSpan.FromSeconds(10)));

      Assert.False(accepted, "the leader must decline");

      // Only the negotiation intent reached the chain (the startup
      // dispatch's empty ShellActivationIntent and the test process's own
      // args conversion are not recorded) — the follower's content stays
      // unhandled here (the follower proceeds on its own).
      var director = (DecliningDirector)shell.Director!;
      Assert.Equal(
        [typeof(NegotiateActivationIntent)],
        director.Intents
          .Where(i => i is not ShellActivationIntent && i is not FileActivationIntent)
          .Select(i => i.GetType()));
    }
    finally
    {
      await agent.DisposeAsync();
      AppLifetime.ResetForTesting();
      await DisposeImpl(impl);
    }
  }

  /// <summary>
  ///   The shell-rebuild relay: the successor binds the agent before the
  ///   predecessor closes; the predecessor's teardown neither stops nor
  ///   reclaims it, and the next handshake reaches the successor's chain.
  /// </summary>
  [AvaloniaFact]
  public async Task RelayedAgent_HandshakeAfterShellRebuild_ReachesTheSuccessor()
  {
    var leaseName = "nester-test-" + Guid.NewGuid().ToString("N");
    var impl = NewImpl();
    var agent = new WindowsActivationAgent(
      leaseName: leaseName,
      enableNegotiation: true);
    try
    {
      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      var first = new TestShell<RecordingDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      first.Start();
      impl.SetMainShell(first);
      await first.Lifetime.Startup;

      // Rebuild: the successor binds before the predecessor closes.
      var second = new TestShell<RecordingDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      second.Start();
      impl.SetMainShell(second);
      await second.Lifetime.Startup;
      await first.DisposeAsync();

      string[] followerArgs = ["https://example.com/deep?x=1"];
      bool accepted = await Task.Run(
        () => agent.NegotiateSync(BuildChannelName(leaseName), followerArgs, TimeSpan.FromSeconds(10)));

      Assert.True(accepted, "the leader must accept the follower");

      var director = (RecordingDirector)second.Director!;
      Assert.Equal(
        [typeof(NegotiateActivationIntent), typeof(UriActivationIntent)],
        director.Intents
          .Where(i => i is not ShellActivationIntent && i is not FileActivationIntent)
          .Select(i => i.GetType()));
    }
    finally
    {
      await agent.DisposeAsync();
      AppLifetime.ResetForTesting();
      await DisposeImpl(impl);
    }
  }

  /// <summary>Declines every negotiation (SingleInstance pattern).</summary>
  private sealed class DecliningDirector : IShellDirector
  {
    public List<IIntent> Intents { get; } = [];

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      if (context.Intent is not ShellActivationIntent)
      {
        Intents.Add(context.Intent);
      }

      if (context.Intent is not NegotiateActivationIntent)
      {
        context.Handle();
        return ValueTask.CompletedTask;
      }

      return next(context);
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception)
    {
      return false;
    }
  }

  private static AppLifetimeImpl NewImpl()
  {
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    impl.BindStaticFacades();   // the negotiation flow reads AppLifetime.Current
    return impl;
  }

  private static async Task DisposeImpl(AppLifetimeImpl impl)
  {
    await impl.DisposeAsync();
    MainDispatcher.ResetForTesting();
  }

  /// <summary>Replicates <c>DesktopActivationAgent.BuildChannelName</c>.</summary>
  private static string BuildChannelName(string leaseName)
  {
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(leaseName));
    return $"nester_activation_{Convert.ToHexString(hash[..8]).ToLowerInvariant()}";
  }

  /// <summary>Fake visual stack: every view model maps to a tagged ContentControl.</summary>
  private sealed class FakeTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new TestHostView { Tag = param.GetType() };

    public bool Match(object? data) => true;
  }
}
