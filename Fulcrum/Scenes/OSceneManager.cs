using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fulcrum;


public class OSceneManager : ILayout, IComponentContainer
{
    public ComponentTree Tree { get; set; }
    public OLayout Layout { get; set; }

    AnimationManager _animManager = new AnimationManager();
    AsyncLock _transitionLock = new AsyncLock();

    public void OnLayout()
    {
        Layout.Left = 0;
        Layout.Top = 0;
        Layout.Width = GScreen.Width;
        Layout.Height = GScreen.Height;
    }

    public async Task<eSceneRunResult> Launch(IScene scene, ITransition there = null) =>
        await BeginRunScene([scene], there);

    internal async Task<eSceneRunResult> BeginRunScene(List<IScene> states, ITransition trans = null)
    {
        using (await CriticalSection())
        {
            var original = Tree.GetChildren(this).FilterCast<IComponent, IScene>().ToList();
            var toClose = original.Except(states).ToList();
            var baseLine = original.Except(toClose).ToList();
            states.ForEach(s => prepFreshScene(s));
            await WaitForScenesToLoad(states);

            Tree.AddChildren(this, states);

            if (trans != null)
            {
                trans.SetScenes(toClose, states, baseLine);
                await _animManager.AsyncRunWaitForAnimations(trans);
            }

            foreach (var s in toClose)
                Tree.Remove(s);
            return eSceneRunResult.Completed;
        }
    }

    async Task<IDisposable> CriticalSection() =>
        await _transitionLock.LockAsync();

    internal async Task WaitForScenesToLoad(List<IScene> states)
    {
        if (states.All(s => s.SceneData.IsLoaded)) return;
        await _animManager.AsyncWaitPollCondition(() => states.All(s => s.SceneData.IsLoaded));
    }

    void prepFreshScene(IScene state)
    {
        state.SceneData = new OScene(state);
        state.SceneData.BeginLoad();
    }
}