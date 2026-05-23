using Everlong.Nester.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using Everlong.Nester.Extensions.Properties;
using Microsoft.Extensions.DependencyInjection;

using Everlong.Nester.Routing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Everlong.DI;
using Everlong.Nester.Notice;

namespace Everlong.Nester.Dialog;

/// <summary>
///   Options for <see cref="Ipv4ComposerDialogSession" />.
/// </summary>
public sealed class Ipv4ComposerOptions : DialogOptions
{
  /// <summary>Gets or sets the initial IP address.</summary>
  public IPAddress? InitialIpAddress { get; set; }

  /// <summary>Gets or sets the initial IP address as a string.</summary>
  public string? InitialIpAddressString { get; set; }

  /// <summary>Gets or sets the override for the title text.</summary>
  public string Title { get; init; } = Lang.Dialog.Ipv4.Title;

  /// <summary>Gets or sets the override for the message text.</summary>
  public string Message { get; init; } = Lang.Dialog.Ipv4.Message;

  /// <summary>Gets or sets the override for the confirm button text.</summary>
  public string ConfirmText { get; init; } = Lang.Dialog.Ipv4.ConfirmText;

  /// <summary>Gets or sets the override for the clear button text.</summary>
  public string ClearText { get; init; } = Lang.Dialog.Ipv4.ClearText;

  /// <summary>Gets or sets the override for the ping button text.</summary>
  public string PingText { get; init; } = Lang.Dialog.Ipv4.PingText;

  internal byte[]? GetAddressBytes()
  {
    if (InitialIpAddress != null)
    {
      return InitialIpAddress.GetAddressBytes();
    }

    if (InitialIpAddressString != null &&
        IPAddress.TryParse(InitialIpAddressString, out IPAddress? ipAddress))
    {
      return ipAddress.GetAddressBytes();
    }

    return null;
  }
}

/// <summary>
///   Represents a dialog session for selecting an IPv4 address using a numpad interface.
/// </summary>
public partial class Ipv4ComposerDialogSession : DialogSessionBase<IPAddress?>, IInjectable
{
  /// <summary>Creates a session with empty address octets.</summary>
  public Ipv4ComposerDialogSession() { }

  internal INoticeService ToastService { get; private set; } = NullNoticeService.Instance;

  /// <summary>
  ///   Gets or sets the heading shown above the address.
  /// </summary>
  [ObservableProperty] public partial string? Title { get; set; }

  /// <summary>
  ///   Gets or sets the guidance text shown below the heading.
  /// </summary>
  [ObservableProperty] public partial string? Message { get; set; }

  /// <summary>
  ///   Gets or sets the text for the confirm button.
  /// </summary>
  [ObservableProperty] public required partial string ConfirmText { get; set; }

  /// <summary>
  ///   Gets or sets the text for the clear button.
  /// </summary>
  [ObservableProperty] public required partial string ClearText { get; set; }

  /// <summary>
  ///   Gets or sets the text for the ping button.
  /// </summary>
  [ObservableProperty] public required partial string PingText { get; set; }

  /// <summary>
  ///   Gets or sets the first octet of the IPv4 address (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(CanPing))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  [NotifyCanExecuteChangedFor(nameof(PingCommand))]
  public partial string Part1 { get; set; } = "";

  /// <summary>
  ///   Gets or sets the second octet of the IPv4 address (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(CanPing))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  [NotifyCanExecuteChangedFor(nameof(PingCommand))]
  public partial string Part2 { get; set; } = "";

  /// <summary>
  ///   Gets or sets the third octet of the IPv4 address (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(CanPing))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  [NotifyCanExecuteChangedFor(nameof(PingCommand))]
  public partial string Part3 { get; set; } = "";

  /// <summary>
  ///   Gets or sets the fourth octet of the IPv4 address (0-255).
  /// </summary>
  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(DisplayValue))]
  [NotifyPropertyChangedFor(nameof(CanPing))]
  [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
  [NotifyCanExecuteChangedFor(nameof(PingCommand))]
  public partial string Part4 { get; set; } = "";

  /// <summary>
  ///   Gets or sets the index of the currently active part (0-3) being edited.
  /// </summary>
  [ObservableProperty]
  public partial int ActivePartIndex { get; set; } = 0;

  [ObservableProperty]
  [NotifyPropertyChangedFor(nameof(CanPing))]
  [NotifyCanExecuteChangedFor(nameof(PingCommand))]
  public partial bool IsPinging { get; set; }

  /// <summary>
  ///   Gets the formatted display value of the IP address.
  /// </summary>
  public string DisplayValue =>
    $"{Part1.PadLeft(3, ' ')}.{Part2.PadLeft(3, ' ')}.{Part3.PadLeft(3, ' ')}.{Part4.PadLeft(3, ' ')}";

  /// <summary>
  ///   Gets a value indicating whether the current IPv4 input can start a ping test.
  /// </summary>
  public bool CanPing => !IsPinging && CanConfirm();

  /// <summary>Gets the tooltip of the backspace key.</summary>
  public string BackspaceToolTip => Lang.Dialog.Ipv4.BackspaceToolTip;

  /// <inheritdoc />
  public virtual void Inject(IServiceProvider services)
  {
    ToastService = services.GetRequiredService<INoticeService>();
  }

  /// <summary>
  ///   Appends a key to the current active part.
  /// </summary>
  /// <param name="key">The key to append (0-9 or .).</param>
  [RelayCommand]
  private void AppendKey(string key)
  {
    if (key == ".")
    {
      if (ActivePartIndex < 3)
      {
        ActivePartIndex++;
      }

      return;
    }

    string currentPart = GetPart(ActivePartIndex);
    string newPart = currentPart + key;

    if (newPart.Length > 3)
    {
      return;
    }

    if (int.TryParse(newPart, out int value) && value <= 255)
    {
      SetPart(ActivePartIndex, newPart);

      if (ShouldAutoAdvance(newPart) && ActivePartIndex < 3)
      {
        ActivePartIndex++;
      }
    }
  }

  /// <summary>
  ///   Removes the last character from the current active part or moves to the previous part.
  /// </summary>
  [RelayCommand]
  private void Backspace()
  {
    while (true)
    {
      string currentPart = GetPart(ActivePartIndex);

      if (string.IsNullOrEmpty(currentPart))
      {
        if (ActivePartIndex > 0)
        {
          ActivePartIndex--;
          // Recursively delete from previous part? No, standard behavior is just move focus or delete last char of prev part.
          // Let's delete last char of previous part for smoother editing.
          continue;
        }
      }
      else
      {
        SetPart(ActivePartIndex, currentPart[..^1]);
      }

      break;
    }
  }

  /// <summary>
  ///   Clears all parts and resets the active index.
  /// </summary>
  [RelayCommand]
  private void Clear()
  {
    Part1 = "";
    Part2 = "";
    Part3 = "";
    Part4 = "";
    ActivePartIndex = 0;
  }

  [RelayCommand(CanExecute = nameof(CanPing))]
  private async Task PingAsync()
  {
    IPAddress? ipAddress = CreateCurrentIpAddress();
    if (ipAddress is null)
    {
      return;
    }

    IsPinging = true;
    using Ping ping = new();

    try
    {
      PingReply reply = await ping.SendPingAsync(ipAddress, 3000).ConfigureAwait(true);
      if (reply.Status == IPStatus.Success)
      {
        ToastService.Toast(Lang.Dialog.Ipv4.FormatPingSuccess(reply.RoundtripTime), ToastLevel.Success);
        return;
      }

      ToastService.Toast(Lang.Dialog.Ipv4.FormatPingFailed(GetPingFailureReason(reply.Status)), ToastLevel.Error);
    }
    catch (PingException)
    {
      ToastService.Toast(Lang.Dialog.Ipv4.PingUnavailableError, ToastLevel.Error);
    }
    catch (Exception)
    {
      ToastService.Toast(Lang.Dialog.Ipv4.PingUnavailableError, ToastLevel.Error);
    }
    finally
    {
      IsPinging = false;
    }
  }

  /// <summary>
  ///   Confirms the selection and closes the dialog.
  /// </summary>
  [RelayCommand(CanExecute = nameof(CanConfirm))]
  private void Confirm()
  {
    string ip = $"{GetPartValue(Part1)}.{GetPartValue(Part2)}.{GetPartValue(Part3)}.{GetPartValue(Part4)}";
    Close(IPAddress.Parse(ip));
  }

  /// <summary>
  ///   Sets the active part index.
  /// </summary>
  /// <param name="index">The index to set (0-3).</param>
  [RelayCommand]
  private void SetActivePart(string? index)
  {
    if (!int.TryParse(index, out int parsedIndex))
    {
      return;
    }

    if (parsedIndex is >= 0 and <= 3)
    {
      ActivePartIndex = parsedIndex;
    }
  }

  private bool CanConfirm()
  {
    return !string.IsNullOrEmpty(Part1) &&
           !string.IsNullOrEmpty(Part2) &&
           !string.IsNullOrEmpty(Part3) &&
           !string.IsNullOrEmpty(Part4);
  }

  private IPAddress? CreateCurrentIpAddress()
  {
    return CanConfirm()
             ? IPAddress.Parse(
               $"{GetPartValue(Part1)}.{GetPartValue(Part2)}.{GetPartValue(Part3)}.{GetPartValue(Part4)}")
             : null;
  }

  private string GetPart(int index)
  {
    return index switch
    {
      0 => Part1,
      1 => Part2,
      2 => Part3,
      3 => Part4,
      _ => ""
    };
  }

  private void SetPart(int index, string value)
  {
    switch (index)
    {
      case 0:
        Part1 = value;
        break;
      case 1:
        Part2 = value;
        break;
      case 2:
        Part3 = value;
        break;
      case 3:
        Part4 = value;
        break;
    }
  }

  private static string GetPartValue(string part)
  {
    return string.IsNullOrEmpty(part) ? "0" : part;
  }

  private static string GetPingFailureReason(IPStatus status)
  {
    return status switch
    {
      IPStatus.TimedOut => Lang.Dialog.Ipv4.PingTimedOutReason,
      IPStatus.DestinationHostUnreachable => Lang.Dialog.Ipv4.PingHostUnreachableReason,
      IPStatus.DestinationNetworkUnreachable => Lang.Dialog.Ipv4.PingNetworkUnreachableReason,
      IPStatus.DestinationProtocolUnreachable => Lang.Dialog.Ipv4.PingProtocolUnreachableReason,
      IPStatus.DestinationPortUnreachable => Lang.Dialog.Ipv4.PingPortUnreachableReason,
      IPStatus.PacketTooBig => Lang.Dialog.Ipv4.PingPacketTooBigReason,
      IPStatus.TtlExpired => Lang.Dialog.Ipv4.PingTtlExpiredReason,
      IPStatus.BadRoute => Lang.Dialog.Ipv4.PingBadRouteReason,
      _ => Lang.Dialog.Ipv4.PingUnknownReason
    };
  }

  private static bool ShouldAutoAdvance(string part)
  {
    if (part.Length >= 3)
    {
      return true;
    }

    for (int digit = 0; digit <= 9; digit++)
    {
      string candidate = part + digit;
      if (candidate.Length <= 3 && int.TryParse(candidate, out int value) && value <= 255)
      {
        return false;
      }
    }

    return true;
  }

  private sealed class NullNoticeService : INoticeService
  {
    public static readonly INoticeService Instance = new NullNoticeService();

    public void Toast(string message, ToastLevel level = ToastLevel.Information, TimeSpan? duration = null) { }
    public void Show(string message, string? actionText = null, TimeSpan? duration = null) { }
    public Task<SnackbarResult> ShowAsync(string message, string? actionText = null, TimeSpan? duration = null)
      => Task.FromResult(SnackbarResult.Dismissed);
    public void Notify(string title, string message, NotificationLevel level = NotificationLevel.Information,
                       TimeSpan? duration = null, string? actionText = null)
    { }
    public Task<NotificationResult> NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Information,
                                                TimeSpan? duration = null, string? actionText = null)
      => Task.FromResult(NotificationResult.Dismissed);
  }
}

public static partial class DialogSessionExtensions
{
  /// <summary>
  ///   Shows a dialog for composing an IPv4 address.
  /// </summary>
  /// <param name="router">The router.</param>
  /// <param name="composerOptions">Optional string overrides for labels and button text.</param>
  /// <returns>The selected IPv4 address, or <see langword="null" /> if canceled.</returns>
  public static Task<IPAddress?> ComposeIpv4Async(this IRouter router,
                                                  Ipv4ComposerOptions? composerOptions = null)
  {
    composerOptions ??= new Ipv4ComposerOptions();
    Ipv4ComposerDialogSession session = new()
    {
      Title = composerOptions.Title,
      Message = composerOptions.Message,
      ConfirmText = composerOptions.ConfirmText,
      ClearText = composerOptions.ClearText,
      PingText = composerOptions.PingText
    };

    if (composerOptions.GetAddressBytes() is { Length: 4 } bytes)
    {
      session.Part1 = bytes[0].ToString();
      session.Part2 = bytes[1].ToString();
      session.Part3 = bytes[2].ToString();
      session.Part4 = bytes[3].ToString();
    }

    return router.ShowAsync<IPAddress?>(session, composerOptions.ToDimmer());
  }
}
