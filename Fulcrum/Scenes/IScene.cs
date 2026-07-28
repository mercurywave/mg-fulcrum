using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fulcrum;

public enum eSceneRunResult { Completed, Cancelled }

[Spoke]
public interface IScene : ILayout
{
    public OScene SceneData { get; set; }
    public async Task DoLoadAsync() { } // called every time 
    public void DoUnload() { } // called every time 
    public void OnClose();
    public void RequestClose() => SceneData.CloseRequested = true;
    public static void SpokeOnAdd(IScene scene) { scene.SceneData = new OScene(scene); }
    public static void SpokeOnRemove(IScene scene) { }

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

        // Func<OLoad, Task> helper = async (l) => { if (l != null) await l.AsyncLoad(); };
        // Func<IAssetBundle, Task> helper2 = async (l) => { if (l != null) await l.AsyncLoad(); };

        // await helper2(_OnLoadStaticData());
        // await helper(_StaticContent);

        // await helper2(_OnLoadDynamicData());
        // await helper(_DynamicContent);

        await Scene.DoLoadAsync();

        _loadStage = eLoadStage.Done;
    }
    internal void Unload()
    {
        _loadStage = eLoadStage.Waiting;
        Scene.DoUnload();
        //_DynamicContent?.Unload();
    }
}

public interface ISceneResult<T> : IScene
{
    public T DefaultReturn { get; }
    public Task<(eSceneRunResult Result, T Output)> _AsyncRunGetResult();
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
