using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public enum eDimension { Left = 1, Top = 2, Right = 4, Bottom = 8, Width = 16, Height = 32 }
public enum eEdge { Left = 1, Top = 2, Right = 4, Bottom = 8 }
public enum eSize { Width = 16, Height = 32 }
[Flags]
internal enum eDimensionFlag { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8, Width = 16, Height = 32 }

public interface ILayout
{
    OLayout Layout { get; set; }
    public int Left => Layout.Left;
    public int Top => Layout.Top;
    public int Width => Layout.Width;
    public int Height => Layout.Height;
    public int Right => Layout.Right;
    public int Bottom => Layout.Bottom;
}

public class OLayout
{
    internal Rectangle Rect;
    internal bool LayoutDirty;
    public int Left => Rect.X;
    public int Top => Rect.Y;
    public int Width => Rect.Width;
    public int Height => Rect.Height;
    public int Right => Rect.Right;
    public int Bottom => Rect.Bottom;

    
    public OConstraint Bind(eEdge edge, int value)
    {
        var constraint = new OConstraint(this, eDimensionFlag.None);
        return constraint.Bind(edge, value);
    }
    public OConstraint Bind(eSize size, int value)
    {
        var constraint = new OConstraint(this, eDimensionFlag.None);
        return constraint.Bind(size, value);
    }
    public OConstraint Bind(eDimension dim, int value)
    {
        var constraint = new OConstraint(this, eDimensionFlag.None);
        return constraint.Bind(dim, value);
    }

    internal void StretchEdge(eEdge edge, int value)
    {
        var replacement = Rect.CopyStretchEdge(edge, value);
        if (replacement == Rect) return;
        Rect = replacement;
        LayoutDirty = true;
    }
    internal void ShiftEdge(eEdge edge, int value)
    {
        var replacement = Rect.CopyShiftEdge(edge, value);
        if (replacement == Rect) return;
        Rect = replacement;
        LayoutDirty = true;
    }
    internal void StretchFromEdge(eSize size, int value, bool fromHighEdge)
    {
        var replacement = Rect.CopyStretchSizeFromEdge(size, value, fromHighEdge);
        if (replacement == Rect) return;
        Rect = replacement;
        LayoutDirty = true;
    }

}

public struct OConstraint
{
    internal OLayout Layout;
    internal eDimensionFlag DimsSet;
    internal OConstraint(OLayout layout, eDimensionFlag dims)
    {
        Layout = layout;
        DimsSet = dims;
    }
    internal bool IsDimSet(eDimension dim) => (DimsSet & dim.AsFlag()) > 0;
    internal bool IsDimSet(eEdge edge) => (DimsSet & edge.AsFlag()) > 0;
    internal bool IsDimSet(eSize size) => (DimsSet & size.AsFlag()) > 0;

    public OConstraint Bind(eEdge edge, int value)
    {
        var opp = IsDimSet(edge.Opposite());
        var size = IsDimSet(edge.AxisSize());
        Debug.Assert(!opp || !size);
        if (opp)
            Layout.StretchEdge(edge, value);
        else
            Layout.ShiftEdge(edge, value);
        return new OConstraint(Layout, DimsSet | edge.AsFlag());
    }
    public OConstraint Bind(eSize size, int value)
    {
        var min = IsDimSet(size.AxisEdge(false));
        var max = IsDimSet(size.AxisEdge(true));
        Debug.Assert(!min || !max);
        Layout.StretchFromEdge(size, value, max);
        return new OConstraint(Layout, DimsSet | size.AsFlag());
    }
    public OConstraint Bind(eDimension dim, int value) =>
        dim.IsSize() ? Bind(dim.AsSize(), value) : Bind(dim.AsEdge(), value);

}