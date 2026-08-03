using System;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public static class GColor
{
    public static Color popupBackground = new Color(76, 46, 74);
    public static Color popupFrame = new Color(81, 16, 22); // between border and background
    public static Color popupBorder = new Color(204, 165, 51);
    public static Color popupTitle = new Color(229, 229, 229);
    public static Color popupFont = new Color(229, 229, 229);
    public static Color popupHoverFont = new Color(247, 202, 61);

    public static Color mouseoverOverlay = new Color(40, 40, 40, 40);
    public static Color selectionOverlay = new Color(80, 80, 80, 100);
    public static Color shieldOfJustice = new Color(32, 32, 32) * .8f;

    public static Color focusBorder = new Color(255, 188, 61); // for things with border that need to show focus
    public static Color focusText = new Color(255, 188, 61); // for text-only things that need to show focus

    public static Color FromHex(string clr) => TryFromHex(clr).Value;
    public static Color? TryFromHex(string clr)
    {
        // parses 2 hex digits starting from i
        if (clr.Contains('#')) clr = clr.Replace("#", "");
        if (clr == "") return Color.White;
        byte Parse(int i) => Convert.ToByte(GUtil.BoundedSubstr(clr, i, 2), 16);
        switch (clr.Length)
        {
            case 1:
            case 2:
                return new Color(Parse(0), Parse(0), Parse(0));
            case 6:
                return new Color(Parse(0), Parse(2), Parse(4));
            case 8:
                return new Color(Parse(0), Parse(2), Parse(4), Parse(6));
            default: return null;
        }
    }
    public static string ToHex(Color clr)
    {
        string hex(byte b) => b.ToString("X2");
        string output = hex(clr.R) + hex(clr.G) + hex(clr.B);
        if (clr.A != 255)
            output += hex(clr.A);
        return output;
    }
    // ratio is 0-1, the value to darken by (so .2 means darken absolute 20%)
    // isn't super careful with the math, so may crush colors or result in weird results
    public static Color Darken(Color clr, float ratio)
    {
        var hsv = ToHSV(clr);
        hsv.Z -= ratio * 100f;
        return FromHSV(hsv);
    }
    public static Color Lighten(Color clr, float ratio)
    {
        var hsv = ToHSV(clr);
        hsv.Z += ratio * 100f;
        return FromHSV(hsv);
    }
    #region color math stuff

    // H,S,V,A - note: this is different from HSL
    // hue is 0-360, s/v are 0-100, and alpha is 0-255
    public static Vector4 ToHSV(Color rgb)
        => HSLtoHSV(RGBtoHSL(rgb));
    static Vector4 HSLtoHSV(Vector4 hsl)
    {
        var modifiedS = hsl.Y / 100f;
        var modifiedL = hsl.Z / 100f;

        var hsvV = modifiedL + modifiedS * Math.Min(modifiedL, 1 - modifiedL);

        var hsvS = (hsvV == 0) ? 0 : 2 * (1 - modifiedL / hsvV);

        return new Vector4(hsl.X, hsvS * 100, hsvV * 100, hsl.W);
    }
    static Vector4 RGBtoHSL(Color rgb)
    {
        float h, s, l;

        var modifiedR = rgb.R / 255f;
        var modifiedG = rgb.G / 255f;
        var modifiedB = rgb.B / 255f;

        var min = Math.Min(modifiedR, Math.Min(modifiedG, modifiedB));
        var max = Math.Max(modifiedR, Math.Max(modifiedG, modifiedB));
        var delta = max - min;
        l = (min + max) / 2;

        if (delta == 0)
        {
            h = 0;
            s = 0;
        }
        else
        {
            s = (l <= 0.5) ? (delta / (min + max)) : (delta / (2 - max - min));

            if (modifiedR == max)
            {
                h = (modifiedG - modifiedB) / 6 / delta;
            }
            else if (modifiedG == max)
            {
                h = (1f / 3) + ((modifiedB - modifiedR) / 6 / delta);
            }
            else
            {
                h = (2f / 3) + ((modifiedR - modifiedG) / 6 / delta);
            }

            h = (h < 0) ? ++h : h;
            h = (h > 1) ? --h : h;
        }

        return new Vector4(h * 360, s * 100, l * 100, rgb.A);
    }
    public static Color FromHSV(Vector4 hsv)
        => FromHSL(HSVtoHSL(hsv));
    static Vector4 HSVtoHSL(Vector4 hsv)
    {
        float modifiedS, modifiedV, hslS, hslL;

        modifiedS = hsv.Y / 100f;
        modifiedV = hsv.Z / 100f;

        hslL = modifiedV * (1 - modifiedS / 2f);
        hslS = (hslL == 0 || hslL == 1) ? 0 : (modifiedV - hslL) / Math.Min(hslL, 1 - hslL);

        return new Vector4(hsv.X, hslS * 100f, hslL * 100f, hsv.W);
    }
    static Color FromHSL(Vector4 hsl)
    {
        float modifiedH, modifiedS, modifiedL, r = 1, g = 1, b = 1, q, p;

        modifiedH = hsl.X / 360f;
        modifiedS = hsl.Y / 100f;
        modifiedL = hsl.Z / 100f;

        q = (modifiedL < 0.5) ? modifiedL * (1 + modifiedS) : modifiedL + modifiedS - modifiedL * modifiedS;
        p = 2 * modifiedL - q;

        if (modifiedL == 0)  // if the lightness value is 0 it will always be black
        {
            r = 0;
            g = 0;
            b = 0;
        }
        else if (modifiedS != 0)
        {
            r = GetHue(p, q, modifiedH + 1f / 3);
            g = GetHue(p, q, modifiedH);
            b = GetHue(p, q, modifiedH - 1f / 3);
        }
        else // ensure greys are not converted to white
        {
            r = modifiedL;
            g = modifiedL;
            b = modifiedL;
        }

        return new Color((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255), (byte)hsl.W);
    }
    static float GetHue(float p, float q, float t)
    {
        float value = p;
        if (t < 0) t++;
        if (t > 1) t--;

        if (t < 1f / 6)
        {
            value = p + (q - p) * 6 * t;
        }
        else if (t < 1f / 2)
        {
            value = q;
        }
        else if (t < 2f / 3)
        {
            value = p + (q - p) * (2f / 3 - t) * 6;
        }

        return value;
    }
    #endregion
}