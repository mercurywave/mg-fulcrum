using System.Threading.Tasks;

namespace Fulcrum;

public class IAnimation
{
    bool _complete = false;
    public delegate void AnimationCompleteHandler(IAnimation anim);
    public event AnimationCompleteHandler evAnimationComplete;
    public event AnimationCompleteHandler evAnimationCleanup; // called whether it completed or not
    public virtual void Initialize(AnimationManager manager) { } // keep your state-based setup here, not constructor (enqueue animation will be funny)
    // note: now may not line up with delta - delta is capped per frame
    public virtual void Advance(AnimationManager manager, Tick now, Tick delta) { }
    public virtual void Cleanup(AnimationManager manager) { evAnimationCleanup?.Invoke(this); }

    public bool Complete
    {
        get { return _complete; }
        set
        {
            bool prev = _complete;
            _complete = value;
            if (_complete && !prev)
                evAnimationComplete?.Invoke(this);
        }
    }
    public void Cancel() { _complete = true; } // doesn't trigger completion event, but does trigger cleanup
    public static bool IsActive(IAnimation anim) { return anim != null && !anim.Complete; } // easy way to check for animation running or null for static references

    public async Task<bool> AsyncWait()
    {
        if (Complete) return true;
        var tcs = new TaskCompletionSource<bool>();
        Task t0 = tcs.Task;
        evAnimationCleanup += (anim)
            => tcs.SetResult(true);
        await t0;
        return Complete;
    }
}