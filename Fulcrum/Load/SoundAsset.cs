using Microsoft.Xna.Framework.Audio;

namespace Fulcrum;

public class SoundAsset : IAsset
{
    SoundEffect _eff;
    public SoundAsset()
    {
        Location = eAssetLocation.Content;
    }
    public SoundAsset(string fileName) : base(fileName)
    {
        Location = eAssetLocation.Content;
    }
    public override eLoadState _Load(OLoad load)
    {
        _eff = load.con.Load<SoundEffect>(PathWithoutExtension);
        return eLoadState.Complete;
    }
    public override void _Unload(OLoad load)
    {
        load.con.UnloadAsset(PathWithoutExtension);
    }

    public SoundEffect Effect { get { return _eff; } }
    public static implicit operator SoundEffect(SoundAsset asset) { return asset.Effect; }
    public override bool SafeForParallel => true;
    public static SoundAsset StaticConstructor(string path) { return new SoundAsset(path); }
}