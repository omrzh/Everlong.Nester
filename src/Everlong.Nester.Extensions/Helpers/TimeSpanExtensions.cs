namespace Everlong.Nester.Helpers;

/// <summary>Time-span conveniences over <see cref="int"/> counts.</summary>
public static class TimeSpanExtensions
{
  extension(int number)
  {
    /// <summary>The count as a time span in milliseconds.</summary>
    public TimeSpan Milliseconds => TimeSpan.FromMilliseconds(number);

    /// <summary>The count as a time span in minutes.</summary>
    public TimeSpan Minutes => TimeSpan.FromMinutes(number);
  }
}
