using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public static class GRenderQueue
{
    static OPriorityQueue<int, Action> _toRender = new OPriorityQueue<int, Action>();

    //only add once source assets are loaded (that's not always obvious - use imageasset evafterload)
    public static void Add(Action render, int priority = 1)
    {
        _toRender.Enqueue(priority, render);
    }

    public static bool IsEmpty { get { return _toRender.IsEmpty; } }

    //just call every render cycle for simplicity
    //throttle is number of frames to skip
    public static void RunRenders()
    {
        GPerf.BeginBlock("GRenderQueue Render");
        if (_tcs != null || (GCore.Debug && GLoad.Throttling == eThrottle.Prioritized)) RenderAllNow();
        else if (!IsEmpty)
        {
            var end = Tick.Now() + Tick.Ms(4);
            while (!IsEmpty && Tick.Now() < end)
                RenderOne();
        }
        GPerf.EndBlock();
    }

    static void RenderOne()
    {
        GPerf.BeginBlock("GRenderQueue RenderOne");
        Action render = _toRender.DequeueLowest();
        render();
        GPerf.EndBlock();
    }

    static void RenderAllNow()
    {
        while (!IsEmpty)
            RenderOne();
        if (_tcs != null)
        {
            _tcs.SetResult(true);
            _tcs = null;
        }
    }

    static TaskCompletionSource<bool> _tcs;
    public static async Task RenderAllAsync()
    {
        if (_tcs == null)
            _tcs = new TaskCompletionSource<bool>();
        Task t0 = _tcs.Task;
        await t0;
    }
    public static int Depth()
    {
        return _toRender.CountNodes();
    }

    // will complete whenever (assumes your handler is flagging completion another way)
    public static void RunRenderJob(Action handler) => Add(handler);
}
// perform some work while screen is ready for drawing
public class RenderableAsset : IAsset
{
    Action _act;
    public RenderableAsset(Action act) { _act = act; }
    public override eLoadState _Load(OLoad load)
    {
        GRenderQueue.RunRenderJob(() =>
        {
            _act();
            CompleteLoad();
        });
        return eLoadState.Waiting;
    }
}

// manages render target life cycle like any other asset
// you're still responsible for setting the render target
public class RenderTargetAsset : IAsset
{
    Action _act;
    RenderTarget2D _render;
    public RenderTarget2D Render => _render;
    public RenderTargetAsset(Action<RenderTarget2D> draw, int W, int H)
    {
        _act = () =>
        {
            _render = new RenderTarget2D(GScreen.Device, W, H);
            draw(Render);
            CompleteLoad();
        };
    }
    public RenderTargetAsset(Action<RenderTarget2D> draw, Func<int> getWidth, Func<int> getHeight)
    {
        _act = () =>
        {
            _render = new RenderTarget2D(GScreen.Device, getWidth(), getHeight());
            draw(Render);
            CompleteLoad();
        };
    }
    public override eLoadState _Load(OLoad load)
    {
        GRenderQueue.RunRenderJob(_act);
        return eLoadState.Waiting;
    }
    public override void _Unload(OLoad load)
    {
        if (Render != null && !Render.IsDisposed)
            Render.Dispose();
    }
    public static implicit operator Texture2D(RenderTargetAsset asset) { return asset.Render; }
}

// use with using block
public class ScreenBuffer : IDisposable
{
    public RenderTarget2D Output;
    public ScreenBuffer(RenderTarget2D target)
    {
        Output = target;
        BeginRender(Matrix.Identity, BlendState.AlphaBlend, null, SpriteSortMode.Immediate, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, Point offset) // offset rendering camera to point
    {
        Output = target;
        BeginRender(Matrix.CreateTranslation(-offset.X, -offset.Y, 1), BlendState.AlphaBlend, null, SpriteSortMode.Immediate, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, BlendState blend) // sprite sort is probably immediate for my use cases
    {
        Output = target;
        BeginRender(Matrix.Identity, blend, null, SpriteSortMode.Immediate, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, BlendState blend, Effect effect) // sprite sort is probably immediate for my use cases
    {
        Output = target;
        BeginRender(Matrix.Identity, blend, effect, SpriteSortMode.Immediate, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, SpriteSortMode sortMode) // sprite sort is probably immediate for my use cases
    {
        Output = target;
        BeginRender(Matrix.Identity, BlendState.AlphaBlend, null, sortMode, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, Matrix transform)
    {
        Output = target;
        BeginRender(transform, BlendState.AlphaBlend, null, SpriteSortMode.Immediate, GDraw.DefaultSamplerState);
    }
    public ScreenBuffer(RenderTarget2D target, Matrix transform, BlendState blend, Effect effect, SpriteSortMode sortMode)
    {
        Output = target;
        BeginRender(transform, blend, effect, sortMode, GDraw.DefaultSamplerState);
    }

    public ScreenBuffer(RenderTarget2D target, Matrix transform, BlendState blend, Effect effect, SpriteSortMode sortMode, SamplerState sample)
    {
        Output = target;
        BeginRender(transform, blend, effect, sortMode, sample);
    }
    void BeginRender(Matrix transform, BlendState blend, Effect effect, SpriteSortMode sortMode, SamplerState sample)
    {
        GPerf.BeginBlock("ScreenBuffer Render");
        GScreen.Device.SetRenderTarget(Output);
        GDraw.Begin(transform, sortMode, blend, effect, sample);
        GDraw.Clear(Color.Transparent);
    }
    public void Dispose()
    {
        GDraw.End();
        GScreen.Device.SetRenderTarget(null);
        GPerf.EndBlock();
    }

    //returns true if we replaced / created render
    public static bool PrepRenderTarget(ref RenderTarget2D render, int w, int h)
    {
        var created = (render == null || render.Width != w || render.Height != h);
        if (created)
        {
            if (render != null && !render.IsDisposed) render.Dispose();
            render = new RenderTarget2D(GScreen.Device, w, h);
        }
        return created;
    }
}