using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Fulcrum;

public class FolderIdAssets<T> : FolderAssets<int, T> where T : IAsset
{
    public FolderIdAssets(string path, Func<string, T> constructor, string extension = "xnb") : base(path, constructor, s => int.Parse(s, System.Globalization.CultureInfo.InvariantCulture), extension) { }
}

// string keyed dictionary of files by name
// I feel like I'm coming full circle, but this is le handy...
public class FolderNamedAssets<T> : FolderAssets<string, T> where T : IAsset
{
    public FolderNamedAssets(string path, Func<string, T> constructor, string extension = "xnb") : base(path, constructor, s => s, extension) { }

    public bool Contains(string name) => _assets.ContainsKey(name);
    protected override string _CleanUpKey(string i)
        => i.ToLowerInvariant();
}

// just loads everything and keeps a simple list of assets
// This is specifically handy for like a set or random music where you want to manipulate the list
public class FolderAssetBag<T> : FolderAssets<string, T> where T : IAsset
{
    static Random _rand = new Random();
    public List<T> Bag;
    public FolderAssetBag(string path, Func<string, T> constructor, string extension = "xnb") : base(path, constructor, s => s, extension) { }

    public override eLoadState _Load(OLoad load)
    {
        var res = base._Load(load);
        Bag = Assets.ToList();
        return res;
    }

    // use the bag like a radio queue, and grab one you haven't used in a while
    public T GetRandomIsh()
    {
        var idx = _rand.Next(Bag.Count / 2);
        var pick = Bag[idx];
        Bag.RemoveAt(idx);
        Bag.Add(pick);
        return pick;
    }
    public void Shuffle() => GUtil.Shuffle(Bag);
}

public abstract class FolderAssets<K, T> : IAsset
    where T : IAsset
{
    protected Dictionary<K, T> _assets = new Dictionary<K, T>();
    Func<string, T> _constructor;
    Func<string, K> _translator;
    string _extension;
    public bool UsesContentDirectory = true;

    //I don't like that this needs a constructor delegate, but it is what it is
    public FolderAssets(string path, Func<string, T> constructor, Func<string, K> translator, string extension = "xnb") : base(path)
    {
        _constructor = constructor;
        _translator = translator;
        _extension = extension;
    }

    public override eLoadState _Load(OLoad load)
    {
        string fullpath;
        if (UsesContentDirectory)
            fullpath = load.con.RootDirectory + "/" + Path;
        else
            fullpath = Path;

        Debug.Assert(GFiles.GetDirectory(fullpath).Exists);
        foreach (var info in GFiles.FilesInFolderByExtension(fullpath, _extension))
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(info.Name);
            K id = _translator(name);
            T ass = _constructor(Path + "/" + id);
            _assets.Add(_CleanUpKey(id), ass);
        }
        load.AsyncLoadAssets(_assets.Values).ContinueWith(_ => CompleteLoad());
        return eLoadState.Started;
    }

    public T this[K i] { get { return _assets[_CleanUpKey(i)]; } }
    // this exists because I really don't want to deal with upper/lower case
    protected virtual K _CleanUpKey(K i) => i;
    public bool FileExists(K i) => _assets.ContainsKey(_CleanUpKey(i));
    public IEnumerable<T> Assets => _assets.Values;
    public IEnumerable<KeyValuePair<K, T>> Files => _assets;
    public int Count => _assets.Count;
}