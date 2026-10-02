using System.ComponentModel;
using System.Runtime.CompilerServices;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   The observable snapshot of a host window's state.
/// </summary>
/// <remarks>
///   Raises <see cref="INotifyPropertyChanged" /> from the snapshot itself;
///   only a derived implementation can write it.
/// </remarks>
public abstract class HostPropertyBase : INotifyPropertyChanged
{
  /// <summary><c>true</c> when this shell is the active foreground surface.</summary>
  public bool IsActive { get; protected set => SetField(ref field, value); }

  /// <summary>Gets a value indicating whether the shell is the topmost surface.</summary>
  public bool TopMost { get; protected set => SetField(ref field, value); }

  /// <summary>Gets the last known shell-state.</summary>
  public HostState LastHostState { get; protected set => SetField(ref field, value); }

  /// <summary>Gets the current shell-state.</summary>
  public HostState HostState { get; protected set => SetField(ref field, value); }

  /// <summary>Gets the shell title.</summary>
  public string Title { get; protected set => SetField(ref field, value); } = string.Empty;

  /// <summary>Gets the bounds of the shell host.</summary>
  public Bounds Bounds { get; protected set => SetField(ref field, value); }

  /// <summary>Gets a value indicating whether the shell is currently visible.</summary>
  public bool IsVisible { get; protected set => SetField(ref field, value); }

  #region INPC

  /// <inheritdoc />
  public event PropertyChangedEventHandler? PropertyChanged;

  internal void OnPropertyChanged([CallerMemberName] string? name = null)
    => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

  internal bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
  {
    if (EqualityComparer<T>.Default.Equals(field, value))
      return false;
    field = value;
    OnPropertyChanged(name);
    return true;
  }

  #endregion
}
