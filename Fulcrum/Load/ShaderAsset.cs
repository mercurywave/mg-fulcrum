using Microsoft.Xna.Framework.Graphics;

namespace Fulcrum;

public class ShaderAsset : IAsset
{
    Effect _effect;
    public ShaderAsset(string fileName) : base(fileName)
    {
    }

    public override eLoadState _Load(OLoad load)
    {
        _effect = load.con.Load<Effect>(Path);
        return eLoadState.Complete;
    }
    public override void _Unload(OLoad load)
    {
        load.con.UnloadAsset(Path);
    }

    public Effect Asset => _effect;
    public Effect BaseEffect => _effect;
    public static implicit operator Effect(ShaderAsset asset) { return asset?.Asset; }
}