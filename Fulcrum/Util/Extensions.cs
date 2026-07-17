using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public static class RectangleExtensions
{
    /// <summary>
    /// Get a dimension value from the rectangle.
    /// </summary>
    public static int GetDim(this Rectangle rect, eDimension dim) =>
        dim switch
        {
            eDimension.Left => rect.X,
            eDimension.Top => rect.Y,
            eDimension.Right => rect.Right,
            eDimension.Bottom => rect.Bottom,
            eDimension.Width => rect.Width,
            eDimension.Height => rect.Height,
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Get a dimension value from the rectangle.
    /// </summary>
    public static int GetDim(this Rectangle rect, eEdge edge) =>
        edge switch
        {
            eEdge.Left => rect.X,
            eEdge.Top => rect.Y,
            eEdge.Right => rect.Right,
            eEdge.Bottom => rect.Bottom,
            _ => throw new NotImplementedException(),
        };
    /// <summary>
    /// Get a dimension value from the rectangle.
    /// </summary>
    public static int GetDim(this Rectangle rect, eSize dim) =>
        dim switch
        {
            eSize.Width => rect.Width,
            eSize.Height => rect.Height,
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Set an edge dimension, keeping the opposite edge stationary
    /// </summary>
    public static Rectangle CopyStretchEdge(this Rectangle rect, eEdge edge, int value) =>
        edge switch
        {
            eEdge.Left => new Rectangle(value, rect.Y, rect.Width - (value - rect.X), rect.Height),
            eEdge.Right => new Rectangle(rect.X, rect.Y, value - rect.X, rect.Height),
            eEdge.Top => new Rectangle(rect.X, value, rect.Width, rect.Height - (value - rect.Y)),
            eEdge.Bottom => new Rectangle(rect.X, rect.Y, rect.Width, value - rect.Y),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Set an edge dimension, keeping the opposite edge stationary
    /// </summary>
    public static Rectangle CopyStretchSizeFromEdge(this Rectangle rect, eSize size, int value, bool fromHighEdge) =>
        size.AxisEdge(!fromHighEdge) switch // which edge is being modified?
        {
            eEdge.Left => new Rectangle(rect.Right - value, value, rect.Y, rect.Height),
            eEdge.Right => new Rectangle(rect.X, rect.Y, value, rect.Height),
            eEdge.Top => new Rectangle(rect.X, rect.Bottom - value, rect.Width, value),
            eEdge.Bottom => new Rectangle(rect.X, rect.Y, rect.Width, value),
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Set an edge dimension, moving the opposite edge
    /// </summary>
    public static Rectangle CopyShiftEdge(this Rectangle rect, eEdge edge, int value) =>
        edge switch
        {
            eEdge.Left => new Rectangle(value, rect.Y, rect.Width, rect.Height),
            eEdge.Right => new Rectangle(rect.X - value, rect.Y, rect.Width, rect.Height),
            eEdge.Top => new Rectangle(rect.X, value, rect.Width, rect.Height),
            eEdge.Bottom => new Rectangle(rect.X, rect.Y - value, rect.Width, rect.Height),
            _ => throw new NotImplementedException(),
        };

}



public static class DimensionExtensions
{
    /// <summary>
    /// Get opposite dimension along the axis (e.g. left -> right)
    /// </summary>
    public static eEdge Opposite(this eEdge edge) =>
        edge switch
        {
            eEdge.Left => eEdge.Right,
            eEdge.Top => eEdge.Bottom,
            eEdge.Right => eEdge.Left,
            eEdge.Bottom => eEdge.Top,
            _ => throw new InvalidOperationException(),
        };

    /// <summary>
    /// Is either length/width
    /// </summary>
    public static bool IsSize(this eDimension dim) =>
        dim == eDimension.Width || dim == eDimension.Height;

    /// <summary>
    /// Is an edge of a rectangle
    /// </summary>
    public static bool IsEdge(this eDimension dim) =>
        dim != eDimension.Width && dim != eDimension.Height;

    /// <summary>
    /// Get the dimension that represents the size dim for this axis (e.g. left -> width)
    /// </summary>
    public static eSize AxisSize(this eEdge edge) =>
        edge switch
        {
            eEdge.Left or eEdge.Right => eSize.Width,
            eEdge.Top or eEdge.Bottom => eSize.Height,
            _ => throw new InvalidOperationException(),
        };

    /// <summary>
    /// Get the dimension that represents the size of this edge (e.g. the height of the left side)
    /// </summary>
    public static eSize EdgeSize(this eEdge edge) =>
        edge switch
        {
            eEdge.Left or eEdge.Right => eSize.Height,
            eEdge.Top or eEdge.Bottom => eSize.Width,
            _ => throw new InvalidOperationException(),
        };

    /// <summary>
    /// Gets the edge on either end of the size (e.g. Width => left/right)
    /// <paramref name="high"/> is true for the higher edge</paramref>
    /// </summary>
    public static eEdge AxisEdge(this eSize size, bool high = false) =>
        size switch
        {
            eSize.Width => high ? eEdge.Right : eEdge.Left,
            eSize.Height => high ? eEdge.Top : eEdge.Bottom,
            _ => throw new InvalidOperationException(),
        };

    internal static eDimensionFlag AsFlag(this eDimension dim) =>
        (eDimensionFlag)(int)dim;
    internal static eDimensionFlag AsFlag(this eSize size) =>
        (eDimensionFlag)(int)size;
    internal static eDimensionFlag AsFlag(this eEdge edge) =>
        (eDimensionFlag)(int)edge;
    public static eEdge AsEdge(this eDimension dim)
    {
        Debug.Assert(dim.IsEdge());
        return (eEdge)(int)dim;
    }
    public static eSize AsSize(this eDimension dim)
    {
        Debug.Assert(dim.IsSize());
        return (eSize)(int)dim;
    }
    public static eDimension AsDim(this eSize size) =>
        (eDimension)(int)size;
    public static eDimension AsDim(this eEdge edge) =>
        (eDimension)(int)edge;
}