namespace Everlong.Nester.Activation;

/// <summary>
///   The framework's conservative args heuristics: absolute URIs become
///   <see cref="UriActivationIntent" />, existing file or directory paths
///   become <see cref="FileActivationIntent" />; everything else is left
///   unrecognized (it stays in the startup head's raw args).
/// </summary>
/// <remarks>
///   The OS validates schemes and file associations for runtime activation
///   events, but command-line input has no such validation — a mistyped
///   path must not be swallowed into a URI intent.  File-path shapes are
///   therefore excluded from URI detection (Windows drive roots, UNC
///   paths, and root-relative Unix paths), and existing paths win over URI
///   parsing.
/// </remarks>
public sealed class DefaultActivationArgsConverter
{
  /// <summary>Converts command-line args into activation intents via the conservative heuristics.</summary>
  public IReadOnlyList<IActivationIntent> Convert(IReadOnlyList<string> args)
  {
    ArgumentNullException.ThrowIfNull(args);

    List<IActivationIntent>? intents = null;
    List<string>? files = null;

    foreach (string arg in args)
    {
      if (string.IsNullOrWhiteSpace(arg))
        continue;

      if (LooksLikeFilePath(arg) && File.Exists(arg) || Directory.Exists(arg))
      {
        (files ??= []).Add(arg);
        continue;
      }

      if (Uri.TryCreate(arg, UriKind.Absolute, out var uri) && !LooksLikeFilePath(arg))
      {
        (intents ??= []).Add(new UriActivationIntent(uri));
      }
    }

    if (files is not null)
      (intents ??= []).Add(new FileActivationIntent(files));

    return intents ?? [];
  }

  /// <summary>Detects file-path shapes that must never be misread as URIs.</summary>
  private static bool LooksLikeFilePath(string arg)
    => arg.Length >= 2
       && (arg[1] == ':' && char.IsLetter(arg[0])        // Windows drive root: C:\...
           || arg.StartsWith(@"\\", StringComparison.Ordinal)  // UNC
           || arg[0] == '/');                            // root-relative Unix
}
