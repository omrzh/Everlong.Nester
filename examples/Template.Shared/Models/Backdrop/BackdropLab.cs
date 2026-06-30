using CommunityToolkit.Mvvm.ComponentModel;

namespace NesterApp.Backdrop;

/// <summary>The backdrop render path.</summary>
public enum BackdropMode
{
  /// <summary>One element draws base, veils and motes.</summary>
  Vector,

  /// <summary>One element draws base and veils; each mote is a child visual.</summary>
  Visual,
}

/// <summary>Tunable backdrop parameters and the live frame statistics.</summary>
public sealed partial class BackdropLab : ObservableObject
{
  /// <summary>The default mote count.</summary>
  public const int DefaultMotes = 220;

  /// <summary>The default veil count.</summary>
  public const int DefaultVeils = 4;

  /// <summary>The default motion speed multiplier.</summary>
  public const double DefaultSpeed = 1.0;

  /// <summary>The default translucent-surface alpha.</summary>
  public const double DefaultPanelOpacity = 0.72;

  /// <summary>Applies the small-surface preset: three veils, seventy motes, just over half speed.</summary>
  public void UseLoginPreset()
  {
    VeilCount = 3;
    MoteCount = 70;
    Speed = 0.55;
  }

  /// <summary>Live frame statistics of the backdrop loop.</summary>
  public FrameStats Stats { get; } = new();

  /// <summary>Every render path, for pickers.</summary>
  public static BackdropMode[] Modes { get; } = Enum.GetValues<BackdropMode>();

  /// <summary>The active render path.</summary>
  [ObservableProperty]
  private BackdropMode _mode = BackdropMode.Vector;

  /// <summary>The resolved mote count.</summary>
  [ObservableProperty]
  private int _moteCount = DefaultMotes;

  /// <summary>The resolved veil count.</summary>
  [ObservableProperty]
  private int _veilCount = DefaultVeils;

  /// <summary>The motion speed multiplier.</summary>
  [ObservableProperty]
  private double _speed = DefaultSpeed;

  /// <summary>The alpha applied to the shell's translucent surfaces.</summary>
  [ObservableProperty]
  private double _panelOpacity = DefaultPanelOpacity;

  /// <summary>Whether pointer position feeds the parallax offset.</summary>
  [ObservableProperty]
  private bool _parallax = true;

  /// <summary>Whether the field stops advancing.</summary>
  [ObservableProperty]
  private bool _paused;

  /// <summary>Restores every parameter to its default.</summary>
  public void Reset()
  {
    Mode = BackdropMode.Vector;
    MoteCount = DefaultMotes;
    VeilCount = DefaultVeils;
    Speed = DefaultSpeed;
    PanelOpacity = DefaultPanelOpacity;
    Parallax = true;
    Paused = false;
  }
}
