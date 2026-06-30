namespace NesterApp.Backdrop;

/// <summary>An 8-bit RGBA colour.</summary>
public readonly record struct Rgba(byte R, byte G, byte B, byte A);

/// <summary>One veil resolved into device-independent units.</summary>
public readonly record struct VeilSample(double X, double Y, double RadiusX, double RadiusY, Rgba Color, double Alpha);

/// <summary>One mote resolved into device-independent units.</summary>
public readonly record struct MoteSample(double X, double Y, double Radius, Rgba Color, double Alpha);

/// <summary>
///   A deterministic field of drifting veils and motes.  The field owns the
///   motion, the palette and the parallax damping; it resolves geometry in
///   device-independent units and holds no platform type.
/// </summary>
public sealed class AuroraField
{
  /// <summary>The largest supported veil count.</summary>
  public const int MaxVeils = 6;

  /// <summary>The largest supported mote count.</summary>
  public const int MaxMotes = 2000;

  private const double Tau = Math.PI * 2;
  private const double VeilSaturation = 0.78;
  private const double VeilLightness = 0.60;

  private static readonly double[] VeilHues = [168, 258, 322, 205, 292, 142];

  private readonly Veil[] _veils = new Veil[MaxVeils];
  private readonly Mote[] _motes = new Mote[MaxMotes];
  private readonly VeilSample[] _veilSamples = new VeilSample[MaxVeils];
  private readonly MoteSample[] _moteSamples = new MoteSample[MaxMotes];

  private double _time;
  private double _width = 1;
  private double _height = 1;
  private double _targetX;
  private double _targetY;
  private double _parallaxX;
  private double _parallaxY;
  private bool _dark = true;
  private int _veilCount = 4;
  private int _moteCount = 220;

  /// <summary>Creates the field; the seed selects the deterministic layout.</summary>
  public AuroraField(int seed = 20260911)
  {
    var rng = new Rng(seed);
    for (int i = 0; i < MaxVeils; i++)
    {
      _veils[i] = new Veil
      {
        FreqX = 0.009 + rng.Next() * 0.021,
        FreqY = 0.008 + rng.Next() * 0.019,
        PhaseX = rng.Next(),
        PhaseY = rng.Next(),
        AmplitudeX = 0.24 + rng.Next() * 0.20,
        AmplitudeY = 0.20 + rng.Next() * 0.20,
        BaseRadius = 0.42 + rng.Next() * 0.34,
        Breath = 0.10 + rng.Next() * 0.16,
        BreathFreq = 0.013 + rng.Next() * 0.021,
        BreathPhase = rng.Next(),
        HueDrift = 14 + rng.Next() * 22,
        HueFreq = 0.005 + rng.Next() * 0.011,
        HuePhase = rng.Next(),
        Alpha = 0.24 + rng.Next() * 0.16,
      };
    }

    for (int i = 0; i < MaxMotes; i++)
    {
      _motes[i] = new Mote
      {
        Nx = rng.Next(),
        Ny = rng.Next(),
        Depth = rng.Next(),
        Drift = 0.006 + rng.Next() * 0.020,
        SwayAmplitude = 0.4 + rng.Next() * 1.2,
        SwayFreq = 0.05 + rng.Next() * 0.18,
        SwayPhase = rng.Next(),
        TwinkleFreq = 0.12 + rng.Next() * 0.55,
        TwinklePhase = rng.Next(),
      };
    }
  }

  /// <summary>The resolved veils for the current frame.</summary>
  public ReadOnlySpan<VeilSample> Veils => _veilSamples.AsSpan(0, _veilCount);

  /// <summary>The resolved motes for the current frame.</summary>
  public ReadOnlySpan<MoteSample> Motes => _moteSamples.AsSpan(0, _moteCount);

  /// <summary>The resolved veil count.</summary>
  public int VeilCount
  {
    get => _veilCount;
    set => _veilCount = Math.Clamp(value, 0, MaxVeils);
  }

  /// <summary>The resolved mote count.</summary>
  public int MoteCount
  {
    get => _moteCount;
    set => _moteCount = Math.Clamp(value, 0, MaxMotes);
  }

  /// <summary>Whether the dark palette is active.</summary>
  public bool DarkTheme
  {
    get => _dark;
    set => _dark = value;
  }

  /// <summary>The top stop of the base wash.</summary>
  public Rgba BaseTop => _dark ? new Rgba(6, 8, 15, 255) : new Rgba(248, 250, 255, 255);

  /// <summary>The bottom stop of the base wash.</summary>
  public Rgba BaseBottom => _dark ? new Rgba(11, 18, 32, 255) : new Rgba(232, 238, 250, 255);

  /// <summary>The mote tint.</summary>
  public Rgba MoteColor => _dark ? new Rgba(228, 238, 255, 255) : new Rgba(58, 74, 120, 255);

  /// <summary>Sets the viewport the field resolves against, in device-independent units.</summary>
  public void Resize(double width, double height)
  {
    _width = Math.Max(1, width);
    _height = Math.Max(1, height);
  }

  /// <summary>Sets the pointer position normalized to [-1, 1] on both axes.</summary>
  public void SetPointer(double x, double y)
  {
    _targetX = Math.Clamp(x, -1, 1);
    _targetY = Math.Clamp(y, -1, 1);
  }

  /// <summary>Stops feeding the pointer into the parallax offset.</summary>
  public void ClearPointer()
  {
    _targetX = 0;
    _targetY = 0;
  }

  /// <summary>
  ///   Advances the field by <paramref name="seconds" /> and resolves every
  ///   sample; a non-positive step resolves the current state without moving it.
  /// </summary>
  public void Advance(double seconds)
  {
    if (seconds > 0)
    {
      _time += seconds;

      double damping = Math.Min(1, seconds * 3.2);
      _parallaxX += (_targetX - _parallaxX) * damping;
      _parallaxY += (_targetY - _parallaxY) * damping;
    }

    for (int i = 0; i < _veilCount; i++)
      ResolveVeil(i);

    for (int i = 0; i < _moteCount; i++)
      ResolveMote(i, seconds);
  }

  private void ResolveVeil(int index)
  {
    ref readonly Veil veil = ref _veils[index];
    double radius = veil.BaseRadius * Math.Max(_width, _height)
                    * (1 + veil.Breath * Math.Sin(Tau * veil.BreathFreq * _time + veil.BreathPhase));
    double hue = VeilHues[index % VeilHues.Length]
                 + veil.HueDrift * Math.Sin(Tau * veil.HueFreq * _time + veil.HuePhase);

    _veilSamples[index] = new VeilSample(
      _width * (0.5 + veil.AmplitudeX * Math.Sin(Tau * veil.FreqX * _time + veil.PhaseX)),
      _height * (0.5 + veil.AmplitudeY * Math.Sin(Tau * veil.FreqY * _time + veil.PhaseY)),
      radius,
      radius * 0.78,
      FromHsl(hue, VeilSaturation, _dark ? VeilLightness : 0.74),
      veil.Alpha * (_dark ? 1.0 : 0.92));
  }

  private void ResolveMote(int index, double seconds)
  {
    ref Mote mote = ref _motes[index];
    double depth = mote.Depth;

    mote.Ny -= mote.Drift * seconds;
    if (mote.Ny < -0.03)
      mote.Ny += 1.06;

    double depthScale = 0.25 + 0.75 * depth;
    double sway = Math.Sin(Tau * mote.SwayFreq * _time + mote.SwayPhase) * mote.SwayAmplitude * (4 + 26 * depth);
    double twinkle = 0.42 + 0.58 * (0.5 + 0.5 * Math.Sin(Tau * mote.TwinkleFreq * _time + mote.TwinklePhase));

    _moteSamples[index] = new MoteSample(
      mote.Nx * _width + sway + _parallaxX * depthScale * 34,
      mote.Ny * _height + _parallaxY * depthScale * 22,
      0.65 + 2.7 * depth,
      MoteColor,
      (0.14 + 0.62 * depth) * twinkle);
  }

  /// <summary>Converts HSL (hue in degrees, saturation/lightness in [0, 1]) to opaque RGBA.</summary>
  private static Rgba FromHsl(double hue, double saturation, double lightness)
  {
    double h = ((hue % 360) + 360) % 360 / 60.0;
    double c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
    double x = c * (1 - Math.Abs(h % 2 - 1));
    double m = lightness - c / 2;

    (double r, double g, double b) = (int)h switch
    {
      0 => (c, x, 0d),
      1 => (x, c, 0d),
      2 => (0d, c, x),
      3 => (0d, x, c),
      4 => (x, 0d, c),
      _ => (c, 0d, x),
    };

    return new Rgba(
      (byte)Math.Clamp((r + m) * 255, 0, 255),
      (byte)Math.Clamp((g + m) * 255, 0, 255),
      (byte)Math.Clamp((b + m) * 255, 0, 255),
      255);
  }

  private struct Veil
  {
    public double FreqX, FreqY, PhaseX, PhaseY, AmplitudeX, AmplitudeY;
    public double BaseRadius, Breath, BreathFreq, BreathPhase;
    public double HueDrift, HueFreq, HuePhase, Alpha;
  }

  private struct Mote
  {
    public double Nx, Ny, Depth, Drift;
    public double SwayAmplitude, SwayFreq, SwayPhase;
    public double TwinkleFreq, TwinklePhase;
  }

  /// <summary>A deterministic 32-bit linear congruential generator.</summary>
  private struct Rng(int seed)
  {
    private uint _state = (uint)seed | 1u;

    public double Next()
    {
      _state = _state * 1664525u + 1013904223u;
      return (_state >> 8) * (1.0 / 16777216.0);
    }
  }
}
