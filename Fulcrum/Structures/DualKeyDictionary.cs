using System.Collections.Generic;

namespace Fulcrum;

public class DualKeyDictionary<T, U, Value>
{
    Dictionary<T, Dictionary<U, Value>> _dic = new Dictionary<T, Dictionary<U, Value>>();

    public void Set(T a, U b, Value val)
    {
        if (!_dic.ContainsKey(a))
            _dic.Add(a, new Dictionary<U, Value>());
        if (_dic[a].ContainsKey(b))
            _dic[a][b] = val;
        else
            _dic[a].Add(b, val);
    }

    public Value Get(T a, U b)
    {
        return _dic[a][b];
    }

    public bool HasKeys(T a, U b)
    {
        if (!_dic.ContainsKey(a)) return false;
        if (_dic[a].ContainsKey(b)) return true;
        return false;
    }

    public IEnumerable<T> PrimaryKeys()
    {
        foreach (T key in _dic.Keys)
            yield return key;
    }
    public IEnumerable<U> SecondaryKeys(T primary)
    {
        foreach (U key in _dic[primary].Keys)
            yield return key;
    }
    public IEnumerable<Value> Values(T primary)
    {
        foreach (U second in _dic[primary].Keys)
            yield return Get(primary, second);
    }
    public IEnumerable<Value> AllValues()
    {
        foreach (T a in PrimaryKeys())
            foreach (U b in SecondaryKeys(a))
                yield return Get(a, b);
    }
    public bool IsEmpty => _dic.Count > 0;

    public Dictionary<U, Value> SubTree(T primary)
    {
        if (!_dic.ContainsKey(primary))
            return new Dictionary<U, Value>();
        return new Dictionary<U, Value>(_dic[primary]);
    }
    public void ReplaceSubTree(T primary, Dictionary<U, Value> dic)
    {
        Kill(primary);
        MergeSubTree(primary, dic);
    }
    public void MergeSubTree(T primary, Dictionary<U, Value> dic)
    {
        if (!_dic.ContainsKey(primary))
            _dic.Add(primary, new Dictionary<U, Value>());
        var sub = _dic[primary];
        foreach (var pair in dic)
            sub.Add(pair.Key, pair.Value);
    }
    public void Kill(T primary)
    {
        if (_dic.ContainsKey(primary))
            _dic.Remove(primary);
    }
    public void Kill(T primary, U secondary)
    {
        if (!_dic.ContainsKey(primary)) return;
        var sub = _dic[primary];
        if (sub.ContainsKey(secondary))
            sub.Remove(secondary);
    }
}