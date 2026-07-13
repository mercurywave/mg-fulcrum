using Microsoft.Xna.Framework;

namespace Fulcrum;

//implement this instead of Game to get many of features for free
//implement the _versions of functions instead of the base ones for simplicity
public class FulcrumGame : Game
{
	GraphicsDeviceManager graphics;
    public static FulcrumGame Current;
    bool _skipEveryOtherFrame;
    internal static int _Tick = 0;
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
}