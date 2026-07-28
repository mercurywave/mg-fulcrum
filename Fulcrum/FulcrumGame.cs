using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Fulcrum;

//implement this instead of Game to get many of features for free
//implement the _versions of functions instead of the base ones for simplicity
public class FulcrumGame : Game
{
    public GraphicsDeviceManager graphics;
    public static FulcrumGame Current;
    bool _skipEveryOtherFrame;
    internal static int _Frame = 0;
    internal static Tick FrameStart = new Tick();
    internal static SingleThreadSynchronizationContext _sync;
    public FpsCounter UpdateCounter = new FpsCounter();
    public FpsCounter DrawCounter = new FpsCounter();
    public FulcrumGame() : base()
    {
#if DEBUG
        GCore.CanUseDebug = true;
#endif
        GCore.Debug = GCore.CanUseDebug && System.Diagnostics.Debugger.IsAttached;

        Content.RootDirectory = "Content";
        Current = this;
        HalfDrawRate = GCore.Debug;
        GPerf.Enabled = GCore.Debug;

        _sync = new SingleThreadSynchronizationContext();
        graphics = new GraphicsDeviceManager(this) { GraphicsProfile = Microsoft.Xna.Framework.Graphics.GraphicsProfile.HiDef };
    }
    public bool HalfDrawRate
    {
        get { return _skipEveryOtherFrame; }
        set { _skipEveryOtherFrame = value; }
    }
    public bool ThrottleFPS
    {
        get { return IsFixedTimeStep; }
        set { IsFixedTimeStep = value; }
    }

    override protected void Initialize()
    {
        GReflection.Scan();
        GReflection.Scan(this.GetType().GetTypeInfo().Assembly); //getting type here pulls the implmenting assembly
        GCore.SceneManager = new OSceneManager();
        GCore.ComponentTree = new ComponentTree(GCore.SceneManager);
        _InitializeScreen(graphics);
        _Initialize();
        _sync.Run(() => RunAsyncMain());
        base.Initialize();
    }

    //implement to change standard config and do any other config, if neccessary
    protected virtual void _InitializeScreen(GraphicsDeviceManager graphics)
    {
        GScreen.Setup(graphics, Window);
        int width = _InitialWidth();
        int height = _InitialHeight();
        bool fullscreen = _InitFullScreen();
        if (GScreen.ApplicationSetsScale)
            GScreen.SetScale(GetScaleForWindow());
        Window.ClientSizeChanged += Window_ClientSizeChanged;
        GScreen.Initialize(fullscreen, width, height);
    }
    protected virtual int _InitialWidth() { return -1; }
    protected virtual bool _InitFullScreen() { return false; }
    protected virtual int _InitialHeight() { return -1; }
    protected virtual void _Initialize() { } // generic initialize



    async Task RunAsyncMain()
    {
        await RunLoadAsync();
        try
        {
            await _AsyncMain();
        }
        catch (Exception e) { GError.RaiseError(e); }
        Debug.WriteLine("AsyncMain has completed");
    }

    async Task RunLoadAsync()
    {
        try
        {
            await _LoadAsync();
        }
        catch (Exception e) { GError.RaiseError(e); }
    }
    protected virtual async Task _LoadAsync() { await Task.Yield(); }
    // implement for async after the menu content is loaded
    public virtual async Task _AsyncMain()
    {
    }



    protected override void Update(GameTime gameTime)
    {
        _Frame++;
        FrameStart = new Tick();
        GPerf._BeginFrame(_Frame);
        GPerf.BeginBlock(GPerf.eMajorTraceType.Update);
        UpdateCounter.BeginFrame(FrameStart);

        _sync.Run(() =>
        {
            if (GCore.IsLoaded)
            {
                _Update();
                GCore.SceneManager.OnLayout();
                GCore.ComponentTree.WalkTree<IUpdate>((c) => c.OnUpdate(FrameStart));
            }
        });


        if (!GCore.IsLoaded || (_skipEveryOtherFrame && _Frame % 2 == 0))
            SuppressDraw();
        base.Update(gameTime);
        GPerf.EndBlock();
    }

    //implement if you want additional logic to run at every game frame
    //call base._Update if you want to support the debug shortcut key
    protected virtual void _Update()
    {
        // if (GCore.CanUseDebug && GCore.Press(Keys.OemTilde))
        //     GCore.Debug = !GCore.Debug;
        //why don't alt keys work?
        //if ((GKeyboard.held(Keys.LeftControl) || GKeyboard.held(Keys.RightControl)) && GKeyboard.press(Keys.Enter))
        //	GScreen.ToggleFullScreen();
    }



    protected override void Draw(GameTime gameTime)
    {
        var start = new Tick();
        GPerf.BeginBlock(GPerf.eMajorTraceType.Draw);
        DrawCounter.BeginFrame(start);

        if (GCore.IsLoaded)
        {
            _sync.Run(() =>
            {
                GCore.SceneManager.OnLayout();

                GPerf.BeginBlock(GPerf.eMajorTraceType.Render);
                GCore.ComponentTree.WalkTree<IDraw>((c) => c.OnRender(FrameStart));
                GPerf.EndBlock();

                GPerf.BeginBlock(GPerf.eMajorTraceType.Draw);
                GDraw.Clear(GDraw.DefaultColor);

                using (GDraw.BatchBlock())
                {
                    GCore.ComponentTree.WalkTree<IDraw>((c) => c.OnPreDraw(FrameStart));
                    GCore.ComponentTree.WalkTree<IDraw>((c) => c.OnDraw(FrameStart));
                    GCore.ComponentTree.WalkTree<IDraw>((c) => c.OnPostDraw(FrameStart));
                }
                GPerf.EndBlock();
            });
        }
        else GDraw.Clear(GDraw.DefaultColor);

        base.Draw(gameTime);
        GPerf.EndBlock();
    }


    #region DPI Scaling
    //update the inner resolution and scaling
    public void FlagWindowAsDirty()
    {
        GCore.GlobalAnimator.Postpone(.25f, ApplyWindowChanges);
    }
    public void ApplyWindowChanges()
    {
        //must be minimized or something? this is unreasonable
        if (Window.ClientBounds.Width < 320 || Window.ClientBounds.Height < 240) return;
        GScreen.WindowChanged(Window.ClientBounds.Width, Window.ClientBounds.Height);
        if (GScreen.ApplicationSetsScale)
        {
            float newScale = GetScaleForWindow();
            GScreen.SetScale(newScale);
        }
    }
    private void Window_ClientSizeChanged(object sender, System.EventArgs e)
    {
        FlagWindowAsDirty();
    }

    //must set Gscreen.ApplicationSetsScale true to enable DPI scaling
    protected float MonitorDPI = 60; // PLATFORM: Game is reponsible for looking up
    public virtual float GetScaleForWindow()
    {
        var dpi = MonitorDPI;
        if (dpi < 60) dpi = 60;
        float auto = (float)(dpi / 96);
        float scale = auto;
        if (Window.ClientBounds.Width < 600 * scale || Window.ClientBounds.Height < 480 * scale)
            scale = .75f;
        else if (Window.ClientBounds.Width < 800 * scale || Window.ClientBounds.Height < 600 * scale)
            scale = 1;
        return scale;
    }
    #endregion
}