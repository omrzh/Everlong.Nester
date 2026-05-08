namespace Everlong.Nester.Activation;

/// <summary>
///   The current process's startup arguments (excluding the executable
///   path) — the activation domain's own args channel, consistent with
///   <c>AppLifetime.Args</c>.
/// </summary>
internal static class ActivationArgs
{
  /// <summary>The current process's startup arguments, excluding the executable path.</summary>
  public static IReadOnlyList<string> Current
  {
    get
    {
      string[] args = Environment.GetCommandLineArgs();
      return args.Length <= 1 ? [] : args[1..];
    }
  }
}
