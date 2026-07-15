
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public static class GScreen
{

    static bool _initizlized = false;
    public static int Width, Height;
    public static int HalfWidth => Width / 2;
    public static int HalfHeight => Height / 2;
    public static float AspectRatio;

    public static int SafeLeft, SafeRight, SafeTop, SafeBottom; // tilesafe area
    public static int SafeWidth, SafeHeight;
    public static bool IsWide;
    public static bool IsPortrait;
    internal static SpriteBatch sb;
    static int screenW, screenH;
    public static GraphicsDevice device;
    public static GraphicsDeviceManager _graphics;
    public static GameWindow _window;


    public static float MinScale = 1f;
    public static float Scale { get { return _scale; } }
    public static int iScaleCieling => (int)Math.Ceiling(_scale);
    public static int GetScaledPixelDim(int dim) => (int)(dim * Scale); // simple helper to get a distance in real pixels adjusted for scale
    static float _scale = 1;
    public static Matrix ScaleTransform;

    //true - either use GameEngines DPI implementation with DPI events and DPI aware app
    //false - scale based on resolution relative to 720p is 1x
    public static bool ApplicationSetsScale = true;
    public enum eAutoDisplaySizeMode { None, Small, Large, FullScreen, WindowSize } // PLATFORM: UAP should probably always use WindowSize
    static eAutoDisplaySizeMode _autoDisplayMode = eAutoDisplaySizeMode.None; // you probably want to set this 
    internal static void Setup(GraphicsDeviceManager graphics, GameWindow window)
    {
        device = graphics.GraphicsDevice;
        _graphics = graphics;
        _window = window;
    }

    public static void Initialize(bool fullscreen = true, int w = -1, int h = -1)
    {
        _graphics.HardwareModeSwitch = false; // use borderless window full screen (99% case for me)
        IsWide = GraphicsAdapter.DefaultAdapter.IsWideScreen;
        //screenH = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        //screenW = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        //screenH = device.PresentationParameters.BackBufferHeight; // yeah, this isn't actually the screen's size...
        //screenW = device.PresentationParameters.BackBufferWidth;
        screenH = device.Adapter.CurrentDisplayMode.Height;
        screenW = device.Adapter.CurrentDisplayMode.Width;

        if (AutoDisplayMode != eAutoDisplaySizeMode.None)
        {
            var size = GetAutoSize(AutoDisplayMode);
            w = size.X;
            h = size.Y;
        }

        Resize(fullscreen, w, h);
        //ScaleTransform = Matrix.CreateScale(Scale, Scale, 1);
        sb = new SpriteBatch(device);
        _initizlized = true;
    }

    public static eAutoDisplaySizeMode AutoDisplayMode
    {
        get { return _autoDisplayMode; }
        set { UpdateAutoDisplay(value); }
    }

    public static void UpdateAutoDisplay(eAutoDisplaySizeMode mode)
    {
        if (AutoDisplayMode == mode) return;
        _autoDisplayMode = mode;
        if (!_initizlized) return;
        var size = GetAutoSize(mode);
        Resize(mode == eAutoDisplaySizeMode.FullScreen, size.X, size.Y);
    }

    public static Point GetAutoSize(eAutoDisplaySizeMode mode)
    {
        int h = screenH;
        int w = screenW;
        if (mode == eAutoDisplaySizeMode.WindowSize)
        {
            h = device.PresentationParameters.BackBufferHeight;
            w = device.PresentationParameters.BackBufferWidth;
        }
        else if (mode != eAutoDisplaySizeMode.None)
        {
            //var monitor = FindMaxAdapterRes();
            //int maxW = monitor.Width;
            //int maxH = monitor.Height;
            int maxW = device.Adapter.CurrentDisplayMode.Width;
            int maxH = device.Adapter.CurrentDisplayMode.Height;
            float proportion = .5f;
            if (maxW < 1024 || maxH < 900) proportion = 1;
            else if (mode == eAutoDisplaySizeMode.Large) proportion = .8f;
            else if (mode == eAutoDisplaySizeMode.Small) proportion = .6f;
            else if (mode == eAutoDisplaySizeMode.FullScreen) proportion = 1;
            float px = 1f * w / maxW;
            float py = 1f * h / maxH;
            float modify = 1f;
            if (px > py)
                modify = proportion / px;
            else
                modify = proportion / py;
            if (modify != 1f)
            {
                w = (int)(w * modify);
                h = (int)(h * modify);
            }
            //this API isn't available in UAP :(
            //_window.Position = new Point(maxW / 2 - w / 2, maxH / 2 - h / 2); // vaguely center - funny on multiple monitors
        }
        return new Point(w, h);
    }

    static DisplayMode FindMaxAdapterRes()
    {
        DisplayMode big = null;
        foreach (var disp in device.Adapter.SupportedDisplayModes)
            if (big == null || disp.Width * disp.Height > big.Width * big.Height)
                big = disp;
        return big;
    }

    public static void Resize()
    {
        int h = device.PresentationParameters.BackBufferHeight;
        int w = device.PresentationParameters.BackBufferWidth;
        Resize(true, w, h);
    }

    public static void Resize(bool fullscreen)
    {
        fullscreen = true;
        if (fullscreen)
            Resize(fullscreen, screenW, screenH); // use monitor res
        else
            Resize(fullscreen, 800, 600);
    }

    static bool _resizing = false;
    public static void Resize(bool fullscreen, int w, int h)
    {
        if (_resizing) return;
        _resizing = true;
        int oldW = screenW;
        int oldH = screenH;
        _graphics.IsFullScreen = fullscreen;
        _graphics.PreferredBackBufferWidth = w;// (int)(Scale * w);
        _graphics.PreferredBackBufferHeight = h;// (int)(Scale * h);

        _graphics.ApplyChanges();

        screenH = device.PresentationParameters.BackBufferHeight; //since we may have been slightly denied our preference...
        screenW = device.PresentationParameters.BackBufferWidth;
        setSize(screenW, screenH);

        IsPortrait = (screenH > screenW);


        sb = new SpriteBatch(device);

        if (!ApplicationSetsScale)
        {
            float scale;
            if (screenH > 2100 && screenW > 3800)
                scale = 3f;
            else if (screenH > 1400 && screenW > 2500)
                scale = 2f;
            else if (screenH > 1000 && screenW > 1900)
                scale = 1.5f;
            else if (screenH > 950 && screenW > 1400)
                scale = 1.25f;
            else if (screenH > 700 && screenW > 1250)
                scale = 1f;
            else
                scale = .75f;
            _SetScale(scale);
        }

        //if (pixelRatio > 1)
        //render = new RenderTarget2D(device, width, height, 1, device.DisplayMode.Format); // use our scaled screen size

        _resizing = false;
    }


    #region Full screen

    public static void ToggleFullScreen()
    {
        SetFullScreen(!IsFullScreen);
    }

    // call post-initialize
    public static void SetFullScreen(bool full)
    {
        _graphics.IsFullScreen = full;
        if (!full && _autoDisplayMode != eAutoDisplaySizeMode.None)
        {
            var size = GetAutoSize(_autoDisplayMode);
            Resize(false, size.X, size.Y);
        }
        else
            _graphics.ApplyChanges();
    }
    public static bool IsFullScreen => _graphics.IsFullScreen;
    #endregion



    //quietly changes the size
    public static void WindowChanged(int w, int h)
    {
        int oldW = screenW;
        int oldH = screenH;

        screenH = h; //since we may have been slightly denied our preference...
        screenW = w;
        setSize(screenW, screenH);

        IsPortrait = (screenH > screenW);

        _graphics.PreferredBackBufferWidth = w;
        _graphics.PreferredBackBufferHeight = h;
        _graphics.ApplyChanges();

        screenH = device.PresentationParameters.BackBufferHeight; //since we may have been slightly denied our preference...
        screenW = device.PresentationParameters.BackBufferWidth;


        if (!ApplicationSetsScale)
        {
            float scale;
            if (screenH > 2100 && screenW > 3800)
                scale = 3f;
            else if (screenH > 1400 && screenW > 2500)
                scale = 2f;
            else if (screenH > 1080 && screenW > 1920)
                scale = 1.5f;
            else if (screenH > 950 && screenW > 1400)
                scale = 1.25f;
            else if (screenH > 700 && screenW > 1250)
                scale = 1f;
            else
                scale = .75f;
            _SetScale(scale);
        }
    }
    public static void SetScale(float scale)
    {
        if (ApplicationSetsScale)
            _SetScale(scale);
    }

    static void _SetScale(float scale)
    {
        float temp = _scale;
        _scale = Math.Max(scale, MinScale);
    }

    private static void setSize(int w, int h)
    {
        Width = w;
        Height = h;
        SafeLeft = w / 10; // safe area stuff
        SafeTop = h / 10;
        SafeRight = w * 9 / 10;
        SafeBottom = h * 9 / 10;
        SafeWidth = w * 8 / 10;
        SafeHeight = h * 8 / 10;
        AspectRatio = (float)w / h;
    }
}