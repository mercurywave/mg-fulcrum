
namespace Fulcrum;

// triggers an event after a condition has been true for a certain duration
public struct LatchTimer
{
    Tick _engagedSince = Tick.EndOfDays;
    public LatchTimer() { }

    public void Update(bool isEngaged)
    {
        var now = GCore.FrameStart;
        if (isEngaged && _engagedSince > now)
        {
            _engagedSince = now;
        }
        else if (!isEngaged)
            _engagedSince = Tick.EndOfDays;
    }
    public bool IsEngagedFor(Tick duration)
        => GCore.FrameStart - _engagedSince > duration;
    public bool IsEngagedFor(bool isEngaged, Tick duration)
    {
        Update(isEngaged);
        return IsEngagedFor(duration);
    }
    public Tick TimeEngaged()
        => GCore.FrameStart - _engagedSince;
    public float FadeIn(Tick duration)
    {
        // returns 0.0 if the latch has not been engaged for the duration, and 1.0 if it has been engaged for the duration
        var since = GCore.FrameStart - _engagedSince;
        if (since > duration) return 1f;
        return (float)since.Frame / (float)duration.Frame;
    }
    public void Reset()
    {
        _engagedSince = Tick.EndOfDays;
    }
}

// triggers an event once after a condition has been true for a certain duration, and then resets when the condition is false
public struct PulseTimer
{
    Tick _lastPulse = Tick.TimeImmemorial;
    public PulseTimer() { }
    
    public void Update(bool isEngaged)
    {
        var now = GCore.FrameStart;
        if (isEngaged && _lastPulse < now)
            _lastPulse = now;
    }

    public bool EngagedRecently(Tick duration)
    {
        var now = GCore.FrameStart;
        return (_lastPulse + duration) > now;
    }

    public Tick TimeSinceLastPulse()
        => GCore.FrameStart - _lastPulse;

    public float FadeOut(Tick duration)
    {
        // returns 1.0 if the pulse was just triggered, and 0.0 if the pulse was longer ago than the duration
        var since = TimeSinceLastPulse();
        if (since > duration) return 0f;
        return 1f - ((float)since.Frame / (float)duration.Frame);
    }
}