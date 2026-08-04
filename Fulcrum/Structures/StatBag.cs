using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

public class StatBag<T> where T : IComparable<T>
{
    public SortedDictionary<T, int> _bag = new SortedDictionary<T, int>();

    public StatBag()
    {
    }
    public StatBag(T k1, int v1)
    {
        _bag.Add(k1, v1);
    }
    public StatBag(T k1, int v1, T k2, int v2)
    {
        _bag.Add(k1, v1);
        _bag.Add(k2, v2);
    }
    public StatBag(T k1, int v1, T k2, int v2, T k3, int v3)
    {
        _bag.Add(k1, v1);
        _bag.Add(k2, v2);
        _bag.Add(k3, v3);
    }

    public int AddCapped(T stat, int amount, T cap)
    {
        var max = Check(cap);
        return AddCapped(stat, amount, max);
    }

    public int AddCapped(T stat, int amount, int max)
    {
        var curr = Check(stat);
        var next = GMath.Clamp(curr + amount, 0, max);
        if (curr != next)
            Set(stat, next);
        return next - curr;
    }

    public int Add(T stat, int amount)
    {
        //need key check?
        if (!_bag.ContainsKey(stat))
            _bag.Add(stat, 0);
        var prev = _bag[stat];
        _bag[stat] += amount;
        if (_bag[stat] < 0)
            _bag[stat] = 0;
        return _bag[stat] - prev;
    }
    public void Subtract(T stat, int amount) => Add(stat, -amount);
    public void Set(T stat, int amount)
    {
        //need key check?
        if (!_bag.ContainsKey(stat))
            _bag.Add(stat, amount);
        _bag[stat] = amount;
    }

    public bool Any => LoopStats().Any();
    public bool IsEmpty => !Any;
    public int Sum() => _bag.Sum(p => p.Value);

    public int Check(T res)
    {
        var val = safeGetStat(res);
        return val;
    }

    int safeGetStat(T res)
    {
        if (_bag.ContainsKey(res))
            return _bag[res];
        return 0;
    }

    public void Clear()
    {
        foreach (var pair in _bag.ToArray()) // make copy
            Add(pair.Key, -pair.Value); //lay-zee!
    }

    public void Clear(T res) => Add(res, -safeGetStat(res)); //lay-zee!

    public void MergeInDelta(DeltaBag<T> delta)
    {
        foreach (KeyValuePair<T, int> pair in delta.LoopPairs())
            Add(pair.Key, pair.Value);
    }
    public void MergeInDelta(StatBag<T> delta) => MergeInDelta(delta._bag);
    void MergeInDelta(SortedDictionary<T, int> delta)
    {
        foreach (KeyValuePair<T, int> pair in delta)
            Add(pair.Key, pair.Value);
    }

    public void SubtractDelta(StatBag<T> delta) => SubtractDelta(delta._bag);
    void SubtractDelta(SortedDictionary<T, int> delta)
    {
        foreach (KeyValuePair<T, int> pair in delta)
            Add(pair.Key, -pair.Value);
    }

    // loops over non-zero stats
    public IEnumerable<T> LoopStats()
    {
        foreach (var p in _bag)
            if (p.Value > 0)
                yield return p.Key;
    }
    public IEnumerable<KeyValuePair<T, int>> LoopPairs()
    {
        foreach (var p in _bag)
            if (p.Value > 0)
                yield return p;
    }

    public override string ToString()
    {
        string temp = "StatBag ";
        foreach (var p in LoopPairs())
            temp += " " + p.Key.ToString() + ":" + p.Value;
        return temp;
    }
}

// because stat bags can't have negatives
public class DeltaBag<T>
{
    Dictionary<T, int> _bag = new Dictionary<T, int>();
    public DeltaBag()
    {
    }
    public DeltaBag(T k1, int v1)
    {
        _bag.Add(k1, v1);
    }
    public DeltaBag(T k1, int v1, T k2, int v2)
    {
        _bag.Add(k1, v1);
        _bag.Add(k2, v2);
    }
    public DeltaBag(T k1, int v1, T k2, int v2, T k3, int v3)
    {
        _bag.Add(k1, v1);
        _bag.Add(k2, v2);
        _bag.Add(k3, v3);
    }
    public int Add(T stat, int amount)
    {
        //need key check?
        if (!_bag.ContainsKey(stat))
            _bag.Add(stat, 0);
        var prev = _bag[stat];
        _bag[stat] += amount;
        return _bag[stat] - prev;
    }
    public void Subtract(T stat, int amount) => Add(stat, -amount);
    public void Set(T stat, int amount)
    {
        //need key check?
        if (!_bag.ContainsKey(stat))
            _bag.Add(stat, amount);
        _bag[stat] = amount;
    }
    public bool Any => LoopStats().Any();
    public bool IsEmpty => !Any;
    public int Sum() => _bag.Sum(p => p.Value);

    public int Check(T res)
    {
        var val = safeGetStat(res);
        return val;
    }

    int safeGetStat(T res)
    {
        if (_bag.ContainsKey(res))
            return _bag[res];
        return 0;
    }

    public void Clear()
    {
        foreach (var pair in _bag.ToArray()) // make copy
            Add(pair.Key, -pair.Value); //lay-zee!
    }

    public void Clear(T res) => Add(res, -safeGetStat(res)); //lay-zee!

    // loops over non-zero stats
    public IEnumerable<T> LoopStats()
    {
        foreach (var p in _bag)
            if (p.Value != 0)
                yield return p.Key;
    }
    public IEnumerable<KeyValuePair<T, int>> LoopPairs()
    {
        foreach (var p in _bag)
            if (p.Value != 0)
                yield return p;
    }

    public override string ToString()
    {
        string temp = "DeltaBag ";
        foreach (var p in LoopPairs())
            temp += " " + p.Key.ToString() + ":" + p.Value;
        return temp;
    }
}