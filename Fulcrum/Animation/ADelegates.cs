using System;

namespace Fulcrum;


//called until you return false
public class ALoop : IAnimation
{
    public delegate bool LoopHandler(Tick runDuration);
    LoopHandler _handle;
    Tick _start;
    public ALoop(LoopHandler handle)
    {
        _handle = handle;
    }
    public override void Initialize(AnimationManager manager)
    {
        _start = GCore.FrameStart;
    }
    public override void Advance(AnimationManager manager, Tick now, Tick delta)
    {
        if (!_handle(now - _start))
            Complete = true;
    }
}