namespace Everlong.Nester.Notice;

/// <summary>
///   One notice entry — the model a panel presents and dismisses.
/// </summary>
/// <remarks>
///   The panel resolves the entry to a view through the platform's template
///   table; the entry carries the state, the view carries the looks.
/// </remarks>
public interface INoticeEntry : IDismissable
{
}
