namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's stage — the visual surface of a shell.
/// </summary>
public interface IShellStage
{
  /// <summary>The owning shell — the shell that presents this stage.</summary>
  IShell Shell { get; }
}
