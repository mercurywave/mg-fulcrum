using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fulcrum;

public static class GCore
{
    public static bool Debug = false;
    internal static bool CanUseDebug = false;
    public static AnimationManager GlobalAnimator = new AnimationManager();
    internal static ComponentTree ComponentTree = null;
    public static Tick FrameStart => FulcrumGame.FrameStart;


    internal static SingleThreadSynchronizationContext _sync;
    public static void Defer(Task t)
    {
        System.Diagnostics.Debug.Assert(SynchronizationContext.Current == _sync);
    }
    public static void Defer(Func<Task> t) { Defer(t()); }


    public static List<Action> _nextFrame = new List<Action>();
    public static async Task HoldFrame()
    {
        var tcs = new TaskCompletionSource<bool>();
        Postpone(() => tcs.SetResult(true));
        await tcs.Task;
    }
    public static void Postpone(Action action)
    {
        _nextFrame.Add(action);
    }
    public static void RunPostponed()
    {
        while (_nextFrame.Count > 0)
        {
            using (GPerf.UseBlock("Postponed"))
            {
                var actions = _nextFrame.ToArray();
                _nextFrame.Clear();
                foreach (var action in actions)
                    action();
            }
        }
    }
}