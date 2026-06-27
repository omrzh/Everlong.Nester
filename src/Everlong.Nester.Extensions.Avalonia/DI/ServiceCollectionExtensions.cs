using Everlong.Nester.Notice;
// NOTE: Single-source file — the Extensions WPF project compiles this exact
// file via <Compile Include> in Everlong.Nester.Extensions.Wpf.csproj.
// Edit it here only; never create a WPF-side copy (the two builds would drift).
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Everlong.Nester.DI;

/// <summary>
///   DI registration for the notice domain.
/// </summary>
public static partial class ServiceCollectionExtensions
{
  /// <param name="services">The service collection.</param>
  extension(IServiceCollection services)
  {
    /// <summary>
    ///   Registers the Notice domain (toast / snackbar / notification) —
    ///   one tenant per window, registered under the aggregate
    ///   <see cref="INoticeService" /> and each per-kind surface.
    /// </summary>
    /// <param name="options">The notice options; <see langword="null" /> uses the defaults.</param>
    public IServiceCollection AddNesterNotice(NoticeServiceOptions? options = null)
    {
      // The notice tenant is window-wide (one per window): it rents the
      // notice layer from the window's stage broker.  Singleton — every
      // scope (window and layer-internal) resolves the same tenant, so it
      // can never rent the band twice.
      options ??= new NoticeServiceOptions();
      services.TryAddSingleton(options);
      services.AddSingleton<NoticeService>();
      services.AddSingleton<IToastService>(sp => sp.GetRequiredService<NoticeService>());
      services.AddSingleton<ISnackbarService>(sp => sp.GetRequiredService<NoticeService>());
      services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<NoticeService>());
      services.AddSingleton<INoticeService>(sp => sp.GetRequiredService<NoticeService>());
      return services;
    }
  }
}
