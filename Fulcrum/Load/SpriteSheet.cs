
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public class SpriteSheet : IAsset
{
    // represents a horizontal strip of sprites (I guess I could support the strip wrapping...?)
    internal ImageAsset Texture;
    int _frameWidth;
    int _frameHeight;
    public int Frames => CalcFrames(); // requires texture to be loaded
    public int PixelPad = 0; // assumes not buffer to the left Use to avoid slight float errors in sampling

    public SpriteSheet(string path, int frameWidth, int frameHeight) : base(path)
    {
        _frameHeight = frameHeight;
        _frameWidth = frameWidth;
        Texture = new ImageAsset(path);
    }
    public override eLoadState _Load(OLoad load)
    {
        if(Texture.LoadState == eLoadState.Complete)
            return eLoadState.Complete;
        load.AsyncLoadAsset(Texture).ContinueWith(_ => CompleteLoad());
        return eLoadState.Waiting;
    }

    int CalcFrames()
    {
        int w = (Texture.Width / _frameWidth);
        int h = (Texture.Height / _frameHeight);
        if (w == 0) w = 1;
        if (h == 0) h = 1;
        return h * w;
    }

    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;


    Rectangle GetCrop(int idx)
    {
        int perRow = (Texture.Width + PixelPad) / (FrameWidth + PixelPad);
        int x = idx % perRow;
        int y = (idx % Frames) / perRow; // I shouldn't need the mod here, but it will make testing easier
        return new Rectangle(x * _frameWidth + PixelPad * x, y * _frameHeight + PixelPad * y, _frameWidth, _frameHeight);
    }

    public int GetFrameIdx(int x, int y)
    {
        int perRow = (Texture.Width + PixelPad) / (FrameWidth + PixelPad);
        return y * perRow + x;
    }

    //returns a frame index that corresponds to iterating through the list and coming back
    // mods the input by the number of frames
    public int SwayFrameIdx(int i)
    {
        i %= SwayFramesCount;
        if (i >= Frames) return Frames - (i - Frames) - 2;
        return i;
    }
    public int SwayFramesCount => Frames + Frames - 2;

    public void Draw(int frame, int x, int y)
        => GDraw.Cropped(Texture, GetCrop(frame), new Rectangle(x, y, FrameWidth, FrameHeight));

    public void Draw(int frame, int x, int y, Color clr)
        => GDraw.Cropped(Texture, GetCrop(frame), new Rectangle(x, y, FrameWidth, FrameHeight), clr);

    public void Draw(int frame, int x, int y, float scale)
        => GDraw.Cropped(Texture, GetCrop(frame), new Rectangle(x, y, (int)(FrameWidth * scale), (int)(FrameHeight * scale)), Color.White);

    public void Draw(int frame, int x, int y, float scale, Color clr)
        => GDraw.Cropped(Texture, GetCrop(frame), new Rectangle(x, y, (int)(FrameWidth * scale), (int)(FrameHeight * scale)), clr);

    public void Draw(int frame, Vector2 pos, float scale, Color clr)
        => GDraw.Cropped(Texture, GetCrop(frame), pos, scale, clr);

    public void Draw(int frame, Point pos, float scale, Color clr)
        => GDraw.Cropped(Texture, GetCrop(frame), pos.ToVector2(), scale, clr);

    public void DrawCentered(int frame, int x, int y, float scale = 1, bool flip = false)
        => DrawCentered(frame, x, y, scale, flip, Color.White);

    public void DrawCentered(int frame, Point pt, float scale = 1, bool flip = false)
        => DrawCentered(frame, pt.X, pt.Y, scale, flip, Color.White);

    public void DrawCentered(int frame, Point pt, Color clr, float scale = 1, bool flip = false)
        => DrawCentered(frame, pt.X, pt.Y, scale, flip, clr);

    public void DrawCentered(int frame, int x, int y, float scale, bool flip, Color clr)
    {
        var crop = GetCrop(frame);
        if (flip)
            GDraw.Cropped(Texture, crop.X, crop.Y, crop.Width, crop.Height, x - (int)(FrameWidth * scale / 2), y - (int)(FrameHeight * scale / 2), (int)(FrameWidth * scale), (int)(FrameHeight * scale), SpriteEffects.FlipHorizontally, clr);
        else
            GDraw.Cropped(Texture, crop, new Rectangle(x - (int)(FrameWidth * scale / 2), y - (int)(FrameHeight * scale / 2), (int)(FrameWidth * scale), (int)(FrameHeight * scale)), clr);
    }

    public void DrawCenterRotated(int frame, Vector2 pos, float rotation, float scale, Color clr)
    {
        var crop = GetCrop(frame);
        GDraw.CroppedRotated(Texture, crop, pos, new Vector2(FrameWidth, FrameHeight) / 2f, rotation, scale, clr);
    }

    public void DrawLayered(int frame, Point pt, float layer, float scale = 1, bool flip = false)
        => DrawLayered(frame, pt.ToVector2(), layer, Color.White, scale, flip);
    public void DrawLayered(int frame, Point pt, float layer, Color clr, float scale = 1, bool flip = false)
        => DrawLayered(frame, pt.ToVector2(), layer, clr, scale, flip);
    public void DrawLayered(int frame, int x, int y, float layer, float scale = 1, bool flip = false)
        => DrawLayered(frame, x, y, layer, Color.White, scale, flip);
    public void DrawLayered(int frame, int x, int y, float layer, Color clr, float scale = 1, bool flip = false)
        => DrawLayered(frame, new Vector2(x, y), layer, clr, scale, flip);
    public void DrawLayered(int frame, Vector2 pos, float layer, Color clr, float scale = 1, bool flip = false)
    {
        GDraw.LayerCropped(Texture, GetCrop(frame), pos, scale, clr, layer, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
    }
    public void DrawOffsetCenterRotated(int frame, Vector2 pos, float rotation, float scale = 1f)
        => DrawOffsetCenterRotated(frame, pos, Color.White, rotation, scale);
    public void DrawOffsetCenterRotated(int frame, Vector2 pos, Color clr, float rotation, float scale = 1f)
        => DrawOffsetRotated(frame, pos, new Vector2(FrameWidth, FrameHeight) / 2, clr, rotation, scale);
    public void DrawOffsetRotated(int frame, Vector2 pos, Vector2 origin, float rotation, float scale = 1f)
        => DrawOffsetRotated(frame, pos, origin, Color.White, rotation, scale);
    public void DrawOffsetRotated(int frame, Vector2 pos, Vector2 origin, Color clr, float rotation, float scale = 1f)
    {
        GDraw.CroppedRotated(Texture, GetCrop(frame), pos, origin, rotation, scale, clr);
    }

    public override void _Unload(OLoad load)
    {
        load.Unload(Texture);
    }
}