using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Fulcrum;

public class AnimationManager
{
    internal List<IAnimation> _animations = new List<IAnimation>();
    internal List<IAnimation> _toInitialize = new List<IAnimation>();
    public static Tick DEFAULT_CAP = new Tick(16);
    public Tick Cap = DEFAULT_CAP;
    public static float GlobalSlowDown = 1f;
    public float SlowDown = 1f;


    public IAnimation AddAnimation(IAnimation anim)
    {
        Debug.Assert(anim != null);
        _toInitialize.Add(anim);
        return anim;
    }
    // add animation after another, or run immediately if null/complete (handy for keeping a single reference for something like slide in/out)
    public void QueueAnimation(IAnimation oldAnim, IAnimation newAnim)
    {
        if (oldAnim == null || oldAnim.Complete)
            AddAnimation(newAnim);
        else
            oldAnim.evAnimationComplete += (anim) =>
            {
                AddAnimation(newAnim);
            };
    }
    // cancel a running animation, if it's running, and start a new one. Returns the new anim. Designed for common action-reverse animations
    // _anim = manager.ReplaceAnimation(_anim,new AAnimation())
    public IAnimation ReplaceAnimation(IAnimation oldAnim, IAnimation newAnim)
    {
        if (oldAnim != null)
            Postpone(() => oldAnim.Cancel()); // we don't want to have a frame delay if you transition, so cleanup after last cycle
        if (newAnim != null)
            AddAnimation(newAnim);
        return newAnim;
    }
    public bool IsAnimating { get { return _animations.Count > 0; } }

    public void Advance(Tick delta)
    {
        GPerf.BeginBlock("Animation Update");
        if (delta > Cap) delta = Cap;
        delta *= SlowDown * GlobalSlowDown;

        InitQueued(false);
        foreach (var anim in _animations.ToArray())
            if (!anim.Complete)
                anim.Advance(this, delta);

        InitQueued(true);
        CleanupQueued();

        GPerf.EndBlock();
    }

    void InitQueued(bool catchup)
    {
        while (_toInitialize.Count > 0)
        {
            var toInit = _toInitialize.ToArray();
            _toInitialize.Clear();
            _animations.AddRange(toInit);
            foreach (var anim in toInit)
                anim.Initialize(this);
            if (catchup)
                foreach (var anim in toInit)
                    if (!anim.Complete)
                        anim.Advance(this, Tick.Zero);
        }
    }

    void CleanupQueued()
    {
        while (_animations.Any(a => a.Complete))
        {
            var toClean = _animations.Where(a => a.Complete).ToHashSet();
            foreach (var anim in toClean)
                anim.Cleanup(this);
            _animations.RemoveAll(a => toClean.Contains(a));
        }
    }

    public void Postpone(Action callback) 
        => Postpone(0, callback);
    public void Postpone(float seconds, Action callback)
    {
        var timer = new ATimer(new Tick(seconds), _ => callback());
        AddAnimation(timer);
    }

    public async Task AsyncDelay(float seconds)
    {
        var tcs = new TaskCompletionSource<bool>();
        Task t0 = tcs.Task;
        ATimer timer = new ATimer(seconds);
        timer.evAnimationComplete += (anim)
            => tcs.SetResult(true);
        AddAnimation(timer);
        await t0;
    }
}