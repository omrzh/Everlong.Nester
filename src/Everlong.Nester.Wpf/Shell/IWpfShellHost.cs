namespace Everlong.Nester.Shell;

/// <summary>
///   The WPF host of a shell's stage — the shell's window (WPF has no
///   single-view host) that mounts the shell's surface into its own visual
///   container.
/// </summary>
/// <remarks>
///   Called once at shell start, synchronously, before the host presents
///   itself.
/// </remarks>
public interface IWpfShellHost
{
  /// <summary>
  ///   Hosts the shell's stage — the content the shell presents — wiring it
  ///   into the host's own visual container.
  /// </summary>
  /// <param name="shell">The shell being hosted.</param>
  /// <param name="stage">The shell's stage, to be mounted into the host's visual container.</param>
  void HostShell(IWpfShell shell, PControl stage);
}
