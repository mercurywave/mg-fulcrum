
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace Fulcrum;

public static class GKeyboard
{
    static KeyboardState currentstate;
    static KeyboardState laststate;
    static Dictionary<Keys, Tick> _held = new Dictionary<Keys, Tick>();
    static Dictionary<Keys, Tick> _lastRepeat = new Dictionary<Keys, Tick>();

    public static void Initialize()
    {
        currentstate = Keyboard.GetState();
        laststate = currentstate;
    }

    public static void Update()
    {
        if (_held.Count > 0)
        {
            // if we released it last frame, clean it up
            List<Keys> clean = new List<Keys>();
            foreach (var k in _held.Keys)
                if (!IsHeld(k)) clean.Add(k);
            clean.ForEach(k => _held.Remove(k));
        }

        laststate = currentstate;
        currentstate = Keyboard.GetState();

        if(_lastRepeat.Count > 0)
        {
            List<Keys> clean = new List<Keys>();
            foreach (var k in _lastRepeat.Keys)
                if (!IsHeld(k)) clean.Add(k);
            clean.ForEach(k => _lastRepeat.Remove(k));
        }

        Keys[] keys = currentstate.GetPressedKeys();
        foreach (var k in keys)
            if (!_held.ContainsKey(k))
                _held.Add(k, GCore.FrameStart);

        if (GPerf.Enabled)
        {
            foreach (Keys k in currentstate.GetPressedKeys())
                GPerf.LogText("Key Pressed: " + k.ToString());
        }
    }

    public static bool WasPressed(Keys key)
    {
        return currentstate.IsKeyDown(key) && laststate.IsKeyUp(key);
    }
    public static bool WasAnyPressed(params Keys[] keys)
    {
        foreach (Keys key in keys)
            if (WasPressed(key))
                return true;
        return false;
    }

    public static bool WasRelease(Keys key)
    {
        return laststate.IsKeyDown(key) && currentstate.IsKeyUp(key);
    }
    public static bool WasAnyReleased(params Keys[] keys)
    {
        foreach (Keys key in keys)
            if (WasRelease(key))
                return true;
        return false;
    }

    public static bool WasReleased(Keys key, out Tick heldTime)
    {
        heldTime = HeldTime(key);
        return WasRelease(key);
    }

    public static Tick HeldTime(Keys key)
    {
        if (!_held.ContainsKey(key)) return Tick.Zero;
        return GCore.FrameStart - _held[key];
    }

    public static bool IsHeld(Keys key)
    {
        return currentstate.IsKeyDown(key);
    }

    public static bool IsHolding(Keys key)
    {
        return currentstate.IsKeyDown(key) && laststate.IsKeyDown(key);
    }

    public static bool IsHeld(Keys key, out Tick heldTime)
    {
        heldTime = HeldTime(key);
        return currentstate.IsKeyDown(key);
    }
    public static bool HoldingAny(params Keys[] keys)
    {
        foreach (Keys key in keys)
            if (IsHeld(key))
                return true;
        return false;
    }

    //returns true if just pressed, or at a regular frequency after being held for a time (like holding a button in Word)
    public static bool PressRepeat(Keys key, Tick? gapBetweenRepeats = null, Tick? delay = null)
    {
        gapBetweenRepeats ??= Tick.Ms(100);
        delay ??= Tick.Ms(300);
        if (_held.ContainsKey(key))
        {
            var durr = HeldTime(key);
            if (durr > delay || durr == Tick.Zero)
            {
                if(!_lastRepeat.ContainsKey(key)) // first time, just press it
                {
                    _lastRepeat.Add(key, GCore.FrameStart);
                    return true;
                }
                var durrSinceLast = GCore.FrameStart - _lastRepeat[key];
                if (durrSinceLast > gapBetweenRepeats) // time to repeat
                {
                    _lastRepeat[key] = GCore.FrameStart;
                    return true;
                }
            }
        }
        return false;
    }
    public static bool PressRepeatAny(Tick gapBetweenRepeats, Tick delay, params Keys[] keys)
    {
        foreach (Keys key in keys)
            if (PressRepeat(key, gapBetweenRepeats, delay))
                return true;
        return false;
    }
    public static bool PressRepeatAny(Tick gapBetweenRepeats, params Keys[] keys)
        => PressRepeatAny(gapBetweenRepeats, Tick.Ms(300), keys);
    public static bool PressRepeatAny(params Keys[] keys) => PressRepeatAny(Tick.Ms(100), keys);
    public static bool PressRepeatAny(List<Keys> keys) => keys.Any(k => PressRepeat(k));

    public static string GetText(string filterOut = "")
    {
        string text = "";
        Keys[] keys = currentstate.GetPressedKeys();
        foreach (var k in keys)
        {
            if (laststate.IsKeyUp(k))
            {
                char c = TranslateChar(k, HoldingAny(Keys.LeftShift, Keys.RightShift), false, true);
                if (c != (char)0 && filterOut.IndexOf(c) < 0)
                    text += c;
            }
        }
        return text;
    }

    public static char TranslateChar(Keys key, bool shift, bool capsLock, bool numLock)
    {
        switch (key)
        {
            case Keys.A: return TranslateAlphabetic('a', shift, capsLock);
            case Keys.B: return TranslateAlphabetic('b', shift, capsLock);
            case Keys.C: return TranslateAlphabetic('c', shift, capsLock);
            case Keys.D: return TranslateAlphabetic('d', shift, capsLock);
            case Keys.E: return TranslateAlphabetic('e', shift, capsLock);
            case Keys.F: return TranslateAlphabetic('f', shift, capsLock);
            case Keys.G: return TranslateAlphabetic('g', shift, capsLock);
            case Keys.H: return TranslateAlphabetic('h', shift, capsLock);
            case Keys.I: return TranslateAlphabetic('i', shift, capsLock);
            case Keys.J: return TranslateAlphabetic('j', shift, capsLock);
            case Keys.K: return TranslateAlphabetic('k', shift, capsLock);
            case Keys.L: return TranslateAlphabetic('l', shift, capsLock);
            case Keys.M: return TranslateAlphabetic('m', shift, capsLock);
            case Keys.N: return TranslateAlphabetic('n', shift, capsLock);
            case Keys.O: return TranslateAlphabetic('o', shift, capsLock);
            case Keys.P: return TranslateAlphabetic('p', shift, capsLock);
            case Keys.Q: return TranslateAlphabetic('q', shift, capsLock);
            case Keys.R: return TranslateAlphabetic('r', shift, capsLock);
            case Keys.S: return TranslateAlphabetic('s', shift, capsLock);
            case Keys.T: return TranslateAlphabetic('t', shift, capsLock);
            case Keys.U: return TranslateAlphabetic('u', shift, capsLock);
            case Keys.V: return TranslateAlphabetic('v', shift, capsLock);
            case Keys.W: return TranslateAlphabetic('w', shift, capsLock);
            case Keys.X: return TranslateAlphabetic('x', shift, capsLock);
            case Keys.Y: return TranslateAlphabetic('y', shift, capsLock);
            case Keys.Z: return TranslateAlphabetic('z', shift, capsLock);

            case Keys.D1: return shift ? '!' : '1';
            case Keys.D2: return shift ? '@' : '2';
            case Keys.D3: return shift ? '#' : '3';
            case Keys.D4: return shift ? '$' : '4';
            case Keys.D5: return shift ? '%' : '5';
            case Keys.D6: return shift ? '^' : '6';
            case Keys.D7: return shift ? '&' : '7';
            case Keys.D8: return shift ? '*' : '8';
            case Keys.D9: return shift ? '(' : '9';
            case Keys.D0: return shift ? ')' : '0';

            case Keys.Add: return '+';
            case Keys.Divide: return '/';
            case Keys.Multiply: return '*';
            case Keys.Subtract: return '-';

            case Keys.Space: return ' ';
            case Keys.Tab: return '\t';
            case Keys.Enter: return '\n';

            case Keys.Decimal: if (numLock && !shift) return '.'; break;
            case Keys.NumPad0: if (numLock && !shift) return '0'; break;
            case Keys.NumPad1: if (numLock && !shift) return '1'; break;
            case Keys.NumPad2: if (numLock && !shift) return '2'; break;
            case Keys.NumPad3: if (numLock && !shift) return '3'; break;
            case Keys.NumPad4: if (numLock && !shift) return '4'; break;
            case Keys.NumPad5: if (numLock && !shift) return '5'; break;
            case Keys.NumPad6: if (numLock && !shift) return '6'; break;
            case Keys.NumPad7: if (numLock && !shift) return '7'; break;
            case Keys.NumPad8: if (numLock && !shift) return '8'; break;
            case Keys.NumPad9: if (numLock && !shift) return '9'; break;

            case Keys.OemBackslash: return shift ? '|' : '\\';
            case Keys.OemCloseBrackets: return shift ? '}' : ']';
            case Keys.OemComma: return shift ? '<' : ',';
            case Keys.OemMinus: return shift ? '_' : '-';
            case Keys.OemOpenBrackets: return shift ? '{' : '[';
            case Keys.OemPeriod: return shift ? '>' : '.';
            case Keys.OemPipe: return shift ? '|' : '\\';
            case Keys.OemPlus: return shift ? '+' : '=';
            case Keys.OemQuestion: return shift ? '?' : '/';
            case Keys.OemQuotes: return shift ? '"' : '\'';
            case Keys.OemSemicolon: return shift ? ':' : ';';
            case Keys.OemTilde: return shift ? '~' : '`';
        }
        return (char)0;
    }

    static char TranslateAlphabetic(char baseChar, bool shift, bool capsLock)
    {
        return (capsLock ^ shift) ? char.ToUpper(baseChar) : baseChar;
    }
}