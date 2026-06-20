namespace Everlong.Nester.Generators.Helpers;

using System;
using System.Diagnostics.CodeAnalysis;

internal static class Guard
{
  public static T NotNull<T>(
    [NotNull] T? value,
    string? paramName = null)
    where T : class
  {
    return value ?? throw new ArgumentNullException(paramName);
  }

  public static string NotNullOrEmpty(
    [NotNull] string? value,
    string? paramName = null)
  {
    if (value is null || value.Length == 0)
    {
      throw new ArgumentException("String cannot be null or empty.", paramName);
    }
    return value;
  }

  public static bool TryNotNull<T>(
    T? value,
    [NotNullWhen(true)] out T? result)
    where T : class
  {
    if (value is null)
    {
      result = null;
      return false;
    }

    result = value;
    return true;
  }

  public static bool TryNotNullOrEmpty(
    string? value,
    [NotNullWhen(true)] out string? result)
  {
    if (string.IsNullOrEmpty(value))
    {
      result = null;
      return false;
    }

    result = value!;
    return true;
  }
}
