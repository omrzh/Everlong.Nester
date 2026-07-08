using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Everlong.Nester.Activation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Presentation;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Shell;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The shell-relay contract: a agent outlives the shell that bound it.
///   A rebuilt shell takes over the binding; the predecessor's teardown
///   neither stops nor disposes the agent; only the last binding shell
///   reclaims a declared agent.
/// </summary>
[Collection("RealShell")]
public sealed class ActivationRelayTests
{
  private sealed record ProbeIntent : IActivationIntent;

  private sealed class PassDirector : IShellDirector
  {
    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      context.Handle();
      return ValueTask.CompletedTask;
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Observes the agent lifecycle: bindings, serving state, disposals.</summary>
  private sealed class TrackingAgent : ActivationAgentBase
  {
    public int BindCount { get; private set; }

    public int DisposeCount { get; private set; }

    public bool IsServing { get; private set; }

    public ValueTask<IntentResult> Forward(IActivationIntent intent) => ForwardAsync(intent);

    public override void Bind(IActivationChannel channel)
    {
      base.Bind(channel);
      BindCount++;
      IsServing = true;
    }

    public override async ValueTask<bool> UnbindAsync(IActivationChannel channel)
    {
      bool current = await base.UnbindAsync(channel);
      if (current)
        IsServing = false;
      return current;
    }

    public override ValueTask DisposeAsync()
    {
      DisposeCount++;
      return ValueTask.CompletedTask;
    }
  }

  [AvaloniaFact]
  public async Task RelayedAgent_SurvivesThePredecessor_AndTheLastBinderReleasesIt()
  {
    var impl = NewImpl();
    var agent = new TrackingAgent();
    try
    {
      PrepareTemplates();

      // The process-container path: both shells resolve the same agent.
      var first = new TestShell<PassDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      first.Start();
      await first.Lifetime.Startup;

      var second = new TestShell<PassDirector>(s => s.AddSingleton<IActivationAgent>(agent));
      second.Start();
      await second.Lifetime.Startup;

      Assert.Equal(2, agent.BindCount);
      Assert.True(agent.IsServing);

      // The predecessor closes: superseded — the agent keeps serving.
      await first.DisposeAsync();

      Assert.True(agent.IsServing);
      Assert.Equal(0, agent.DisposeCount);
      await agent.Forward(new ProbeIntent());
      Assert.Empty(agent.TakePendingIntents());   // still delivered to the successor

      // The relay tail closes: serving stops, but the container path never
      // reclaims (the registrant owns it).
      await second.DisposeAsync();

      Assert.False(agent.IsServing);
      Assert.Equal(0, agent.DisposeCount);
      await agent.Forward(new ProbeIntent());
      Assert.Single(agent.TakePendingIntents());   // no channel — parked
    }
    finally
    {
      await agent.DisposeAsync();
      await DisposeImpl(impl);
    }
  }

  [AvaloniaFact]
  public async Task DeclaredAgent_Alone_IsReclaimedAtTeardown()
  {
    var impl = NewImpl();
    var agent = new TrackingAgent();
    try
    {
      PrepareTemplates();

      var shell = new TestShell<PassDirector> { ActivationAgent = agent };
      shell.Start();
      await shell.Lifetime.Startup;

      Assert.Equal(1, agent.BindCount);

      await shell.DisposeAsync();

      Assert.False(agent.IsServing);
      Assert.Equal(1, agent.DisposeCount);
    }
    finally
    {
      await agent.DisposeAsync();
      await DisposeImpl(impl);
    }
  }

  [AvaloniaFact]
  public async Task DeclaredAgent_RelayedToTheSuccessor_IsReclaimedOnlyByTheTail()
  {
    var impl = NewImpl();
    var agent = new TrackingAgent();
    try
    {
      PrepareTemplates();

      var first = new TestShell<PassDirector> { ActivationAgent = agent };
      first.Start();
      await first.Lifetime.Startup;

      var second = new TestShell<PassDirector> { ActivationAgent = agent };
      second.Start();
      await second.Lifetime.Startup;

      // The predecessor declared ownership but lost the binding: it must not
      // reclaim the agent the successor is serving through.
      await first.DisposeAsync();

      Assert.True(agent.IsServing);
      Assert.Equal(0, agent.DisposeCount);

      await second.DisposeAsync();

      Assert.Equal(1, agent.DisposeCount);
    }
    finally
    {
      await agent.DisposeAsync();
      await DisposeImpl(impl);
    }
  }

  private static void PrepareTemplates()
  {
    Application.Current!.DataTemplates.Clear();
    Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
    Application.Current!.DataTemplates.Add(new FakeTemplate());
  }

  private static AppLifetimeImpl NewImpl() => new(new AppLifetimeOptions(), isSingleView: false);

  private static async Task DisposeImpl(AppLifetimeImpl impl)
  {
    await impl.DisposeAsync();
    MainDispatcher.ResetForTesting();
  }

  /// <summary>Fake visual stack: every view model maps to a tagged ContentControl.</summary>
  private sealed class FakeTemplate : IDataTemplate
  {
    public Control? Build(object? param)
      => param is null ? null : new TestHostView { Tag = param.GetType() };

    public bool Match(object? data) => true;
  }
}
