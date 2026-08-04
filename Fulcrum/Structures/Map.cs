using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;
//one to many map
public class Map<Key, Value>
{
    Dictionary<Key, List<Value>> _map;
    public Map()
    {
        _map = new Dictionary<Key, List<Value>>();
    }

    public void Add(Key key, Value value)
    {
        if (!_map.ContainsKey(key)) _map.Add(key, new List<Value>());
        _map[key].Add(value);
    }

    public void Kill(Key key)
    {
        _map.Remove(key);
    }


    public void Kill(Key key, Value value)
    {
        _map[key].Remove(value);
        if (_map[key].Count == 0) _map.Remove(key);
    }

    public bool Exists(Key key)
    {
        return _map.Keys.Contains(key);
    }

    public bool Exists(Key key, Value value)
    {
        if (!_map.Keys.Contains(key)) return false;
        return _map[key].Contains(value);
    }

    public IEnumerable<Key> Keys()
    {
        foreach (Key key in _map.Keys)
            yield return key;
    }

    public IEnumerable<Value> Values()
    {
        foreach (Key key in _map.Keys)
            foreach (Value val in _map[key])
                yield return val;
    }

    public IEnumerable<Value> Values(Key key)
    {
        if (key != null)
            if (_map.Keys.Contains(key))
                foreach (Value val in _map[key])
                    yield return val;
    }
    public int KeyCount => _map.Count;
    public int ValueCount(Key key) => _map.ContainsKey(key) ? _map[key].Count : 0;
    public void Clear() => _map.Clear();

    public IEnumerable<KeyValuePair<Key, Value>> AllPairs()
    {
        foreach (Key key in _map.Keys)
            foreach (Value val in _map[key])
                yield return new KeyValuePair<Key, Value>(key, val);
    }

    public void KillKeys(Predicate<Key> where)
    {
        var kill = _map.Keys.Where(i => where(i)).ToList();
        foreach (var k in kill)
            Kill(k);
    }

    public static Map<K, V> SplitList<K, V>(List<V> list, Func<V, K> mapFunc)
    {
        var map = new Map<K, V>();
        foreach (var elem in list)
            map.Add(mapFunc(elem), elem);
        return map;
    }
}