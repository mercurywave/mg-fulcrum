using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public class ScaledFont : IAsset
{
    LookbackList<float, FontAsset> _scaled = new LookbackList<float, FontAsset>(null);

    // provide a folder with compiled spritefonts named like "1.5x"
    // where 1x is the default scale for 1x scale screen
    public ScaledFont(string folderName) : base(folderName)
    {
        Location = eAssetLocation.Content;
    }

    // if you have a fixed size and want to use a tool that expects a scaled font, use this instead
    public ScaledFont(FontAsset font)
    {
        Location = eAssetLocation.Content;
        _scaled = new LookbackList<float, FontAsset>(font);
    }

    public override eLoadState _Load(OLoad load)
    {
        foreach (var packed in GFiles.ScaledFiles(load.con.RootDirectory + "/" + Path, "xnb"))
        {
            FontAsset ass = new FontAsset(Path + "/" + packed.Item3);
            _scaled.Add(packed.Item1, ass);
            if (packed.Item1 == 1) _scaled.Default = ass;
        }
        if (_scaled.Default == null) GError.RaiseError(new Exception("could not find default font for " + Path));

        load.AsyncLoadAssets(_scaled.Values).ContinueWith(_ => CompleteLoad());
        return eLoadState.Started;
    }

    public static implicit operator FontAsset(ScaledFont asset) { return asset.GetForScreen(); }
    public static implicit operator SpriteFont(ScaledFont asset) { return asset.GetForScreen(); }

    public FontAsset GetForScale(float scale)
    {
        return _scaled.Lookback(scale);
    }
    public FontAsset GetForScreen()
    {
        return _scaled.Lookback(GScreen.Scale);
    }

    public Vector2 Measure(string str) { return GetForScreen().Measure(str); }
    public Vector2 Measure(string str, float scale)
    {
        return GetForScale(scale).Measure(str);
    }
    public float MeasureHeight(string str = "|") { return GetForScreen().Measure(str).Y; }

    //maxLineWidth in pixels
    public string Elipsize(string text, int maxLineWidth)
    {
        string prev = "...";
        for (int i = 0; i < text.Length; i++)
        {
            string next = text.Substring(0, i) + "...";
            if (Measure(next).X > maxLineWidth)
                return prev;
            prev = next;
        }
        return text;
    }

    public string WrapText(string str, int width) { return GetForScreen().WrapText(str, width); }
    public string WrapText(string str, int width, float scale)
    {
        return GetForScale(scale).WrapText(str, width);
    }

    public string SafeString(string str)
    {
        Regex rx = new Regex(@"[^\u0020-\u007E]+");
        str = str.Replace("\t", "  ");
        return rx.Replace(str, "?");
    }
}

// instance of a font that can be scaled dynamically relative to screen DPI
public class FontScaler
{
    ScaledFont _font;
    float _scale;

    public FontScaler(ScaledFont font, float scale = 1f)
    {
        _font = font;
        _scale = scale;
    }

    public float Scale
    {
        get { return _scale; }
        set
        {
            if (_scale == value) return;
            _scale = value;
        }
    }

    public FontAsset GetScaled() { return _font.GetForScale(GScreen.Scale * _scale); }

    public static implicit operator FontAsset(FontScaler font) { return font.GetScaled(); }
    public static implicit operator SpriteFont(FontScaler font) { return font.GetScaled(); }

}

public class FontAsset : IAsset
{
    SpriteFont _font;

    public FontAsset(string fileName) : base(fileName)
    {
        Location = eAssetLocation.Content;
    }

    public override eLoadState _Load(OLoad load)
    {
        _font = load.con.Load<SpriteFont>(Path);

        return eLoadState.Complete;
    }

    public static implicit operator SpriteFont(FontAsset asset) { return asset._font; }

    public Vector2 Measure(string str)
    {
        return _font.MeasureString(str);
    }

    public int LineHeight { get { return _font.LineSpacing; } }

    public string WrapText(string str, int width)
    {
        return _font.WrapText(str, width);
    }

    // this is a bit naive, but good in the general case
    public string SafeString(string str)
    {
        Regex rx = new Regex(@"[^\u0020-\u007E]+");
        str = str.Replace("\t", "  ");
        return rx.Replace(str, "?");
    }
    //public override bool SafeForParallel => true;
}