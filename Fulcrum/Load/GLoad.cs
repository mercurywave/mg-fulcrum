using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public enum eThrottle { Background, Normal, Prioritized }

public static class GLoad
{
    public static eThrottle Throttling = eThrottle.Background;
    public static string DataDirectory;
    public static string ContentDirectory => GCore.Content.RootDirectory;
    static SortedDictionary<AutoInitialize.eLoadBy, OLoad> _stages = new SortedDictionary<AutoInitialize.eLoadBy, OLoad>() {
            { AutoInitialize.eLoadBy.Game, new OLoad() },
            { AutoInitialize.eLoadBy.Menu, new OLoad() },
            { AutoInitialize.eLoadBy.Launch, new OLoad() }
        };
    static SortedDictionary<string, OLoad> _staticKeys = new SortedDictionary<string, OLoad>();

    public static OLoad Launch => _stages[AutoInitialize.eLoadBy.Launch];
    public static OLoad Menu => _stages[AutoInitialize.eLoadBy.Menu];
    public static OLoad Game => _stages[AutoInitialize.eLoadBy.Game];

    internal static SortedDictionary<string, OLoad> Keyed => _staticKeys; // should probably be carful about access of these things
    static SortedDictionary<AutoInitialize.eLoadBy, OLoad> Staged => _stages;
    public static void Queue(IAsset asset, AutoInitialize attr)
    {
        if (attr.LoadBy == AutoInitialize.eLoadBy.Key)
            Queue(asset, attr.Key, attr.Priority);
        else
            Queue(asset, attr.LoadBy, attr.Priority);
    }
    public static void Queue(IAsset asset, AutoInitialize.eLoadBy step, int priority = 1)
    {
        Staged[step].Queue(asset, priority);
    }
    public static void Queue(IAsset asset, string key, int priority = 1)
    {
        if (!_staticKeys.ContainsKey(key))
            _staticKeys.Add(key, new OLoad());
        _staticKeys[key].Queue(asset, priority);
    }

    public static OLoad GetKeyedLoad(string key) => Keyed[key];
}

public class OLoad
{
    public ContentManager con = new ContentManager(GCore.Content.ServiceProvider, GCore.Content.RootDirectory);

    // syntax for priority queue isn't ideal, but this maintains the order code is written, which a sorted list can't
    OPriorityQueue<int, IAsset> _kickoff = new OPriorityQueue<int, IAsset>();
    List<IAsset> _ownedAssets = new List<IAsset>();
    int _progress = 0;

    public enum eLoaderState { Waiting, Started, Complete, LateLoad }
    eLoaderState _state = eLoaderState.Waiting;
    public bool IsLoaded => (_state >= eLoaderState.Complete);
    public bool WaitingForLoad => (_state == eLoaderState.Waiting);

    public int TotalAssets => _ownedAssets.Count();
    public int AssetsLoaded => _progress;
    public float Progress => TotalAssets == 0 ? 1f : 1f * AssetsLoaded / TotalAssets;

    public OLoad()
    {
    }

    // queue something for load whenever
    public void Queue(IAsset asset, int priority = 1)
    {
        _ownedAssets.Add(asset);
        if (!asset.IsLoaded)
        {
            _kickoff.Enqueue(priority, asset);
            if (IsLoaded)
            {
                _state = eLoaderState.LateLoad;
                GCore.Defer(AsyncLoad());
            }
        }
    }

    // queue up a single asset, and return when it's loaded
    public async Task AsyncLoadAsset(IAsset ass)
    {
        if (ass.IsLoaded) return;
        Queue(ass);
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        ass.DoRunNowOrWhenLoaded(() => tcs.TrySetResult(true));
        await tcs.Task;
    }

    public async Task AsyncLoadAssets(IEnumerable<IAsset> assets)
    {
        List<Task> tasks = new List<Task>();
        foreach (var ass in assets)
            tasks.Add(AsyncLoadAsset(ass));
        await Task.WhenAll(tasks);
    }

    public void Preload()
    {
        GCore.Defer(AsyncLoad(eThrottle.Background));
    }

    Task _loadTask = null;
    public async Task AsyncLoad(eThrottle throttling = eThrottle.Normal)
    {
        _throttling = throttling; // presumably, the latest one is the priorty you want
        if (_loadTask == null)
            _loadTask = _AsyncLoad();
        await _loadTask;
        _loadTask = null;
    }
    eThrottle _throttling;
    async Task _AsyncLoad()
    {
        string firstFileName = _ownedAssets.Count > 0 ? _ownedAssets[0].Path : "";
        using (var logger = GPerf.GetAsyncLogger("Async load (" + _ownedAssets.Count + "): " + firstFileName))
        {
            //need loop here in case things are added while waiting for follow ups
            while (!_kickoff.IsEmpty)
            {
                List<IAsset> followUp = new List<IAsset>();
                //PERF: as I add more "worker" assets I should maybe build a requirement system here
                //		so that things like render tasks can run once the prerequisistes are met
                //		probably using like dependency graph flattening to prioritize
                while (!_kickoff.IsEmpty)
                {
                    if (followUp.Count > 0)
                        await Task.Yield();
                    if (!_kickoff.IsEmpty)
                    {
                        var doKick = _kickoff.DequeueAllLowest();
                        SplitList(doKick, i => i.SafeForParallel, out var canParallel, out var inOrder);
                        if (canParallel.Count > 0)
                        {
                            GPerf.LogAsync(logger, "parallel kickoff (" + canParallel.Count + ")");

                            if (GCore.Debug) // gotta go fast
                                foreach (var i in canParallel)
                                    i.Load(this);
                            else
                            {
                                // you would think it would be faster to parallelize harder, but loading requires the device lock
                                // other attempts seem to have nasty bugs, but at least this prevents frame drops
                                // still not the fastest, but time is probably better spent minimizing the work being done
                                var job = Task.Run(() =>
                                {
                                    foreach (var i in canParallel)
                                        i.Load(this);
                                });
                                while (!job.IsCompleted)
                                    await Task.Delay(5);
                            }

                            GPerf.LogAsync(logger, "parallel done (" + canParallel.Count + ")");
                            foreach (var i in canParallel)
                            {
                                Debug.Assert(i.IsLoaded);
                                i.RunFollowUps();
                                _progress++;
                            }
                            GPerf.LogAsync(logger, "parallel followups");
                            //else followUp.Add(i); // would require special handling to resolve safely
                        }

                        var end = new Tick().OffsetMs(2);
                        foreach (var i in inOrder)
                        {
                            GPerf.LogAsync(logger, "load " + i.ToString());
                            GPerf.BeginBlock("load" + i.ToString());
                            if (i.Load(this) == Fulcrum.eLoadState.Started)
                                followUp.Add(i);
                            else _progress++;
                            GPerf.EndBlock();
                            GPerf.LogAsync(logger, "finish load " + i.ToString());

                            var currThrot = eThrottle.Background;
                            if (_throttling > currThrot) currThrot = _throttling;
                            if (GLoad.Throttling > currThrot) currThrot = GLoad.Throttling;

                            if (currThrot != eThrottle.Prioritized)
                                if (currThrot == eThrottle.Background || Tick.Now() > end) // don't waste async overhead when prioritized, always async when backgrounded
                                {
                                    await AsyncLoadGate(_throttling);
                                    end = new Tick().OffsetMs(2);
                                }
                        }
                    }
                    GPerf.LogAsync(logger, "phase advance");
                }
                GPerf.LogAsync(logger, "end of loop");
                if (followUp.Count > 0)
                {
                    await GCore.GlobalAnimator.AsyncWaitPollCondition(() =>
                    {
                        //we're waiting on someting external like rendering
                        return followUp.All(a => a.IsLoaded);
                    });
                    _progress += followUp.Count;
                    GPerf.LogAsync(logger, "follow ups checked");
                }
            }
            GPerf.LogAsync(logger, "all done");
        }
        if (GCore.CanUseDebug)
            HotLoader.Register(this);
        _state = eLoaderState.Complete;
    }

    #region queue
    public static IAnimation _animQueueManager = null;
    static OPriorityQueue<eThrottle, TaskCompletionSource<bool>> _loadQueue = new OPriorityQueue<eThrottle, TaskCompletionSource<bool>>();
    internal static async Task AsyncLoadGate(eThrottle throttle)
    {
        var tcs = new TaskCompletionSource<bool>();
        Task t0 = tcs.Task;
        _loadQueue.Enqueue(throttle, tcs);

        if (!IAnimation.IsActive(_animQueueManager))
            _animQueueManager = GCore.GlobalAnimator.ReplaceAnimation(_animQueueManager, doPrioritze());

        await t0;
    }
    static ALoop doPrioritze() => new ALoop(i =>
    {
        if (_loadQueue.IsEmpty) return false;
        var rel = _loadQueue.DequeueLowest();
        rel.TrySetResult(true);
        return !_loadQueue.IsEmpty;
    });
    #endregion

    static void SplitList<T>(IEnumerable<T> iter, Func<T, bool> cond, out List<T> ifTrue, out List<T> ifFalse)
    {
        // there are maybe cleverer ways to do this, but not simpler ones
        ifTrue = new List<T>();
        ifFalse = new List<T>();
        foreach (var i in iter)
            if (cond(i))
                ifTrue.Add(i);
            else ifFalse.Add(i);
    }

    public void Unload(IAsset asset)
    {
        if (!asset.IsLoaded) return; // you probably shouldn't unload an unloaded asset
        asset.Unload(this);
    }

    public void Unload()
    {
        HotLoader.Unregister(this);
        foreach (var ass in _ownedAssets)
            if (ass.IsLoaded)
                ass.Unload(this);
        con.Unload();
        _state = eLoaderState.Waiting; // you probably don't want to unload and then load again
    }
}