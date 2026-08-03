
using Microsoft.Xna.Framework.Media;

namespace Fulcrum;

public class MusicAsset : IAsset
{
    Song _eff;
    public MusicAsset()
    {
        Location = eAssetLocation.Content;
    }
    public MusicAsset(string fileName) : base(fileName)
    {
        Location = eAssetLocation.Content;
    }
    public override eLoadState _Load(OLoad load)
    {
        _eff = load.con.Load<Song>(PathWithoutExtension);
        return eLoadState.Complete;
    }
    public override void _Unload(OLoad load)
    {
        load.con.UnloadAsset(PathWithoutExtension);
    }

    public static MusicAsset StaticConstructor(string file) => new MusicAsset(file);
    public Song Song { get { return _eff; } }
    public static implicit operator Song(MusicAsset asset) { return asset.Song; }
    public override bool SafeForParallel => true;
}