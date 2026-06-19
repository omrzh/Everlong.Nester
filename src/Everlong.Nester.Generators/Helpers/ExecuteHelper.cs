using Microsoft.CodeAnalysis;
namespace Everlong.Nester.Generators.Helpers;

public static class ExecuteHelper
{
  public static void Execute<T>(
    SourceProductionContext context,
    T value,
    Action<SourceProductionContext, T> action)
  {
    try
    {
      action(context, value);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch (Exception ex)
    {
      context.ReportDiagnostic(
        Diagnostic.Create(Descriptors.ExecutionError, Location.None, typeof(T).Name, ex.Message, ex.StackTrace));
    }
  }
}
