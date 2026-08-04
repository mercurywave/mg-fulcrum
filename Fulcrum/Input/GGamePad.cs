using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Fulcrum;

public static class GGamePad
{
    public delegate void NewPlayerJoinedHandler(OGamePad pad);

    static OGamePad[] _pads =
    {
        new OGamePad(PlayerIndex.One),
        new OGamePad(PlayerIndex.Two),
        new OGamePad(PlayerIndex.Three),
        new OGamePad(PlayerIndex.Four)
    };
    static ePadButton _newPlayer = ePadButton.Start;

    public static void Update()
    {
        foreach (OGamePad pad in _pads)
            pad.Update();

        foreach (OGamePad pad in _pads)
        {
            if (!pad.Assigned && pad.WasButtonPressed(_newPlayer))
            {
                GCore.ComponentTree.WalkTree<IListenForPlayerJoin>(c => c.GGamePad_NewPlayerJoined(pad));
            }
        }
    }

    public static IEnumerable<OGamePad> GamePads()
    {
        return _pads;
    }

    public static bool AnyControllerPressing(params ePadButton[] buttons)
    {
        return _pads.Any(o => o.WasButtonPressed(buttons));
    }

    public static bool AnyControllersPluggedIn => _pads.Any(p => p.Connected);

    public static void SetNewPlayerButton(ePadButton button) { _newPlayer = button; }
}

public enum ePadButton { Up, Down, Left, Right, A, B, X, Y, Start, Back, LBumper, RBumper, BigButton, LStick, RStick };
public enum ePadSide { Left, Right };
public enum ePadMenuDirection { Up, Down, Left, Right, None };

public class OGamePad
{
    bool _stickReturned;

    public bool Assigned = false; // caller must claim the controller for themselves
    public bool Connected;

    PlayerIndex _index;
    public GamePadState _previous, _current;

    public int Index => (int)_index + 1;

    Dictionary<ePadButton, Tick> _buttonsHeld = new Dictionary<ePadButton, Tick>();

    public OGamePad(PlayerIndex index)
    {
        _index = index;
    }

    internal void Update()
    {
        var toClean = new List<ePadButton>();
        foreach (var b in _buttonsHeld.Keys)
            if (!IsButtonHeld(b))
                toClean.Add(b);
        foreach (var b in toClean)
            _buttonsHeld.Remove(b);

        _previous = _current; // does this need to happen if it isn't connected or polling?
        _current = GamePad.GetState(_index);

        Connected = _current.IsConnected;

        foreach (var b in GUtil.EnumOptions<ePadButton>())
            if (IsButtonHeld(b) && !_buttonsHeld.ContainsKey(b))
                _buttonsHeld.Add(b, GCore.FrameStart);

        _cacheMovement = null;
    }

    public bool WasButtonPressed(ePadButton button)
    {
        ButtonState curr = GetState(_current, button);
        ButtonState prev = GetState(_previous, button);
        return curr == ButtonState.Pressed && prev == ButtonState.Released;
    }

    public bool WasButtonPressed(params ePadButton[] buttons)
    {
        foreach (var b in buttons)
            if (WasButtonPressed(b))
                return true;
        return false;
    }

    public Tick HowLongHasButtonHeld(ePadButton button)
    {
        if (!_buttonsHeld.ContainsKey(button)) return Tick.Zero;
        return GCore.FrameStart - _buttonsHeld[button];
    }

    public bool IsButtonHeld(ePadButton button)
    {
        ButtonState curr = GetState(_current, button);
        return curr == ButtonState.Pressed;
    }

    public bool IsButtonHeld(ePadButton button, out Tick heldTime)
    {
        heldTime = HowLongHasButtonHeld(button);
        return IsButtonHeld(button);
    }

    public bool WasButtonReleased(ePadButton button)
    {
        ButtonState curr = GetState(_current, button);
        ButtonState prev = GetState(_previous, button);
        return curr == ButtonState.Released && prev == ButtonState.Pressed;
    }

    public bool WasButtonReleased(ePadButton button, out Tick heldTime)
    {
        heldTime = HowLongHasButtonHeld(button);
        return WasButtonReleased(button);
    }

    public float TriggerValue(ePadSide side)
    {
        if (side == ePadSide.Left) return _current.Triggers.Left;
        else return _current.Triggers.Right;
    }

    public bool WasTriggerPulled(ePadSide side, float threshold = .1f)
    {
        float prev, curr;
        if (side == ePadSide.Left)
        {
            prev = _previous.Triggers.Left;
            curr = _current.Triggers.Left;
        }
        else
        {
            prev = _previous.Triggers.Right;
            curr = _current.Triggers.Right;
        }
        return prev < threshold && curr > threshold;
    }

    // left stick or pad, check any direction
    // you proably don't want to mix the various menu movements, they share variables
    ePadMenuDirection? _cacheMovement = null; // cahce so multiple listeners don't step on each others toes
    public ePadMenuDirection CheckMenuMovement()
    {
        if (_cacheMovement != null) return _cacheMovement.Value;
        _cacheMovement = _checkMenuMovement();
        return _cacheMovement.Value;
    }

    private ePadMenuDirection _checkMenuMovement()
    {
        if (WasButtonPressed(ePadButton.Up)) return ePadMenuDirection.Up;
        if (WasButtonPressed(ePadButton.Down)) return ePadMenuDirection.Down;
        if (WasButtonPressed(ePadButton.Left)) return ePadMenuDirection.Left;
        if (WasButtonPressed(ePadButton.Right)) return ePadMenuDirection.Right;
        Vector2 stick = _current.ThumbSticks.Left;
        float dist = stick.Length();
        if (!_stickReturned && dist < .25f)
            _stickReturned = true;
        else if (_stickReturned && dist > .5f)
        {
            _stickReturned = false;
            if (Math.Abs(stick.X) > Math.Abs(stick.Y))
            {
                if (stick.X > 0) return ePadMenuDirection.Right;
                else return ePadMenuDirection.Left;
            }
            else
            {
                if (stick.Y < 0) return ePadMenuDirection.Down; // vertical is opposite of screen
                else return ePadMenuDirection.Up;
            }
        }
        return ePadMenuDirection.None;
    }

    public Vector2 Stick(ePadSide side)
    {
        Vector2 dir;
        if (side == ePadSide.Left) dir = _current.ThumbSticks.Left;
        else dir = _current.ThumbSticks.Right;
        if (dir.LengthSquared() < .05) return Vector2.Zero;
        return dir;
    }

    ButtonState GetState(GamePadState state, ePadButton button)
    {
        GamePadButtons butts = state.Buttons;
        GamePadDPad dpad = state.DPad;
        switch (button)
        {
            case ePadButton.Up:
                return dpad.Up;
            case ePadButton.Down:
                return dpad.Down;
            case ePadButton.Left:
                return dpad.Left;
            case ePadButton.Right:
                return dpad.Right;
            case ePadButton.A:
                return butts.A;
            case ePadButton.B:
                return butts.B;
            case ePadButton.X:
                return butts.X;
            case ePadButton.Y:
                return butts.Y;
            case ePadButton.Start:
                return butts.Start;
            case ePadButton.Back:
                return butts.Back;
            case ePadButton.LBumper:
                return butts.LeftShoulder;
            case ePadButton.RBumper:
                return butts.RightShoulder;
            case ePadButton.BigButton:
                return butts.BigButton;
            case ePadButton.LStick:
                return butts.LeftStick;
            case ePadButton.RStick:
                return butts.RightStick;
            default:
                throw new NotImplementedException();
        }
    }
    ePadButton DirectionToButton(ePadMenuDirection dir)
    {
        if (dir == ePadMenuDirection.Up) return ePadButton.Up;
        if (dir == ePadMenuDirection.Down) return ePadButton.Down;
        if (dir == ePadMenuDirection.Left) return ePadButton.Left;
        if (dir == ePadMenuDirection.Right) return ePadButton.Right;
        throw new Exception("what were you expecting?");
    }
}
public interface IListenForPlayerJoin
{
    // if you want to handle this, you want to set the gamepad to assigned
    void GGamePad_NewPlayerJoined(OGamePad pad);
}