
using System;

namespace Fulcrum;

public class ATimer : IAnimation
{
    Tick _remaining;
    public ATimer(int ms) { _remaining = new Tick(ms); }
    public ATimer(float dur) { _remaining = new Tick(dur); }
    public ATimer(Tick tick, AnimationCompleteHandler callback)
    {
        _remaining = tick;
        evAnimationComplete += callback;
    }
    public override void Advance(AnimationManager manager, Tick now, Tick delta)
    {
        _remaining -= delta;
        if (_remaining.LessThanZero()) Complete = true;
    }
    public void Postpone(Tick dur) => _remaining += dur;
}