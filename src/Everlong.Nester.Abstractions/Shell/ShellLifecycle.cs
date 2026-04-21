namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's lifecycle state.  A shell walks <see cref="Created"/> → <see cref="Assembling"/> →
///   <see cref="Assembled"/> → <see cref="Started"/> → <see cref="Disposed"/> exactly once; teardown
///   may jump any state to <see cref="Disposed"/>.
/// </summary>
public enum ShellLifecycle
{
  /// <summary>
  ///   The shell object has been instantiated but has not started assembly yet.
  /// </summary>
  Created = 0,

  /// <summary>
  ///   The shell is building its infrastructure.
  /// </summary>
  Assembling = 1,

  /// <summary>
  ///   The shell's infrastructure is built and ready to resolve services.
  /// </summary>
  Assembled = 2,

  /// <summary>
  ///   The shell is started — the intent pipeline responds to dispatches.
  /// </summary>
  Started = 3,

  /// <summary>
  ///   The shell and its resources are disposed.
  /// </summary>
  Disposed = 4,
}
