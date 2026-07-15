using System;
using System.Diagnostics;

namespace Fulcrum;

// A Tick can represent both an instant in time, as well as a duration
public readonly struct Tick : IEquatable<Tick>, IComparable<Tick>
{
    public readonly long Frame;

    public Tick() { Frame = Now(); }
    // CAUTION - if you aren't careful with types you might mix up units!
    public Tick(long ms) { Frame = MsToTicks(ms); }
    public Tick(float sec) { Frame = MsToTicks(sec * 1000f); }

    public static double DurationMs(long start, long end) => 1000.0 * (end - start) / Stopwatch.Frequency;
    public static double DurationMs(long length) => 1000.0 * length / Stopwatch.Frequency;
    public static long MsToTicks(long ms) => ms * Stopwatch.Frequency / 1000;
    public static long MsToTicks(float ms) => (long)(ms * Stopwatch.Frequency / 1000.0f);
    public static long MsToTicks(double ms) => (long)(ms * Stopwatch.Frequency / 1000.0);
    public static long Now() => Stopwatch.GetTimestamp();

    public static long TicksToMs(long ticks) => ticks * 1000 / Stopwatch.Frequency;

    // --- Time accessors ---

    /// <summary>
    /// Converts this tick to seconds as a float.
    /// </summary>
    public float Seconds
        => (float)(Frame * 1000.0 / Stopwatch.Frequency) / 1000f;

    /// <summary>
    /// Converts this tick to seconds as a double.
    /// </summary>
    public double SecondsDouble
        => Frame * 1000.0 / Stopwatch.Frequency / 1000.0;

    /// <summary>
    /// Converts this tick to milliseconds as a float.
    /// </summary>
    public long Milliseconds
        => (long)(Frame * 1000.0 / Stopwatch.Frequency);

    // --- Derived Tick operations ---

    /// <summary>
    /// Creates a new Tick representing the delta (difference) between this tick and another.
    /// </summary>
    public Tick Delta(Tick other)
        => new Tick(other.Frame - Frame);

    /// <summary>
    /// Creates a new Tick offset by the given number of milliseconds.
    /// </summary>
    public Tick OffsetMs(float ms)
        => new Tick(Frame + MsToTicks(ms));

    /// <summary>
    /// Creates a new Tick offset by the given number of seconds.
    /// </summary>
    public Tick OffsetSec(float sec)
        => new Tick(Frame + MsToTicks(sec * 1000f));


    public bool LessThanZero() => Frame < 0;


    public static Tick operator +(Tick a, Tick b) => new Tick(a.Frame + b.Frame);
    public static Tick operator -(Tick a, Tick b) => new Tick(a.Frame - b.Frame);
    public static Tick operator *(Tick a, float b) => new Tick((long)(a.Frame * b));
    public static Tick operator *(Tick a, double b) => new Tick((long)(a.Frame * b));
    public static Tick operator /(Tick a, float b) => b != 0 ? new Tick((long)(a.Frame / b)) : a;
    public static Tick operator /(Tick a, double b) => b != 0 ? new Tick((long)(a.Frame / b)) : a;

    public static Tick operator -(Tick a) => new Tick(-a.Frame);

    public static bool operator ==(Tick a, Tick b) => a.Frame == b.Frame;
    public static bool operator !=(Tick a, Tick b) => a.Frame != b.Frame;
    public static bool operator <(Tick a, Tick b) => a.Frame < b.Frame;
    public static bool operator <=(Tick a, Tick b) => a.Frame <= b.Frame;
    public static bool operator >(Tick a, Tick b) => a.Frame > b.Frame;
    public static bool operator >=(Tick a, Tick b) => a.Frame >= b.Frame;

    public bool Equals(Tick other) => Frame == other.Frame;
    public override bool Equals(object obj) => obj is Tick other && Equals(other);
    public override int GetHashCode() => Frame.GetHashCode();

    public int CompareTo(Tick other) => Frame.CompareTo(other.Frame);

    public override string ToString() => $"Tick({Frame})";

    public static Tick Zero = new Tick(0);
    public static Tick TimeImmemorial = new Tick(long.MinValue);
    public static Tick EndOfDays = new Tick(long.MaxValue);
}
