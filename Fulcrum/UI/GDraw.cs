using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public static class GDraw
{
    public static SpriteBatch sb = null; // set by screen init
    public static SamplerState DefaultSamplerState = null;
    static RasterizerState _scissorRasterizer = new RasterizerState() { ScissorTestEnable = true };
    internal static Texture2D _pixel = null; // set by screen init
    internal static float _safeLayerSubstep = -.000001f;

    #region Spritebatch
    public static ODisposable BatchBlock()
            => new ODisposable(Begin, End);
    public static ODisposable PauseBatchBlock()
        => new ODisposable(End, Begin);

    public static ODisposable BatchBlock(Matrix transform)
        => new ODisposable(() => Begin(transform), End);

    public static ODisposable BatchBlock(Matrix transform, SpriteSortMode sort)
        => new ODisposable(() => Begin(transform, sort), End);

    public static ODisposable BatchBlock(Matrix transform, SpriteSortMode sort, BlendState blendState)
        => new ODisposable(() => Begin(transform, sort, blendState), End);

    public static ODisposable BatchBlock(Matrix transform, SpriteSortMode sort, BlendState blendState, Effect effect)
        => new ODisposable(() => Begin(transform, sort, blendState, effect), End);
    public static ODisposable BatchBlock(Matrix transform, SpriteSortMode sort, BlendState blendState, Effect effect, SamplerState sampler)
        => new ODisposable(() => Begin(transform, sort, blendState, effect, sampler), End);
    public static ODisposable BatchBlock(SamplerState sampler)
        => new ODisposable(() => Begin(Matrix.Identity, SpriteSortMode.Deferred, BlendState.AlphaBlend, null, sampler), End);


    public static ODisposable RenderTargetBlock(RenderTarget2D tex)
        => new ODisposable(() => GScreen.device.SetRenderTarget(tex), () => GScreen.device.SetRenderTarget(null));


    static void Begin()
    {
        Begin(Matrix.Identity, SpriteSortMode.Deferred);
    }

    static void Begin(Matrix transform)
    {
        Begin(transform, SpriteSortMode.BackToFront);
    }

    static void Begin(Matrix transform, SpriteSortMode sort)
    {
        using (GPerf.UseBlock("GScreen:Begin"))
            sb.Begin(sort, BlendState.AlphaBlend, DefaultSamplerState, null, null, null, transform);
    }

    static void Begin(Matrix transform, SpriteSortMode sort, BlendState blendState)
    {
        using (GPerf.UseBlock("GScreen:Begin"))
            sb.Begin(sort, blendState, DefaultSamplerState, null, null, null, transform);
    }

    internal static void Begin(Matrix transform, SpriteSortMode sort, BlendState blendState, Effect effect)
    {
        using (GPerf.UseBlock("GScreen:Begin"))
            sb.Begin(sort, blendState, DefaultSamplerState, null, null, effect, transform);
    }

    internal static void Begin(Matrix transform, SpriteSortMode sort, BlendState blendState, Effect effect, SamplerState sample)
    {
        using (GPerf.UseBlock("GScreen:Begin"))
            sb.Begin(sort, blendState, sample, null, null, effect, transform);
    }
    internal static void End()
    {
        using (GPerf.UseBlock("GScreen:EndDraw"))
            sb.End();
    }

    public static ODisposable ClipBlock(Rectangle clipRect)
    {
        var origRect = sb.GraphicsDevice.ScissorRectangle;
        return new ODisposable(
            () => BeginClip(clipRect),
            () => EndClip(origRect));
    }
    static void BeginClip(Rectangle clipRect)
    {
        using (GPerf.UseBlock("GScreen:BeginClip"))
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, _scissorRasterizer);
        sb.GraphicsDevice.ScissorRectangle = clipRect;
    }
    internal static void EndClip(Rectangle restoreRect)
    {
        using (GPerf.UseBlock("GScreen:EndClip"))
            sb.End();
        sb.GraphicsDevice.ScissorRectangle = restoreRect;
    }
    #endregion


    public static void Clear(Color color) { GScreen.device.Clear(color); }
    public static void Overlay(Color color) { Clear(color); }
    public static void Clear() { Clear(Color.Black); }


    // Scaled from top left
    public static void Scaled(Texture2D tex, int x, int y, float scale)
    {
        sb.Draw(tex, new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public static void Scaled(Texture2D tex, int x, int y, Vector2 scale)
    {
        sb.Draw(tex, new Vector2(x, y), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public static void Scaled(Texture2D tex, int x, int y, Vector2 scale, Color color)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public static void Scaled(Texture2D tex, Vector2 pos, Vector2 scale, Color color)
    {
        sb.Draw(tex, pos, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }


    // simplest blits
    public static void Blit(Texture2D tex, int x, int y)
    {
        Blit(tex, x, y, Color.White);
    }

    public static void Blit(Texture2D tex, Vector2 vec, Color color)
    {
        sb.Draw(tex, vec, color);
    }
    public static void Blit(Texture2D tex, int x, int y, Color color)
    {
        sb.Draw(tex, new Vector2(x, y), color);
    }


    // center on x/y
    public static void Centered(Texture2D tex, int x, int y)
    {
        Centered(tex, x, y, Color.White);
    }
    public static void Centered(Texture2D tex, int x, int y, Color color)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), 1f, SpriteEffects.None, 0f);
    }
    public static void Centered(Texture2D tex, int x, int y, float scale, Color color)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, SpriteEffects.None, 0f);
    }
    public static void Centered(Texture2D tex, int x, int y, float scale, Color color, SpriteEffects eff)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, eff, 0f);
    }
    public static void Centered(Texture2D tex, int x, int y, Vector2 scale)
    {
        sb.Draw(tex, new Vector2(x, y), null, Color.White, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, SpriteEffects.None, 0f);
    }
    public static void Centered(Texture2D tex, int x, int y, Vector2 scale, Color color)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, SpriteEffects.None, 0f);
    }
    public static void Centered(Texture2D tex, int x, int y, Vector2 scale, Color color, SpriteEffects eff)
    {
        sb.Draw(tex, new Vector2(x, y), null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, eff, 0f);
    }
    public static void Centered(Texture2D tex, Vector2 at)
        => Centered(tex, at, Color.White);
    public static void Centered(Texture2D tex, Vector2 at, Color color)
        => Centered(tex, at, new Vector2(1f), color);
    public static void Centered(Texture2D tex, Vector2 at, float scale, Color color)
        => Centered(tex, at, new Vector2(scale), color);
    public static void Centered(Texture2D tex, Vector2 at, Vector2 scale, Color color)
        => Centered(tex, at, scale, color, SpriteEffects.None);
    public static void Centered(Texture2D tex, Vector2 at, float scale, Color color, SpriteEffects eff)
        => Centered(tex, at, new Vector2(scale), color, eff);
    public static void Centered(Texture2D tex, Vector2 at, Vector2 scale, Color color, SpriteEffects eff)
    {
        sb.Draw(tex, at, null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, eff, 0f);
    }

    public static void LayerCentered(Texture2D tex, Vector2 at, Vector2 scale, Color color, SpriteEffects eff, float layer)
    {
        sb.Draw(tex, at, null, color, 0, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, eff, layer);
    }

    // Crop input
    public static void Cropped(Texture2D tex, int cropX, int cropY, int cropW, int cropH, int x, int y)
    {
        sb.Draw(tex, new Rectangle(x, y, cropW, cropH), new Rectangle(cropX, cropY, cropW, cropH), Color.White);
    }
    public static void Cropped(Texture2D tex, int cropX, int cropY, int cropW, int cropH, int x, int y, int w, int h)
    {
        sb.Draw(tex, new Rectangle(x, y, w, h), new Rectangle(cropX, cropY, cropW, cropH), Color.White);
    }
    public static void Cropped(Texture2D tex, Rectangle imgCrop, Rectangle screenStretch) => Cropped(tex, imgCrop, screenStretch, Color.White);
    public static void Cropped(Texture2D tex, Rectangle imgCrop, Rectangle screenStretch, Color color)
    {
        sb.Draw(tex, screenStretch, imgCrop, color);
    }
    public static void Cropped(Texture2D tex, int cropX, int cropY, int cropW, int cropH, int x, int y, int w, int h, Color color)
        => Cropped(tex, cropX, cropY, cropW, cropH, x, y, w, h, SpriteEffects.None, color);
    public static void Cropped(Texture2D tex, int cropX, int cropY, int cropW, int cropH, int x, int y, int w, int h, SpriteEffects effects, Color color)
    {
        sb.Draw(tex, new Rectangle(x, y, w, h), new Rectangle(cropX, cropY, cropW, cropH), color, 0f, Vector2.Zero, effects, 0f);
    }
    public static void Cropped(Texture2D tex, int cropX, int cropY, int cropW, int cropH, int x, int y, float scale, Color color)
         => Cropped(tex, new Rectangle(cropX, cropY, cropW, cropH), x, y, scale, color);
    public static void Cropped(Texture2D tex, Rectangle sourceRect, float x, float y, float scale, Color color)
        => Cropped(tex, sourceRect, new Vector2(x, y), scale, color);

    public static void Cropped(Texture2D tex, Rectangle sourceRect, Vector2 pos, float scale, Color color)
    {
        sb.Draw(tex, pos, sourceRect, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public static void LayerCropped(Texture2D tex, Rectangle sourceRect, Vector2 pos, float scale, Color color, float layer, SpriteEffects eff)
    {
        sb.Draw(tex, pos, sourceRect, color, 0f, Vector2.Zero, scale, eff, layer);
    }


    public static void CroppedRotated(Texture2D tex, Rectangle sourceRect, Vector2 pos, float rotation, float scale, Color color)
    {
        sb.Draw(tex, pos, sourceRect, color, rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    public static void CroppedRotated(Texture2D tex, Rectangle sourceRect, Vector2 pos, Vector2 origin, float rotation, float scale, Color color)
    {
        sb.Draw(tex, pos, sourceRect, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public static void Stretched(Texture2D tex, Rectangle rect)
    {
        Stretched(tex, rect, Color.White);
    }
    public static void Stretched(Texture2D tex, int x, int y, int w, int h, Color color)
    {
        sb.Draw(tex, new Rectangle(x, y, w, h), color);
    }
    public static void Stretched(Texture2D tex, Rectangle rect, Color color)
    {
        sb.Draw(tex, rect, color);
    }
    public static void Stretched(Texture2D tex, Rectangle rect, float layer, Color color)
    {
        sb.Draw(tex, rect, null, color, 0f, Vector2.Zero, SpriteEffects.None, layer);
    }
    public static void Stretched(Texture2D tex, Vector2 pos, Vector2 size, Color color)
        => Stretched(tex, pos, size, 0, color);
    public static void Stretched(Texture2D tex, Vector2 pos, Vector2 size, float layer, Color color)
    {
        var scale = new Vector2(GMath.FindScale(tex.Width, size.X), GMath.FindScale(tex.Height, size.Y));
        sb.Draw(tex, pos, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layer);
    }

    public static void Vector(Texture2D tex, Vector2 topLeft, Color color)
    {
        Vector(tex, topLeft, 1f, 0, color);
    }
    public static void Vector(Texture2D tex, Vector2 topLeft, float layer, Color color)
    {
        Vector(tex, topLeft, 1f, layer, color);
    }
    public static void Vector(Texture2D tex, Vector2 topLeft, float scale, float layer, Color color)
    {
        Vector(tex, topLeft, scale, layer, color, SpriteEffects.None);
    }
    public static void Vector(Texture2D tex, Vector2 topLeft, float scale, float layer, Color color, SpriteEffects eff)
    {
        sb.Draw(tex, topLeft, null, color, 0f, Vector2.Zero, new Vector2(scale, scale), eff, layer);
    }

    public static void CenterRotated(Texture2D tex, int x, int y, int w, int h, float rotation)
    {
        CenterRotated(tex, x, y, w, h, rotation, Color.White);
    }
    public static void CenterRotated(Texture2D tex, int x, int y, int w, int h, float rotation, Color color, float layer = 0f)
    {
        sb.Draw(tex, new Rectangle(x, y, w, h), null, color, rotation, new Vector2(tex.Width / 2f, tex.Height / 2f), SpriteEffects.None, layer);
    }
    public static void CenterRotated(Texture2D tex, int x, int y, float rotation, Color color, float scale = 1, float layer = 0f)
    {
        sb.Draw(tex, new Rectangle(x, y, (int)(tex.Width * scale), (int)(tex.Height * scale)), null, color, rotation, new Vector2(tex.Width / 2f, tex.Height / 2f), SpriteEffects.None, layer);
    }
    public static void CenterRotated(Texture2D tex, Vector2 pos, float rotation)
    {
        CenterRotated(tex, pos, rotation, Color.White);
    }
    public static void CenterRotated(Texture2D tex, Vector2 pos, float rotation, Color color, float scale = 1, float layer = 0f)
    {
        sb.Draw(tex, pos, null, color, rotation, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, SpriteEffects.None, layer);
    }
    public static void CenterRotated(Texture2D tex, Vector2 pos, float rotation, Color color, Vector2 scale, float layer = 0f)
    {
        sb.Draw(tex, pos, null, color, rotation, new Vector2(tex.Width / 2f, tex.Height / 2f), scale, SpriteEffects.None, layer);
    }


    #region Shapes
    public static void Rect(int x, int y, int w, int h, Color color)
    {
        Stretched(_pixel, x, y, w, h, color);
    }
    //allows for rectangles with subpixel drawing
    public static void Rect(Vector2 pos, int w, int h, Color color, float layer)
    {
        sb.Draw(_pixel, pos, null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, layer);
    }
    public static void Rect(Vector2 pos, Vector2 size, Color color, float layer = 0)
    {
        sb.Draw(_pixel, pos, null, color, 0f, Vector2.Zero, size, SpriteEffects.None, layer);
    }

    public static void Rect(int x, int y, int w, int h, Color color, Color border)
    {
        int bw = (int)GScreen.Scale;
        if (bw < 1) bw = 1;
        Rect(x, y, w, h, color, border, bw);
    }
    public static void Rect(int x, int y, int w, int h, Color color, Color border, int borderWidth)
    {
        if (color.A > 0)
        {
            Stretched(_pixel, x - borderWidth, y - borderWidth, w + 2 * borderWidth, h + 2 * borderWidth, border);
            Stretched(_pixel, x, y, w, h, color);
        }
        else
            Border(x - borderWidth, y - borderWidth, w + borderWidth * 2, h + borderWidth * 2, border, borderWidth);
    }
    //define the outer edge, this will draw inward
    public static void Border(int x, int y, int w, int h, Color border, int thickness)
    {
        Stretched(_pixel, x, y, w, thickness, border);
        Stretched(_pixel, x, y + thickness, thickness, h - thickness * 2, border);
        Stretched(_pixel, x + w - thickness, y + thickness, thickness, h - thickness * 2, border);
        Stretched(_pixel, x, y + h - thickness, w, thickness, border);
    }

    public static void HorizBar(int x, int y, int w, int h, float percent, Color fill, Color background, Color border)
    {
        int off = (int)(percent * w);
        if (background == Color.Transparent)
            Border(x - 1, y - 1, w + 2, h + 2, border, 1);
        else
            Stretched(_pixel, x - 1, y - 1, w + 2, h + 2, border);
        Stretched(_pixel, x, y, w, h, background);
        Stretched(_pixel, x, y, off, h, fill);
    }

    //border extends outside W/H, pad subtracts inside
    public static void HorizBar(int x, int y, int w, int h, float percent, Color fill, Color background, Color border, int borderW, int pad)
    {
        Border(x - borderW, y - borderW, w + 2 * borderW, h + 2 * borderW, border, borderW);
        if (background != Color.Transparent) Stretched(_pixel, x, y, w, h, background);
        if (!float.IsNaN(percent))
        {
            int off = (int)(percent * w);
            Stretched(_pixel, x + pad, y + pad, off - pad * 2, h - pad * 2, fill);
        }
    }

    // just draws the inner part of a horizontal bar
    public static void HorizBarPart(int x, int y, int w, int h, float fromPercent, float toPercent, Color fill, int pad)
    {
        int off = (int)(fromPercent * w);
        int off2 = (int)(toPercent * w);
        Stretched(_pixel, x + off, y, off2 - off, h, fill);
    }
    public static void Line(Vector2 start, Vector2 end, Color color, float thickness = 1, float layer = 0f)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);

        sb.Draw(_pixel, start, null,
            color, angle, new Vector2(0, .5f), new Vector2(edge.Length(), thickness), SpriteEffects.None, layer);
    }

    #endregion


    #region Text

    public static void Text(SpriteFont font, string str, int x, int y, Color color)
    {
        sb.DrawString(font, str, new Vector2(x, y), color);
    }

    public static void Text(SpriteFont font, string str, int x, int y, Color color, float scale)
    {
        sb.DrawString(font, str, new Vector2(x, y), color, 0f, new Vector2(), scale, SpriteEffects.None, 0f);
    }


    public static void TextRightTopAlign(SpriteFont font, string str, int right, int y, Color color)
    {
        var size = font.MeasureString(str);
        sb.DrawString(font, str, new Vector2(right - size.X, y), color);
    }
    public static void TextCentered(SpriteFont font, string str, int x, int y, Color color)
    {
        var size = font.MeasureString(str);
        sb.DrawString(font, str, new Vector2(x - size.X / 2, y - size.Y / 2), color);
    }
    public static void TextCenterRotated(SpriteFont font, string str, int x, int y, float rotation, Color color)
        => TextCenterRotated(font, str, x, y, rotation, 1f, color);
    public static void TextCenterRotated(SpriteFont font, string str, int x, int y, float rotation, float scale, Color color)
        => TextCenterRotated(font, str, new Vector2(x, y), rotation, scale, color);
    public static void TextCenterRotated(SpriteFont font, string str, Vector2 drawAt, float rotation, float scale, Color color)
    {
        var size = font.MeasureString(str);
        var mid = new Vector2(size.X / 2, size.Y / 2);
        sb.DrawString(font, str, drawAt, color, rotation, mid, scale, SpriteEffects.None, 0);
    }

    #endregion
}