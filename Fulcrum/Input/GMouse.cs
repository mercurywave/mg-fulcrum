using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fulcrum;

public enum eMouseIcon { Default, Interactive, Disabled, Text, Texture, Dragging, Draggable }
public static class GMouse
{
    static MouseState _currentstate;
    static MouseState _laststate;
    public static Texture2D CurrentPointer = null;
    public static bool OutsideOffApp;
    public static bool CursorHidden = false; // if you tap, the mouse will be hidden
    public static bool Disabled = false; // managed by game - disables mouse-based interactions in ifaces, position and clicks are still updated

    static Tick CLICK_SCAN_MAX = new Tick(333); // x frames after click scan starts, trigger click
    static Tick HOLD_TIME = CLICK_SCAN_MAX - new Tick(83); // x frames after click scan starts, trigger hold
    static LatchTimer _holdTimer = new LatchTimer();
    static LatchTimer _rHoldTimer = new LatchTimer();
    static LatchTimer _idleTimer = new LatchTimer();
    static PulseTimer _sinceLastClick = new PulseTimer();

    static bool _click, _rClick, _held, _rHeld, _doubleClick;

    static Point startClick;

    public static void Initialize()
    {
        _currentstate = Mouse.GetState();
        _laststate = _currentstate;
    }

    public static void Update()
    {
        _laststate = _currentstate;
        _currentstate = Mouse.GetState();

        OutsideOffApp = _currentstate.X < 0 || _currentstate.Y < 0 || _currentstate.X > GScreen.Width || _currentstate.Y > GScreen.Height;
        if (OutsideOffApp)
        {
            _click = false;
            _rClick = false;
            _doubleClick = false;

            _held = false;
            _rHeld = false;

            _holdTimer.Reset();
            _rHoldTimer.Reset();
            _idleTimer.Reset();
        }
        else
        {
            if (IsDown() || IsRDown()) startClick = new Point(ScreenX, ScreenY);
            _click = WasReleased()
                && !_holdTimer.IsEngagedFor(HOLD_TIME)
                && new Point(ScreenX, ScreenY).AngularDistanceTo(startClick) < 10 * GScreen.Scale;

            _idleTimer.Update(_currentstate.X == _laststate.X && _currentstate.Y == _laststate.Y);

            _doubleClick = false;
            if (_click)
            {
                if (_sinceLastClick.EngagedRecently(CLICK_SCAN_MAX)) _doubleClick = true;
                _sinceLastClick.Update(true);
            }

            _holdTimer.Update(IsDown());
            _rHoldTimer.Update(IsRDown());
            if (IsDown())
                Console.WriteLine($"Mouse: {_holdTimer.TimeEngaged().Frame} frames held");
            _held = IsDown() && _holdTimer.IsEngagedFor(HOLD_TIME);
            _rHeld = IsRDown() && _rHoldTimer.IsEngagedFor(HOLD_TIME);
        }
        if (GPerf.Enabled)
        {
            if (IsClicked()) GPerf.MarkKeyFrame("Click");
        }
    }


    public static bool IsClicked() => _click;
    public static bool IsRClicked() => _rClick;

    public static bool IsDoubleClicked() => _doubleClick;

    //Note: these don't work on fancy mice, like mine :(
    public static bool IsBackButtonClicked() => _currentstate.XButton1 == ButtonState.Pressed && _laststate.XButton1 == ButtonState.Released;
    public static bool IsForwardButtonClicked() => _currentstate.XButton2 == ButtonState.Pressed && _laststate.XButton2 == ButtonState.Released;

    // Has been held for a period
    public static bool IsHeld() => _held; // you'll get these events prior to a click in some cases
    public static bool IsRHeld() => _rHeld;

    //mouse release event
    public static bool WasReleased() => _laststate.LeftButton == ButtonState.Pressed && _currentstate.LeftButton == ButtonState.Released;
    public static bool WasRReleased() => _laststate.RightButton == ButtonState.Pressed && _currentstate.RightButton == ButtonState.Released;

    // button is down
    public static bool IsDown() => _currentstate.LeftButton == ButtonState.Pressed;
    public static bool IsRDown() => _currentstate.RightButton == ButtonState.Pressed;

    // button is newly down
    public static bool IsNewlyDown() => _currentstate.LeftButton == ButtonState.Pressed && _laststate.LeftButton == ButtonState.Released;
    public static bool IsRNewlyDown() => _currentstate.RightButton == ButtonState.Pressed && _laststate.RightButton == ButtonState.Released;

    public static int ScrollDelta() => _currentstate.ScrollWheelValue - _laststate.ScrollWheelValue;

    public static int ScreenX => _currentstate.X;
    public static int ScreenY => _currentstate.Y;

    public static int Dx => _currentstate.X - _laststate.X;

    public static int Dy => _currentstate.Y - _laststate.Y;

    public static Point Pt => new Point(ScreenX, ScreenY);

    public static Tick MouseIdleDur => _idleTimer.TimeEngaged();

    public static void ForceCenter()
    {
        Mouse.SetPosition(GScreen.HalfWidth, GScreen.HalfHeight);
    }

    public static void ForceTowardCenter(float percent)
    {
        float x, y;
        x = (_currentstate.X - GScreen.HalfWidth) * percent + GScreen.HalfWidth;
        y = (_currentstate.Y - GScreen.HalfHeight) * percent + GScreen.HalfHeight;
        Mouse.SetPosition((int)x, (int)y);
    }
}