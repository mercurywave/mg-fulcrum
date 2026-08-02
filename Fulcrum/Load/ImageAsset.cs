
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public class ImageAsset : IAsset
{
    protected Texture2D _tex;
    public ImageAsset() { }
    public ImageAsset(string fileName) : base(fileName)
    {
    }

    public int Height { get { return _tex.Height; } }
    public int Width { get { return _tex.Width; } }

    public static implicit operator Texture2D(ImageAsset asset) { return asset._tex; }

    override public eLoadState _Load(OLoad load)
    {
        if (Location == eAssetLocation.Content)
            _tex = load.con.Load<Texture2D>(PathWithoutExtension);
        else
        {
            FileStream fileStream = new FileStream(Path, FileMode.Open);
            var raw = Texture2D.FromStream(GScreen.Device, fileStream);
            fileStream.Dispose();

            // asset is loaded as non-pre-multiplied by default, which leads to weird semi-transparent edges
            Color[] buffer = new Color[raw.Width * raw.Height];
            raw.GetData(buffer);
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = Color.FromNonPremultiplied(buffer[i].R, buffer[i].G, buffer[i].B, buffer[i].A);
            raw.SetData(buffer);
            _tex = raw;
        }
        return eLoadState.Complete;
    }
    public override bool _Unload(OLoad load)
    {
        if(Location == eAssetLocation.Content)
            load.con.UnloadAsset(PathWithoutExtension);
        else
            _tex.Dispose();
        _tex = null;
        return base._Unload(load);
    }

    public override bool CanHotLoad => Location != eAssetLocation.Content;
    public override void HotLoad(OLoad load)
    {
        if(!CanHotLoad) return;
        Unload(load);
        BeginLoad(load);
    }

    
    public void Blit(int x, int y) 
        => GDraw.Blit(_tex, new Vector2(x, y), Color.White);
    public void Blit(int x, int y, Color multiply) 
        => GDraw.Blit(_tex, new Vector2(x, y), multiply);
    public void Blit(Point topLeft)
        => GDraw.Blit(_tex, topLeft.ToVector2(), Color.White);
    public void Blit(Point topLeft, Color multiply)
        => GDraw.Blit(_tex, topLeft.ToVector2(), multiply);

    public void BlitStretched(Rectangle target)
        => GDraw.Stretched(_tex, target, Color.White);
    public void BlitStretched(Vector2 topLeft, Vector2 size)
        => GDraw.Stretched(_tex, topLeft, size, Color.White);

    public void BlitStretched(Rectangle target, Color multiply)
        => GDraw.Stretched(_tex, target, multiply);
    public void BlitStretched(Vector2 topLeft, Vector2 size, Color multiply)
        => GDraw.Stretched(_tex, topLeft, size, multiply);
}