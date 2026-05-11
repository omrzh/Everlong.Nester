using System.ComponentModel;

namespace Everlong.Nester.Routing;

abstract partial class RouterStack
{
  /// <inheritdoc />
  public bool CanGoBack => _index > 0;

  /// <inheritdoc />
  public bool CanGoForward => _index >= 0 && _index + 1 < _items.Count;

  /// <inheritdoc />
  public ILocation? Location => CurrentChain is { Length: > 0 } current ? current[^1] : null;

  /// <inheritdoc />
  public int Count => _items.Count;

  /// <summary>The cached event args naming the <see cref="Location" /> property.</summary>
  public static readonly PropertyChangedEventArgs LocationChangeArgs = new(nameof(Location));

  /// <summary>The cached event args naming the <see cref="CanGoBack" /> property.</summary>
  public static readonly PropertyChangedEventArgs CanGoBackChangeArgs = new(nameof(CanGoBack));

  /// <summary>The cached event args naming the <see cref="CanGoForward" /> property.</summary>
  public static readonly PropertyChangedEventArgs CanGoForwardChangeArgs = new(nameof(CanGoForward));

  /// <summary>The cached event args naming the <see cref="Count" /> property.</summary>
  public static readonly PropertyChangedEventArgs CountChangeArgs = new(nameof(Count));

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  /// <summary>Raises the surface's properties — the stack's site and traversal state changed.</summary>
  internal void RaiseNavigated()
  {
    PropertyChanged?.Invoke(this, LocationChangeArgs);
    PropertyChanged?.Invoke(this, CanGoBackChangeArgs);
    PropertyChanged?.Invoke(this, CanGoForwardChangeArgs);
    PropertyChanged?.Invoke(this, CountChangeArgs);
  }
}
