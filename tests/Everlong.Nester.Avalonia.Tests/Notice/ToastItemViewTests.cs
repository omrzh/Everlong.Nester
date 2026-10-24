using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Logging;
using Avalonia.Threading;
using Everlong.Nester.Notice;
using Everlong.Nester.Presentation;
using Xunit;

namespace Everlong.Nester.Tests.Notice;

/// <summary>
///   The shipped toast view resolves its level through
///   <see cref="Everlong.Nester.Converters.TypeToIconConverter" />,
///   which returns a geometry and ignores its parameter.  Binding that converter to a boolean property
///   logs a binding error on every toast, so the view must not ask it for anything but the icon.
/// </summary>
public class ToastItemViewTests
{
  [AvaloniaFact]
  public void RenderingAToast_LogsNoBindingError()
  {
    CollectingSink sink = new();
    ILogSink? previous = Logger.Sink;
    Logger.Sink = sink;

    try
    {
      foreach (ToastLevel level in Enum.GetValues<ToastLevel>())
      {
        var view = new ToastItemView
        {
          DataContext = new ToastEntry { Level = level, Message = $"probe {level}" }
        };
        var window = new Window { Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.Close();
      }
    }
    finally
    {
      Logger.Sink = previous;
    }

    Assert.DoesNotContain(sink.Entries,
                          entry => entry.Level >= LogEventLevel.Warning && entry.Area == LogArea.Binding);
  }

  private sealed class CollectingSink : ILogSink
  {
    public List<(LogEventLevel Level, string Area, string Template)> Entries { get; } = [];

    public bool IsEnabled(LogEventLevel level, string area) => true;

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
      => Entries.Add((level, area, messageTemplate));

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
                    params object?[] propertyValues)
      => Entries.Add((level, area, messageTemplate));
  }
}
