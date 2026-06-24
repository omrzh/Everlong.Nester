namespace Everlong.Nester.Auth;

/// <summary>
///   Parses authorization rule strings into structured policy and role components.
/// </summary>
public static class AuthStringParser
{
  /// <summary>
  ///   Parses an authorization rule string into its policy and roles components.
  /// </summary>
  /// <param name="s">
  ///   The rule string. Supports formats:
  ///   <list type="bullet">
  ///     <item><c>""</c> — authenticated only (both outputs null)</item>
  ///     <item><c>"PolicyName"</c> — policy only (positional)</item>
  ///     <item><c>"PolicyName; Role1,Role2"</c> — policy and roles (positional)</item>
  ///     <item><c>"Policy=PolicyName"</c> or <c>"P=PolicyName"</c> — named policy</item>
  ///     <item><c>"Roles=Role1,Role2"</c> or <c>"R=Role1,Role2"</c> — named roles</item>
  ///   </list>
  /// </param>
  /// <returns>A tuple of <c>(Policy, Roles)</c> strings, either of which may be <c>null</c>.</returns>
  public static (string? Policy, string? Roles) ParseAuthInfo(string? s)
  {
    if (string.IsNullOrWhiteSpace(s))
    {
      return (null, null);
    }

    string? policy = null;
    string? roles = null;
    int positionalIndex = 0;

    ReadOnlySpan<char> span = s.AsSpan();

    while (!span.IsEmpty)
    {
      int separatorIndex = span.IndexOf(';');
      ReadOnlySpan<char> segment;

      if (separatorIndex < 0)
      {
        segment = span;
        span = default;
      }
      else
      {
        segment = span.Slice(0, separatorIndex);
        span = span.Slice(separatorIndex + 1);
      }

      segment = segment.Trim();
      if (segment.IsEmpty)
      {
        continue;
      }

      int equalIndex = segment.IndexOf('=');
      if (equalIndex >= 0)
      {
        ReadOnlySpan<char> key = segment[..equalIndex].Trim();
        ReadOnlySpan<char> val = segment[(equalIndex + 1)..].Trim();

        if (key.Equals("Policy", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("P", StringComparison.OrdinalIgnoreCase))
        {
          policy = val.ToString();
        }
        else if (key.Equals("Roles", StringComparison.OrdinalIgnoreCase) ||
                 key.Equals("R", StringComparison.OrdinalIgnoreCase))
        {
          roles = val.ToString();
        }
      }
      else
      {
        if (positionalIndex == 0)
        {
          policy = segment.ToString();
        }
        else if (positionalIndex == 1)
        {
          roles = segment.ToString();
        }

        positionalIndex++;
      }
    }

    return (policy, roles);
  }
}
