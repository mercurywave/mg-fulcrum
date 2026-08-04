using System;
using System.Collections.Generic;

namespace Fulcrum;

//like a sorted dictionary, but assumes all misses return the fallback
// RELATED: LookbackList
class SparseIndex<T, U>
{
    Dictionary<T, U> _dic = new Dictionary<T, U>();
    Func<U> _fallback; // there are probably scenarios where a delegate is slow
    public SparseIndex(U fallback)
    {
        _fallback = () => fallback;
    }
    public SparseIndex(Func<U> fallback) { _fallback = fallback; }
    public bool ContainsKey(T key) => _dic.ContainsKey(key);
    public U GetValue(T key)
    {
        if (ContainsKey(key)) return _dic[key];
        return _fallback();
    }
    public void SetValue(T key, U value)
    {
        if (!ContainsKey(key)) _dic.Add(key, value);
        else _dic[key] = value;
    }
    public U this[T i]
    {
        get { return GetValue(i); }
        set { SetValue(i, value); }
    }
    public void Remove(T key)
    {
        if (ContainsKey(key)) _dic.Remove(key);
    }
}