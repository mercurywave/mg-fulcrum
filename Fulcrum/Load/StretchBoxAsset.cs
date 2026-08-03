using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

//assumes sizes are symetrical around the center tile
public class StretchBoxAsset : IAsset
{
    Texture2D _tex;
    public int CenterTileHeight; // asset pixels
    public int CenterTileWidth;
    public int CornerWidth;
    public int CornerHeight;

    public enum eFillMethod { Stretch, Tile };
    // tile - cuts off any overlap, starting from the top left corner
    public eFillMethod FillMiddle = eFillMethod.Stretch;
    public eFillMethod FillHorizontal = eFillMethod.Stretch; // top+bottom
    public eFillMethod FillVertical = eFillMethod.Stretch;

    public StretchBoxAsset(string filePath, int centerW, int centerH) : base(filePath)
    {
        CenterTileHeight = centerH;
        CenterTileWidth = centerW;
    }

    public int ImgHeight { get { return _tex.Height; } }
    public int ImgWidth { get { return _tex.Width; } }

    public override eLoadState _Load(OLoad load)
    {
        _tex = load.con.Load<Texture2D>(PathWithoutExtension);
        CornerHeight = (_tex.Height - CenterTileHeight) / 2;
        CornerWidth = (_tex.Width - CenterTileWidth) / 2;
        return eLoadState.Complete;
    }

    //draw as a border padded around the outside of the rectangle specified
    // makes the math a bit simpler (maybe....)
    // introducing scale makes this an order of magnatude more complicated - render out to a texture. (you would get dumb looking tearing if you tried to hack it in)
    public void DrawAsBorder(int x, int y, int w, int h, Color? multiply = null)
    {
        Color clr = multiply ?? Color.White;

        if (FillHorizontal == eFillMethod.Stretch)
        {
            GDraw.Cropped(_tex, CornerWidth, 0, CenterTileWidth, CornerHeight, x, y - CornerHeight, w, CornerHeight, clr); // top
            GDraw.Cropped(_tex, CornerWidth, CornerHeight + CenterTileHeight, CenterTileWidth, CornerHeight, x, y + h, w, CornerHeight, clr); // bottom
        }
        else
        {
            int i = 0;
            for (i = 0; i < w; i += CenterTileWidth)
            {
                int cw = Math.Min(CenterTileWidth, w - i);
                GDraw.Cropped(_tex, CornerWidth, 0, cw, CornerHeight, x + i, y - CornerHeight, cw, CornerHeight, clr); // top
                GDraw.Cropped(_tex, CornerWidth, CornerHeight + CenterTileHeight, cw, CornerHeight, x + i, y + h, cw, CornerHeight, clr); // bottom
            }
        }

        if (FillVertical == eFillMethod.Stretch)
        {
            GDraw.Cropped(_tex, 0, CornerHeight, CornerWidth, CenterTileHeight, x - CornerWidth, y, CornerWidth, h, clr); // left
            GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight, CornerWidth, CenterTileHeight, x + w, y, CornerWidth, h, clr); // right
        }
        else
        {
            int j = 0;
            for (j = 0; j < h; j += CenterTileHeight)
            {
                int ch = Math.Min(CenterTileHeight, h - j);
                GDraw.Cropped(_tex, 0, CornerHeight, CornerWidth, ch, x - CornerWidth, y + j, CornerWidth, ch, clr); // left
                GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight, CornerWidth, ch, x + w, y + j, CornerWidth, ch, clr); // right
            }
        }

        if (FillMiddle == eFillMethod.Stretch)
            GDraw.Cropped(_tex, CornerWidth, CornerHeight, CenterTileWidth, CenterTileHeight, x, y, w, h, clr);
        else
        {
            int i = 0, j = 0;
            for (i = 0; i < w; i += CenterTileWidth)
            {
                for (j = 0; j < h; j += CenterTileHeight)
                {
                    int cw = Math.Min(CenterTileWidth, w - i);
                    int ch = Math.Min(CenterTileHeight, h - j);
                    GDraw.Cropped(_tex, CornerWidth, CornerHeight, cw, ch, x + i, y + j, cw, ch, clr);
                }
            }
        }

        GDraw.Cropped(_tex, 0, 0, CornerWidth, CornerHeight, x - CornerWidth, y - CornerHeight, CornerWidth, CornerHeight, clr); // top left
        GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, 0, CornerWidth, CornerHeight, x + w, y - CornerHeight, CornerWidth, CornerHeight, clr); // top right
        GDraw.Cropped(_tex, 0, CornerHeight + CenterTileHeight, CornerWidth, CornerHeight, x - CornerWidth, y + h, CornerWidth, CornerHeight, clr); // bot left
        GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight + CenterTileHeight, CornerWidth, CornerHeight, x + w, y + h, CornerWidth, CornerHeight, clr); // bot right
    }

    public Rectangle GetBorderPadded(int x, int y, int w, int h) => GetBorderPadded(new Rectangle(x, y, w, h));
    public Rectangle GetBorderPadded(Rectangle inner)
    {
        return new Rectangle(inner.X - CornerWidth, inner.Y - CornerHeight, inner.Width + CornerWidth * 2, inner.Height + CornerHeight * 2);
    }

    //pad x/y are additional pixels to add to the outside of the returned rectangle (or negate for margin)
    public Rectangle GetBorderPadded(int x, int y, int w, int h, int padx, int pady) => GetBorderPadded(new Rectangle(x, y, w, h), padx, pady);
    public Rectangle GetBorderPadded(Rectangle inner, int padx, int pady)
    {
        return new Rectangle(inner.X - CornerWidth - padx, inner.Y - CornerHeight - pady, inner.Width + CornerWidth * 2 + padx * 2, inner.Height + CornerHeight * 2 + pady * 2);
    }
    public Rectangle GetBorderPadded(int x, int y, int w, int h, int padx, int pady, float scale) => GetBorderPadded(new Rectangle(x, y, w, h), padx, pady, scale);
    public Rectangle GetBorderPadded(Rectangle inner, int padx, int pady, float scale)
    {
        var cw = (int)(CornerWidth * scale);
        var ch = (int)(CornerHeight * scale);
        return new Rectangle(inner.X - cw - padx, inner.Y - ch - pady, inner.Width + cw * 2 + padx * 2, inner.Height + ch * 2 + pady * 2);
    }

    //inset from the given rectangle as opposed to outside // TODO: I think something is fishy in the maths
    public void DrawAsBorderScaledInset(int x, int y, int w, int h, float scale, Color? multiply = null)
    {
        var rect = GetBorderPadded(x, y, w, h, -(int)(CornerWidth * scale * 2), -(int)(CornerHeight * scale * 2), scale);
        DrawAsBorderScaled(rect, scale, multiply);
    }

    //scale could support float, but would probably result in tearing
    public void DrawAsBorderScaled(Rectangle rect, float scale, Color? multiply = null) =>
        DrawAsBorderScaled(rect.X, rect.Y, rect.Width, rect.Height, scale, multiply);
    public void DrawAsBorderScaled(int x, int y, int w, int h, float scale, Color? multiply = null)
    {
        Color clr = multiply ?? Color.White;

        int cornerWScale = (int)(CornerWidth * scale);
        int cornerHScale = (int)(CornerHeight * scale);
        int centerWScale = (int)(CenterTileWidth * scale);
        int centerHScale = (int)(CenterTileHeight * scale);

        if (FillHorizontal == eFillMethod.Stretch)
        {
            GDraw.Cropped(_tex, CornerWidth, 0, CenterTileWidth, CornerHeight, x, y - cornerHScale, w, cornerHScale, clr); // top
            GDraw.Cropped(_tex, CornerWidth, CornerHeight + CenterTileHeight, CenterTileWidth, CornerHeight, x, y + h, w, cornerHScale, clr); // bottom
        }
        else
        {
            int i = 0;
            for (i = 0; i < w; i += CenterTileWidth)
            {
                int cw = Math.Min(CenterTileWidth, w - i);
                int si = (int)(i * scale);
                int scw = (int)(cw * scale);
                GDraw.Cropped(_tex, CornerWidth, 0, cw, CornerHeight, x + si, y - cornerHScale, scw, cornerHScale, clr); // top
                GDraw.Cropped(_tex, CornerWidth, CornerHeight + CenterTileHeight, cw, CornerHeight, x + si, y + h, scw, cornerHScale, clr); // bottom
            }
        }

        if (FillVertical == eFillMethod.Stretch)
        {
            GDraw.Cropped(_tex, 0, CornerHeight, CornerWidth, CenterTileHeight, x - cornerWScale, y, cornerWScale, h, clr); // left
            GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight, CornerWidth, CenterTileHeight, x + w, y, cornerWScale, h, clr); // right
        }
        else
        {
            int j = 0;
            for (j = 0; j < h; j += CenterTileHeight)
            {
                int ch = Math.Min(CenterTileHeight, h - j);
                int sj = (int)(j * scale);
                int sch = (int)(ch * scale);
                GDraw.Cropped(_tex, 0, CornerHeight, CornerWidth, ch, x - cornerWScale, y + sj, cornerWScale, sch, clr); // left
                GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight, CornerWidth, ch, x + w, y + sj, cornerWScale, sch, clr); // right
            }
        }

        if (FillMiddle == eFillMethod.Stretch)
            GDraw.Cropped(_tex, CornerWidth, CornerHeight, CenterTileWidth, CenterTileHeight, x, y, w, h, clr);
        else
        {
            int i = 0, j = 0;
            for (i = 0; i < w; i += CenterTileWidth)
            {
                for (j = 0; j < h; j += CenterTileHeight)
                {
                    int cw = Math.Min(CenterTileWidth, w - i);
                    int ch = Math.Min(CenterTileHeight, h - j);
                    int si = (int)(i * scale);
                    int scw = (int)(cw * scale);
                    int sj = (int)(j * scale);
                    int sch = (int)(ch * scale);
                    GDraw.Cropped(_tex, CornerWidth, CornerHeight, cw, ch, x + si, y + sj, scw, sch, clr);
                }
            }
        }

        GDraw.Cropped(_tex, 0, 0, CornerWidth, CornerHeight, x - cornerWScale, y - cornerHScale, cornerWScale, cornerHScale, clr); // top left
        GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, 0, CornerWidth, CornerHeight, x + w, y - cornerHScale, cornerWScale, cornerHScale, clr); // top right
        GDraw.Cropped(_tex, 0, CornerHeight + CenterTileHeight, CornerWidth, CornerHeight, x - cornerWScale, y + h, cornerWScale, cornerHScale, clr); // bot left
        GDraw.Cropped(_tex, CornerWidth + CenterTileWidth, CornerHeight + CenterTileHeight, CornerWidth, CornerHeight, x + w, y + h, cornerWScale, cornerHScale, clr); // bot right
    }
}