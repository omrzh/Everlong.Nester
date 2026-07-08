using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input.Platform;
using Everlong.Nester.Presentation;
using Everlong.Nester.Hosting;
using Everlong.Nester.Intent;
using Everlong.Nester.Shell;
using Everlong.Nester.Threading;
using Everlong.Nester.Tests.Shell;
using Xunit;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   Host surface contract: the host owns main-shell tracking and the
///   process container — activation is the agent's surface (see
///   <see cref="ActivationAgentTests"/>), the host never touches it.
///   Every test builds its OWN INDEPENDENT AppLifetimeImpl (never
///   bound to the static facade) and resets the process-wide MainDispatcher
///   in teardown.
/// </summary>
[Collection("RealShell")]
public sealed class ShellManagerTests
{
  private sealed class RecordingDirector : IShellDirector
  {
    public List<IIntent> Intents { get; } = [];

    public ValueTask HandleAsync(IntentContext context, IntentDelegate next)
    {
      Intents.Add(context.Intent);
      context.Handle();
      return ValueTask.CompletedTask;
    }

    public void OnAssembled(IShell shell) { }
    public bool HandleError(Exception exception) => true;
  }

  [AvaloniaFact]
  public async Task GetPlatformService_ProbesShellPlatformCapabilities()
  {
    var impl = NewImpl();
    try
    {

      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      var shell = new TestShell<RecordingDirector>();
      shell.Start();
      await shell.Lifetime.Startup;

      // Platform services are probed by type.  In this harness the shell has
      // no platform TopLevel (the host is a tagged ContentControl), so
      // every probe yields null — the probe must be null-safe and never
      // throw; with a real window the Avalonia natives pass through.
      Assert.Null(shell.GetPlatformService<IClipboard>());
      Assert.Null(shell.GetPlatformService<HostPropertyBase>());
    }
    finally
    {
      AppLifetime.ResetForTesting();
      await DisposeImpl(impl);
    }
  }

  [AvaloniaFact]
  public async Task SetMainShell_TracksTheStartedShell()
  {
    var impl = NewImpl();
    try
    {

      Application.Current!.DataTemplates.Clear();
      Application.Current!.DataTemplates.Add(new NesterExtendedViewLocator());
      Application.Current!.DataTemplates.Add(new FakeTemplate());

      var shell = new TestShell<RecordingDirector>();
      shell.Start();
      impl.SetMainShell(shell);   // the main-shell declaration is explicit — the harness promotes the started shell
      await shell.Lifetime.Startup;

      Assert.Same(shell, impl.MainShell);

      await shell.DisposeAsync();
    }
    finally
    {
      await DisposeImpl(impl);
    }
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
