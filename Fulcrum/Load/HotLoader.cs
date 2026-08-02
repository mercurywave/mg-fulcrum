using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Fulcrum;


record FileTracker
{
    public IAsset Asset;
    public DateTime Modified;
}
class HotLoaderTracker
{
    public OLoad Loader;
    public HotLoaderTracker(OLoad loader)
    {
        Loader = loader;
    }
    List<FileTracker> _hotLoaders = new List<FileTracker>();
    public void Register(IAsset hot)
    {
        var path = hot.Path;
        if (!_hotLoaders.Any(ft => ft.Asset.Path == path))
            _hotLoaders.Add(new FileTracker { Asset = hot, Modified = DateTime.Now });
    }
    public void HotLoad(string path)
    {
        var tracker = _hotLoaders.FirstOrDefault(ft => ft.Asset.Path == path);
        if (tracker != null)
            tracker.Asset.HotLoad();
    }

    public async Task ScanForChanges()
    {
        int counter = 0;
        foreach (var tracker in _hotLoaders.ToList())
        {
            var path = tracker.Asset.AbsolutePath;
            var modified = new FileInfo(path).LastWriteTime;
            if (modified > tracker.Modified)
            {
                HotLoad(path);
                tracker.Modified = modified;
            }
            if ((counter++) % 10 == 0)
                await GCore.HoldFrame();
        }
    }
}

internal static class HotLoader
{
    public static bool Enabled = false;
    static Dictionary<OLoad, HotLoaderTracker> _trackers = new Dictionary<OLoad, HotLoaderTracker>();
    public static void Register(OLoad loader)
    {
        if (!GCore.CanUseDebug) return;
        if (!_trackers.ContainsKey(loader))
            _trackers.Add(loader, new HotLoaderTracker(loader));
    }
    public static void Register(OLoad loader, IAsset hot)
    {
        _trackers[loader].Register(hot);
    }
    public static void Unregister(OLoad loader)
    {
        if (_trackers.ContainsKey(loader))
            _trackers.Remove(loader);
    }

    public static async Task ScanForChanges()
    {
        try
        {
            while (true)
            {
                if (!Enabled)
                {
                    await GCore.GlobalAnimator.AsyncDelay(1);
                    continue;
                }
                foreach (var tracker in _trackers.Values.ToList())
                    await tracker.ScanForChanges();
                await GCore.HoldFrame();
            }
        }
        catch (Exception e)
        {
            GError.RaiseError(e);
        }
    }

}