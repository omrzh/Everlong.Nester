using Everlong.Nester.Diagnostics;
using Everlong.Nester.Intent;

namespace Everlong.Nester.Shell;

/// <summary>
///   The shell's decision maker: handles shell-level intents and errors.
/// </summary>
public interface IShellDirector : IIntentHandler, IErrorHandler
{
  /// <summary>Notifies the director that its shell is assembled and its services are live.</summary>
  /// <param name="shell">The assembled shell.</param>
  /// <remarks>The intent pipeline is not yet active — dispatches pass.</remarks>
  void OnAssembled(IShell shell);
}
