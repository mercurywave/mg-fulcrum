using System;
using System.Threading.Tasks;

namespace Fulcrum;

public enum eLoadState { Waiting, Started, Complete, Failed, Unloaded }

// unspecified is really only meant for attribute use (can't use nullable bool in an attribute)
public enum eAssetLocation { Unspecified, Content, Data }

public class IAsset
{
    public IAsset() { }
    public IAsset(string path)
    {
        _path = path;
    }
    public delegate void AssetAfterLoadHandler(IAsset asset);
    public event AssetAfterLoadHandler evAssetLoaded;
    public eLoadState Load(OLoad load)
    {
        try
        {
            var result = _Load(load);
            if (result == eLoadState.Complete) CompleteLoad();
            return result;
        }
        catch (Exception e) { GError.RaiseError(new Exception("File loading error: " + ToString(), e)); }
        return eLoadState.Failed;
    }
    public virtual eLoadState _Load(OLoad load) { return eLoadState.Complete; } //override this to load the asset
    internal eLoadState BeginLoad(OLoad load)
    {
        LoadState = eLoadState.Started;
        var result = _Load(load);
        if (result == eLoadState.Complete) CompleteLoad();
        return result;
    }
    public virtual bool _Unload(OLoad load) { return true; }
    internal void Unload(OLoad load)
    {
        // most likely, if this isn't complete, it was cleared because a parent unloaded it
        if (LoadState != eLoadState.Complete) return;
        LoadState = eLoadState.Unloaded;
        // NOTE: there are a bunch of assets that don't try to really unload
        // Some assets just don't make a lot of sense
        _Unload(load);
    }
    //called when an asset is fully loaded
    protected void CompleteLoad()
    {
        if (LoadState == eLoadState.Complete) return;
        LoadState = eLoadState.Complete;
        if (!SafeForParallel) RunFollowUps();
    }
    public eLoadState LoadState = eLoadState.Waiting;
    public bool IsLoaded => LoadState == eLoadState.Complete;
    public bool WaitingForLoad => LoadState == eLoadState.Waiting;
    public eAssetLocation Location = eAssetLocation.Data;
    // relative to the location root
    string _path;
    public override string ToString()
    {
        return GetType().Name + " " + _path;
    }
    public string Path { get { return _path; } }

    public string AbsolutePath
    {
        get
        {
            if (Location == eAssetLocation.Content)
                return GLoad.ContentDirectory + "\\" + _path;
            else
                return GLoad.DataDirectory + "\\" + _path;
        }
    }

    public string Extension
    {
        get
        {
            var ext = System.IO.Path.GetExtension(_path);
            if (ext.Length > 0) return ext.Substring(1);
            return "";
        }
    }
    public string PathWithoutExtension
    {
        get
        {
            var ext = System.IO.Path.GetExtension(_path);
            if (ext.Length > 0) return _path.Substring(0, _path.Length - ext.Length);
            return _path;
        }
    }

    public void DoRunNowOrWhenLoaded(Action hook)
    {
        if (LoadState == eLoadState.Complete) hook();
        else evAssetLoaded += a => hook();
    }
    public virtual bool SafeForParallel => false; // opting in can't support deferred loading
    internal void RunFollowUps()
    {
        evAssetLoaded?.Invoke(this);
        evAssetLoaded = null;
    }
    public virtual bool CanHotLoad => false;
    public virtual void HotLoad(OLoad load) { }
}


//one time action asset, primarily to schedule asset loading
public class ActionAsset : IAsset
{
    Action _act;
    Action _cleanup;
    public ActionAsset(Action act) { _act = act; }
    public ActionAsset(Action act, Action cleanup) : this(act) { _cleanup = cleanup; }
    public override eLoadState _Load(OLoad load)
    {
        _act.Invoke();
        return base._Load(load);
    }
    public override bool _Unload(OLoad load)
    {
        _cleanup?.Invoke();
        return base._Unload(load);
    }
}

//one time action asset, primarily to schedule asset loading
public class AsyncAsset : IAsset
{
    Func<Task> _act;
    public AsyncAsset(Func<Task> act) { _act = act; }
    public override eLoadState _Load(OLoad load)
    {
        Func<Task> func = async () =>
        {
            await _act();
            CompleteLoad();
        };
        GCore.Defer(func());
        return eLoadState.Started;
    }
}