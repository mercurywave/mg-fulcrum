using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public enum eDimension { Left = 1, Top = 2, Right = 4, Bottom = 8, Width = 16, Height = 32 }
public enum eEdge { Left = 1, Top = 2, Right = 4, Bottom = 8 }
public enum eSize { Width = 16, Height = 32 }
[Flags]
internal enum eDimensionFlag { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8, Width = 16, Height = 32 }

public interface ILayout : IComponent
{
    public OLayout Layout { get; set; }
    public void OnLayout();
}
public interface IViewport : ILayout
{
    // scroll offset for child components from the top left of the viewport
    // e.g. 100,0 is scrolled down 100 pixels
    // width and height are scrollable area, set by framework to the maximum of children by default
    public OLayout Viewport { get; set; }
}

public class OLayout
{
    internal Rectangle Rect = new Rectangle(0, 0, 0, 0);
    internal eDimensionFlag BoundDims = eDimensionFlag.None;
    public int Left
    {
        get => Rect.X;
        set => Bind(eEdge.Left, value);
    }
    public int Top
    {
        get => Rect.Y;
        set => Bind(eEdge.Top, value);
    }
    public int Width
    {
        get => Rect.Width;
        set => Bind(eSize.Width, value);
    }
    public int Height
    {
        get => Rect.Height;
        set => Bind(eSize.Height, value);
    }
    public int Right
    {
        get => Rect.Right;
        set => Bind(eEdge.Right, value);
    }
    public int Bottom
    {
        get => Rect.Bottom;
        set => Bind(eEdge.Bottom, value);
    }

    private bool IsDimBound(eEdge edge) => (BoundDims & edge.AsFlag()) > 0;
    private bool IsDimBound(eSize size) => (BoundDims & size.AsFlag()) > 0;

    public bool IsSizeBound(eSize size) =>
        IsDimBound(size) || (IsDimBound(size.AxisEdge(false)) && IsDimBound(size.AxisEdge(false)));
    public bool IsEdgeBound(eEdge edge) =>
        IsDimBound(edge) || (IsDimBound(edge.Opposite()) && IsDimBound(edge.AxisSize()));

    public OLayout Bind(eEdge edge, int value)
    {
        if(IsEdgeBound(edge)) return this;
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
        if(IsSizeBound(size)) return this;
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
        GLayoutEngine.Dirty = true;
    }
    internal void ShiftEdge(eEdge edge, int value)
    {
        var replacement = Rect.CopyShiftEdge(edge, value);
        if (replacement == Rect) return;
        Rect = replacement;
        GLayoutEngine.Dirty = true;
    }
    internal void StretchFromEdge(eSize size, int value, bool fromHighEdge)
    {
        var replacement = Rect.CopyStretchSizeFromEdge(size, value, fromHighEdge);
        if (replacement == Rect) return;
        Rect = replacement;
        GLayoutEngine.Dirty = true;
    }

    public void ClearBinds() =>
        BoundDims = eDimensionFlag.None;

    #region Transform
    
    // these are both additive so that they can be started at 0
    public Vector2 RelativeTransformPos = Vector2.Zero;
    public Vector2 RelativeTransformSize = Vector2.Zero;
    public FRectangle TransformedLayout =>
        new FRectangle(
            Rect.GetTopLeft().ToVector2() + RelativeTransformPos,
            Rect.Size.ToVector2() + RelativeTransformSize
        );
    public void TransformScale(Vector2 scaleMod)
    {
        RelativeTransformSize = Rect.Size.ToVector2() * scaleMod;
    }

    public void SetAbsoluteTransformPos(Vector2 pos)
    {
        RelativeTransformPos = pos - Rect.GetTopLeft().ToVector2();
    }

    public void SetAbsoluteTransform(FRectangle rect)
    {
        RelativeTransformPos = rect.TopLeft - Rect.GetTopLeft().ToVector2();
        RelativeTransformSize = rect.Size - Rect.Size.ToVector2();
    }
        
    #endregion
}