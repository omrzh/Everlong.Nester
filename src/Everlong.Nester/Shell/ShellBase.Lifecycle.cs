using System.Diagnostics.CodeAnalysis;
using Everlong.Nester.Activation;

namespace Everlong.Nester.Shell;

abstract partial class ShellBase
{
  /// <summary>The shell's lifetime — the state, the startup signal and the teardown signals; stable from construction.</summary>
  private readonly ShellLifetime _lifetime = new();

  /// <inheritdoc />
  public IShellLifetime Lifetime => _lifetime;

  /// <summary>Creates the shell with the optional startup intent this shell's dispatch starts from.</summary>
  protected ShellBase(IActivationIntent? startupIntent = null)
  {
    StartupIntent = startupIntent;
  }

  /// <summary>
  ///   Assembles the shell's container: transitions to
  ///   <see cref="ShellLifecycle.Assembling"/> (idempotent), runs
  ///   <see cref="InitializeServices"/> and cuts the window scope from the
  ///   returned provider.
  /// </summary>
  [MemberNotNull(nameof(ShellServiceScope))]
  protected void EnsureAssembled()
  {
    if (ShellServiceScope != null)
      return; // already assembled (Start or the constructor path ran it)

    if (_lifetime.Lifecycle == ShellLifecycle.Disposed)
    {
      throw new InvalidOperationException(
        "The shell is disposed — its container and resolution face are gone.");
    }

    _lifetime.Advance(ShellLifecycle.Assembling);
    AssembleShell();
    if (ShellServiceScope is null)
    {
      throw new InvalidOperationException(
        "Shell not assembled — InitializeServices must return the shell's own provider " +
        "(build a ServiceProvider over your registrations and return it).");
    }
  }

  private IServiceProvider RequireShellServiceProvider()
  {
    if (ShellServiceScope is { } scope)
      return scope.ServiceProvider;

    throw new InvalidOperationException(
      "The shell is not assembled — call Start (or EnsureAssembled) before resolving services.");
  }
}
