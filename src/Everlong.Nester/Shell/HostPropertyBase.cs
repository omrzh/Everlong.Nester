using System.ComponentModel;
using System.Runtime.CompilerServices;
using Everlong.Nester.Primitives;

namespace Everlong.Nester.Shell;

/// <summary>
///   Standalone snapshot of the shell's window state, owned by the
///   <see cref="IShell" />.
/// </summary>
/// <remarks>
///   The state is raised through <see cref="INotifyPropertyChanged" /> on the
///   snapshot object itself, so bindings may subscribe directly.
/// </remarks>
public abstract class HostPropertyBase : INotifyPropertyChanged
{
  /// <summary><c>true</c> when this shell is the active foreground surface.</summary>
  public bool IsActive { get; set => SetField(ref field, value); }

  /// <summary>Gets a value indicating whether the shell is the topmost surface.</summary>
  public bool TopMost { get; set => SetField(ref field, value); }

  /// <summary>Gets the last known shell-state.</summary>
  public HostState LastHostState { get; set => SetField(ref field, value); }

  /// <summary>Gets the current shell-state.</summary>
  public HostState HostState { get; set => SetField(ref field, value); }

  /// <summary>Gets the shell title.</summary>
  public string Title { get; set => SetField(ref field, value); } = string.Empty;

  /// <summary>Gets the bounds of the shell host.</summary>
  public ShellBounds Bounds { get; set => SetField(ref field, value); }

  /// <summary>Gets a value indicating whether the shell is currently visible.</summary>
  public bool IsVisible { get; set => SetField(ref field, value); }

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
