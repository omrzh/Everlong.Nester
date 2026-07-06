using Everlong.Nester.Shell;

namespace Everlong.Nester.Tests.Hosting;

/// <summary>
///   The bare shell — the platform shell shape WITHOUT an assembly (no
///   Director / container / root view): tests that exercise the broker /
///   stage / dispatch surfaces directly without assembling a shell
///   (<c>AvaloniaShell</c> is abstract since the assembly
///   <c>InitializeServices</c> became mandatory — this minimal implementation is
///   the unassembled case; starting it fails fast at the null container,
///   which those tests rely on or never attempt).
/// </summary>
internal sealed class BareShell : AvaloniaShell
{
  protected override IServiceProvider InitializeServices()
  {
    // No assembly — intentionally: these tests drive the shell's bare
    // surfaces (broker ledger, stage, dispatch, teardown) directly.
    // Leaving RootProvider unassigned fails fast in EnsureAssembled with
    // a clear message.
    return null!;
  }
}
