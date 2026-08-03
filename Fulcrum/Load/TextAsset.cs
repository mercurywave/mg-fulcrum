
using System.IO;

namespace Fulcrum;

//splits lines into array of strings
public class TextAsset : IAsset
{
    public string[] Lines;
    public TextAsset(string path) : base(path)
    {
        Location = eAssetLocation.Data;
    }

    public override eLoadState _Load(OLoad load)
    {
        Lines = File.ReadAllLines(Path);
        return eLoadState.Complete;
    }
    public override void _Unload(OLoad load)
    {
        Lines = null;
    }
    public override bool SafeForParallel => true;
    public override bool CanHotLoad => true;
}