namespace Everlong.Nester.Notice;

/// <summary>
///   Aggregates the toast, snackbar, and notification services.
/// </summary>
public interface INoticeService : IToastService, ISnackbarService, INotificationService;
