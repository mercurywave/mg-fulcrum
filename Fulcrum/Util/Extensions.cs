using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

    public static Vector4 ToVector4(this Rectangle rect) =>
        new Vector4(rect.X, rect.Y, rect.Width, rect.Height);
    public static FRectangle ToFloatRect(this Rectangle rect)
        => new FRectangle(rect.X, rect.Y, rect.Width, rect.Height);
    public static Point GetTopLeft(this Rectangle rect)
        => new Point(rect.X, rect.Y);
    public static Point GetBottomRight(this Rectangle rect)
        => new Point(rect.X + rect.Width, rect.Y + rect.Height);
}

public static class PointExtensions
{
    public static int ManhattanDistanceTo(this Point a, Point b)
        => ManhattanDistance(a, b);
    public static int ManhattanDistance(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
    //eight directional movement
    public static int AngularDistanceTo(this Point a, Point b)
        => AngularDistance(a, b);
    public static int AngularDistance(Point a, Point b)
    {
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
    // as the crow flies
    public static float BirdDistanceTo(this Point a, Point b)
        => BirdDistance(a, b);
    public static float BirdDistance(Point a, Point b)
    {
        return (float)Math.Sqrt(Math.Pow(Math.Abs(a.X - b.X), 2) + Math.Pow(Math.Abs(a.Y - b.Y), 2));
    }
    // save a sqrt
    public static float BirdDistanceSquaredTo(this Point a, Point b)
        => BirdDistanceSquared(a, b);
    public static float BirdDistanceSquared(Point a, Point b)
    {
        return (float)(Math.Pow(Math.Abs(a.X - b.X), 2) + Math.Pow(Math.Abs(a.Y - b.Y), 2));
    }
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

public static class ListExtensions
{
    public static IEnumerable<U> FilterCast<T, U>(this IEnumerable<T> list) where U : T
    {
        var result = new List<U>();
        foreach (var item in list)
        {
            if (item is U uItem)
                result.Add(uItem);
        }
        return result;
    }
}

public static class DrawExtensions
{
    public static void Blit(this Texture2D tex, int x, int y)
        => GDraw.Blit(tex, new Vector2(x, y), Color.White);
    public static void Blit(this Texture2D tex, int x, int y, Color multiply)
        => GDraw.Blit(tex, new Vector2(x, y), multiply);
    public static void Blit(this Texture2D tex, Point topLeft)
        => GDraw.Blit(tex, topLeft.ToVector2(), Color.White);
    public static void Blit(this Texture2D tex, Point topLeft, Color multiply)
        => GDraw.Blit(tex, topLeft.ToVector2(), multiply);

    public static void BlitStretched(this Texture2D tex, Rectangle target, Color multiply)
        => GDraw.Stretched(tex, target, multiply);
    public static void BlitStretched(this Texture2D tex, Vector2 topLeft, Vector2 size, Color multiply)
        => GDraw.Stretched(tex, topLeft, size, multiply);


    public static IEnumerable<KeyValuePair<Point, Color>> IterPixels(this Texture2D tex)
    {
        Color[] arr = new Color[tex.Width * tex.Height];
        tex.GetData(arr);
        for (int x = 0; x < tex.Width; x++)
            for (int y = 0; y < tex.Height; y++)
                yield return new KeyValuePair<Point, Color>(new Point(x, y), arr[x + y * tex.Width]);
    }

    public static Color[,] GetTextureColorMatrix(this Texture2D tex)
    {
        Color[,] output = new Color[tex.Width, tex.Height];
        Color[] arr = new Color[tex.Width * tex.Height];
        tex.GetData(arr);
        for (int x = 0; x < tex.Width; x++)
            for (int y = 0; y < tex.Height; y++)
                output[x, y] = arr[x + y * tex.Width];
        return output;
    }
}

public static class FontExtensions
{

    public static string WrapText(this SpriteFont spriteFont, string text, long maxLineWidth)
    {
        string[] lines = text.Split('\n');
        StringBuilder sb = new StringBuilder();
        float spaceWidth = spriteFont.MeasureString(" ").X;
        foreach (string line in lines)
        {
            if (sb.Length > 0)
                sb.Append('\n');
            string[] words = line.Split(' ');
            float lineWidth = 0f;

            foreach (string word in words)
            {
                Vector2 size = spriteFont.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    if (lineWidth > 0) //don't insert new line if it's already a new line (word is wider than available space)
                        sb.Append("\n");
                    if (size.X > maxLineWidth)
                    {
                        // this is a naive attempt to wrap better if the space is very narrow
                        // assumes you aren't trying to fit a word in half the space
                        // game display code probably shouldn't fall into this path
                        SplitWrapSingleWord(spriteFont, word, maxLineWidth, out var a, out var b);
                        sb.Append(a + (b.Length > 0 ? "\n" + b : "") + " ");
                        if (b.Length > 0)
                            size = spriteFont.MeasureString(b);
                    }
                    else
                        sb.Append(word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }
        }

        return sb.ToString().TrimEnd(' ');
    }

    static void SplitWrapSingleWord(this SpriteFont spriteFont, string word, long maxLineWidth, out string a, out string b)
    {
        if (maxLineWidth <= 0 || word.Length < 4) { a = word; b = ""; return; }
        // CamelCaseLike
        //if (word.Skip(1).Any(c => char.IsUpper(c)) && word.Skip(1).Any(c => char.IsLower(c)))
        // maybe layer
        for (int i = word.Length - 2; i > 1; i--)
        {
            if ("aeiou()-_".Contains(char.ToLower(word[i])))
            {
                a = word.Substring(0, i) + "-";
                b = word.Substring(i);
                if (spriteFont.MeasureString(a).X < maxLineWidth) return;
            }
        }
        for (int i = word.Length - 3; i > 1; i--)
        {
            a = word.Substring(0, i) + "-";
            b = word.Substring(i + 1);
            if (spriteFont.MeasureString(a).X < maxLineWidth) return;
        }
        a = word;
        b = "";
        return; // shouldn't happen
    }
}