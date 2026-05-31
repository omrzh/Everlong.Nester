using Avalonia.Platform.Storage;

namespace Everlong.Nester.Activation;

/// <summary>
///   One or more storage items (files or folders) delivered by the OS file
///   activation event (Avalonia <c>FileActivatedEventArgs</c>).  The items are
///   platform objects — content access goes through platform APIs
///   (<c>IStorageFile.OpenReadAsync</c>, probing for folders).  Each item is
///   <see cref="System.IDisposable" />: the handling code owns their lifetime
///   and should dispose them when done.
/// </summary>
public sealed record StorageItemActivationIntent(IReadOnlyList<IStorageItem> Files) : IActivationIntent;
