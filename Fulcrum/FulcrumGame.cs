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
    internal static SingleThreadSynchronizationContext _sync;
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