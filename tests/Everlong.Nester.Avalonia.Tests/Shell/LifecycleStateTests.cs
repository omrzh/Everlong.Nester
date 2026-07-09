using Avalonia.Headless.XUnit;
using Everlong.Nester.DI;
using Everlong.Nester.Dialog;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Everlong.Nester.Tests.Shell;

/// <summary>
///   The lifecycle state machine (<see cref="ShellLifecycle"/>): a shell
///   walks Assembling → Active → Disposed exactly once.  Dispatch is admitted
///   only while Active — the ready anchor (<c>OnAssembled</c>) is a
///   dispatch-free pre-flight window by design (the pipeline activates after
///   it, so the first navigation never races unready dependencies); the
///   main-shell declaration requires an active shell.
/// </summary>
/// <remarks>
///   Serialized with the other AppLifetime/MainDispatcher static-facade
///   consumers (<see cref="RealShellCollection"/>): the SetMainShell case
///   binds a fresh <see cref="AppLifetimeImpl"/> (its dispatcher assignment
///   rebinds the static MainDispatcher facade) — parallel classes would race
///   the shared binding.
/// </remarks>
[Collection("RealShell")]
public class LifecycleStateTests
{
  private sealed record PlainIntent : IIntent;

  /// <summary>Records every intent that reaches the chain (proves the pipeline actually ran) and probes the Director's ready-anchor hook.</summary>
  private sealed class RecordingDirector : IShellDirector
  {
    public List<IIntent> Intents { get; } = [];

    public int AssembledCount { get; private set; }

    public IShell? AssembledShell { get; private set; }

    public ShellLifecycle LifecycleAtAssembled { get; private set; }

    public bool ServicesLiveAtAssembled { get; private set; }

    public bool AssembledBeforeFirstIntent { get; private set; }

    public bool ShellAnchorRanFirst { get; private set; }

    public IntentResult? DispatchDuringAssembled { get; private set; }

    public void OnAssembled(IShell shell)
    {
      AssembledCount++;
      AssembledShell = shell;
      LifecycleAtAssembled = shell.Lifetime.Lifecycle;
      ServicesLiveAtAssembled = shell.Services.GetService(typeof(RecordingDirector)) is not null;
      AssembledBeforeFirstIntent = Intents.Count == 0;
      ShellAnchorRanFirst = shell is ReadyAnchorProbeShell { ReadyAnchorRan: true };
      // The ready anchor is a dispatch-free window: the pipeline is not
      // activated yet — a dispatch here must pass and never reach the chain.
      DispatchDuringAssembled = shell.DispatchIntent(null, new PlainIntent()).GetAwaiter().GetResult();
    }

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      Intents.Add(context.Intent);
      return next(context);
    }

    public bool HandleError(Exception exception) => true;
  }

  /// <summary>Probes the ready anchor: records whether dispatch is admitted during <c>OnAssembled</c>.</summary>
  private sealed class ReadyAnchorProbeShell : AvaloniaShell
  {
    public bool ReadyAnchorRan { get; private set; }
    public IntentResult? DispatchDuringReadyAnchor { get; private set; }

    public ReadyAnchorProbeShell()
    {
      DirectorType = typeof(RecordingDirector);
      EnsureAssembled();   // the constructor path (tests) assembles the container up front
    }

    protected override IServiceProvider InitializeServices()
    {
      var services = new ServiceCollection();
      services.AddScoped<RecordingDirector>();
      services.AddNesterShell(this);
      services.AddNesterCore();
      services.AddNesterDialog();
      services.AddNesterNotice();
      return services.BuildServiceProvider(new ServiceProviderOptions
      {
        ValidateScopes = true,
        ValidateOnBuild = true
      });
    }

    protected override void PrepareHost() => ShellHost = new TestHostView();

    protected override void OnAssembled()
    {
      ReadyAnchorRan = true;
      // The ready anchor is a dispatch-free window: the pipeline is not
      // activated yet — a dispatch here must pass and never reach the chain.
      DispatchDuringReadyAnchor = DispatchIntent(null, new PlainIntent()).GetAwaiter().GetResult();
    }

    public RecordingDirector ProbeDirector => (RecordingDirector)Director!;
  }

  [AvaloniaFact]
  public void Lifecycle_Walks_AssemblingToActiveToDisposed()
  {
    var shell = new ReadyAnchorProbeShell();
    Assert.Equal(ShellLifecycle.Assembling, shell.Lifetime.Lifecycle);

    shell.Start();
    Assert.Equal(ShellLifecycle.Started, shell.Lifetime.Lifecycle);

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
    Assert.Equal(ShellLifecycle.Disposed, shell.Lifetime.Lifecycle);
  }

  [AvaloniaFact]
  public void Dispatch_DuringReadyAnchor_PassesAndNeverReachesChain()
  {
    var shell = new ReadyAnchorProbeShell();
    shell.Start();

    // The ready anchor ran and its dispatch passed — the chain stayed cold
    // (the PlainIntent never reached the Director; only the startup
    // dispatch's empty ShellActivationIntent does, after activation).
    Assert.True(shell.ReadyAnchorRan);
    Assert.Equal(IntentResult.Pass, shell.DispatchDuringReadyAnchor);
    Assert.DoesNotContain(shell.ProbeDirector.Intents, i => i is PlainIntent);

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  [AvaloniaFact]
  public void Dispatch_AfterActivation_ReachesChain()
  {
    var shell = new ReadyAnchorProbeShell();
    shell.Start();

    // Active = dispatch admitted: the same intent that passed during the
    // ready anchor now reaches the Director's chain.
    Assert.Equal(IntentResult.Pass, shell.DispatchIntent(null, new PlainIntent()).GetAwaiter().GetResult());
    Assert.Contains(shell.ProbeDirector.Intents, i => i is PlainIntent);

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  [AvaloniaFact]
  public void DirectorOnAssembled_FiresOnce_AtReadyAnchor_WithLiveServices()
  {
    var shell = new ReadyAnchorProbeShell();
    shell.Start();

    var director = shell.ProbeDirector;
    Assert.Equal(1, director.AssembledCount);
    Assert.Same(shell, director.AssembledShell);
    Assert.Equal(ShellLifecycle.Assembled, director.LifecycleAtAssembled);
    Assert.True(director.ServicesLiveAtAssembled);
    Assert.True(director.ShellAnchorRanFirst);   // the shell's ready anchor runs first

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  [AvaloniaFact]
  public void DirectorOnAssembled_PrecedesFirstIntent_AndDispatchPasses()
  {
    var shell = new ReadyAnchorProbeShell();
    shell.Start();

    var director = shell.ProbeDirector;
    Assert.True(director.AssembledBeforeFirstIntent);
    Assert.Equal(IntentResult.Pass, director.DispatchDuringAssembled);
    Assert.DoesNotContain(director.Intents, i => i is PlainIntent);

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  [AvaloniaFact]
  public void Start_Twice_Throws()
  {
    var shell = new ReadyAnchorProbeShell();
    shell.Start();

    Assert.Throws<InvalidOperationException>(() => shell.Start());

    shell.DisposeAsync().AsTask().GetAwaiter().GetResult();
  }

  [AvaloniaFact]
  public async Task SetMainShell_RequiresActiveShell()
  {
    var impl = new AppLifetimeImpl(new AppLifetimeOptions(), isSingleView: false);
    try
    {
      var assembling = new ReadyAnchorProbeShell();
      Assert.Throws<InvalidOperationException>(() => impl.SetMainShell(assembling));

      assembling.Start();   // activation first — then the declaration is admitted
      impl.SetMainShell(assembling);
      Assert.Same(assembling, impl.MainShell);

      await assembling.DisposeAsync();
    }
    finally
    {
      AppLifetime.ResetForTesting();
      MainDispatcher.ResetForTesting();   // the impl's dispatcher assignment rebinds the static facade — restore it for the serialized neighbors
      await impl.DisposeAsync();
    }
  }
}
