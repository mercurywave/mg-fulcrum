using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

public class LookbackList<T, U> where T : IComparable
{
    public SortedDictionary<T, U> _dic = new SortedDictionary<T, U>();
    public U _default;

    public LookbackList(U defaultValue)
    {
        _default = defaultValue;
    }

    public void Add(T key, U value)
    {
        _dic.Add(key, value);
    }

    //Add or update
    public void Set(T key, U value)
    {
        if (_dic.ContainsKey(key))
            _dic[key] = value;
        else _dic.Add(key, value);
    }

    //if the exact key is specified, the value for that key is returned
    public U Lookback(T key)
    {
        U best = _default;
        foreach (var pair in _dic)
        {
            if (pair.Key.CompareTo(key) > 0)
                break;
            best = pair.Value;
        }
        return best;
    }
    public U LookAhead(T key)
    {
        U best = _default;
        foreach (var pair in _dic.Reverse())
        {
            if (pair.Key.CompareTo(key) < 0)
                break;
            best = pair.Value;
        }
        return best;
    }

    public bool HasExactKey(T key) => _dic.ContainsKey(key);
    public U GetExactKey(T key) => _dic[key];

    public void Clear() => _dic.Clear();

    //if the key is in the list, the one prior to that is returned - unlike lookback!
    public T LastKey(T key, T bound)
    {
        T best = bound;
        foreach (var k in _dic.Keys)
        {
            if (k.CompareTo(key) >= 0)
                break;
            best = k;
        }
        return best;
    }
    public T NextKey(T key, T bound)
    {
        T best = bound;
        foreach (var k in _dic.Keys.Reverse())
        {
            if (k.CompareTo(key) <= 0)
                break;
            best = k;
        }
        return best;
    }
    public U Default { get { return _default; } set { _default = value; } }
    public IEnumerable<U> Values => _dic.Values;
}