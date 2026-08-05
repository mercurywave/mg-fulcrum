using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fulcrum;

public enum eSceneRunResult { Completed, Cancelled }

public class IScene : ILayout
{
    public OLayout Layout { get; set; }
    public ComponentTree Tree { get; set; }
    public OScene SceneData { get; set; }
    public virtual OLoad _DynamicContent => null; // specify a content manager that is loaded and unloaded like magic
    public virtual OLoad _StaticContent => null; // loaded once when needed


    public virtual async Task _DoLoadAsync() { } // called every time 
    public void RequestClose() => SceneData.CloseRequested = true;
    public virtual void _OnClose() { }
    public virtual void _DoUnload() { }

    public virtual void OnLayout() { }

    // This isn't a spoke because the scene manager sets it up before it's added to the tree
}

internal enum eInitState { New, Initialized, Running }
public class OScene
{
    public IScene Scene;
    public OScene(IScene scene) { Scene = scene; }

    internal bool _initialized => _initState != eInitState.New;
    internal eInitState _initState = eInitState.New;

    enum eLoadStage { Waiting, Started, Done }
    eLoadStage _loadStage = eLoadStage.Waiting;
    public bool IsLoaded { get { return _loadStage == eLoadStage.Done; } }


    public bool CloseRequested
    {
        get => _closeRequest != eCloseState.Active;
        set
        {
            if (value) _closeRequest = eCloseState.RequestClose;
            else _closeRequest = eCloseState.Active;
        }
    }
    internal enum eCloseState { Active, RequestClose, Closed }
    internal eCloseState _closeRequest = eCloseState.Active;
    public bool IsClosing => _closeRequest != eCloseState.Active;

    internal void BeginLoad()
    {
        if (_loadStage != eLoadStage.Waiting) return;
        GCore.Defer(LoadAsync());
    }
    async Task LoadAsync()
    {
        _loadStage = eLoadStage.Started;

        Func<OLoad, Task> helper = async (l) => { if (l != null) await l.AsyncLoad(); };

        await helper(Scene._StaticContent);

        await helper(Scene._DynamicContent);

        await Scene._DoLoadAsync();

        _loadStage = eLoadStage.Done;
    }
    internal void Unload()
    {
        _loadStage = eLoadStage.Waiting;
        Scene._DoUnload();
        Scene._DynamicContent?.Unload();
    }
}

public class ISceneResult<T> : IScene
{
    public T DefaultReturn { get; }
    public async Task<(eSceneRunResult Result, T Output)> _AsyncRunGetResult() { throw new NotImplementedException(); }
}

public class ITransition : IAnimation
{
    protected List<IScene> _fromState, _toState;
    protected List<IScene> _alwaysBehind; //these are present before and after

    internal void SetScenes(List<IScene> from, List<IScene> to, List<IScene> alwaysBehind)
    {
        _fromState = from;
        _toState = to;
        _alwaysBehind = alwaysBehind;
    }
}
