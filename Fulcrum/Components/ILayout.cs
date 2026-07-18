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
    public OLayout Layout { get; set; }
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
    internal eDimensionFlag BoundDims;
    internal static bool Dirty = false;
    public int Left => Rect.X;
    public int Top => Rect.Y;
    public int Width => Rect.Width;
    public int Height => Rect.Height;
    public int Right => Rect.Right;
    public int Bottom => Rect.Bottom;

    private bool IsDimBound(eEdge edge) => (BoundDims & edge.AsFlag()) > 0;
    private bool IsDimBound(eSize size) => (BoundDims & size.AsFlag()) > 0;

    public bool IsSizeBound(eSize size) =>
        IsDimBound(size) || (IsDimBound(size.AxisEdge(false)) && IsDimBound(size.AxisEdge(false)));
    public bool IsEdgeBound(eEdge edge) =>
        IsDimBound(edge) || (IsDimBound(edge.Opposite()) && IsDimBound(edge.AxisSize()));

    public OLayout Bind(eEdge edge, int value)
    {
        var opp = IsDimBound(edge.Opposite());
        var size = IsDimBound(edge.AxisSize());
        Debug.Assert(!opp || !size);
        if (opp)
            StretchEdge(edge, value);
        else
            ShiftEdge(edge, value);
        BoundDims |= edge.AsFlag();
        return this;
    }
    public OLayout Bind(eSize size, int value)
    {
        var min = IsDimBound(size.AxisEdge(false));
        var max = IsDimBound(size.AxisEdge(true));
        Debug.Assert(!min || !max);
        StretchFromEdge(size, value, max);
        BoundDims |= size.AsFlag();
        return this;
    }
    public OLayout Bind(eDimension dim, int value) =>
        dim.IsSize() ? Bind(dim.AsSize(), value) : Bind(dim.AsEdge(), value);

    internal void StretchEdge(eEdge edge, int value)
    {
        var replacement = Rect.CopyStretchEdge(edge, value);
        if (replacement == Rect) return;
        Rect = replacement;
        Dirty = true;
    }
    internal void ShiftEdge(eEdge edge, int value)
    {
        var replacement = Rect.CopyShiftEdge(edge, value);
        if (replacement == Rect) return;
        Rect = replacement;
        Dirty = true;
    }
    internal void StretchFromEdge(eSize size, int value, bool fromHighEdge)
    {
        var replacement = Rect.CopyStretchSizeFromEdge(size, value, fromHighEdge);
        if (replacement == Rect) return;
        Rect = replacement;
        Dirty = true;
    }
}

public interface ILayoutTransform : ILayout
{
    // these are both additive so that they can be started at 0
    public Vector2 RelativeTransformPos { get; set; }
    public Vector2 RelativeTransformSize { get; set; }
    public FRectangle TransformedLayout =>
        new FRectangle(
            Layout.Rect.GetTopLeft().ToVector2() + RelativeTransformPos,
            Layout.Rect.Size.ToVector2() + RelativeTransformSize
        );
    public void TransformScale(Vector2 scaleMod)
    {
        RelativeTransformSize = Layout.Rect.Size.ToVector2() * scaleMod;
    }
}

public class OLayoutTransform
{
    internal Vector4 Transform = Vector4.Zero;
}