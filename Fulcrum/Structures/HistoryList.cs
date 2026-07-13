

using System;
using System.Collections.Generic;

namespace Fulcrum;

public class HistoryList<T>
{
    T[] _arr;
    int _count = 0;
    int _start = 0; // start is the most recent, and rotates through the array. pushing moves start ahead one slot and places the new thing there
    public HistoryList(int cap)
    {
        _arr = new T[cap];
    }

    public void Push(T value)
    {
        _start = (_start + 1) % _arr.Length;
        _arr[_start] = value;
        if (_count < _arr.Length) _count++;
    }

    //loop from most recent entry to the earliest entry
    public IEnumerable<T> LoopBackwards()
    {
        for (int i = 0; i < _count; i++)
            yield return Get(i);
    }
    //loop from most recent entry to the earliest entry
    public IEnumerable<T> LoopForwards()
    {
        for (int i = _count - 1; i >= 0; i--)
            yield return Get(i);
    }

    // idx is the xth interval previous to the current one
    public T Get(int idx)
    {
        return _arr[GMath.Mod(_start - idx, _arr.Length)];
    }
    public int Count => _count;

    public void Clear()
    {
        _count = 0;
        _start = 0;
    }

    //removes the most recent element off of the history list and returns it (like undo)
    public T Pop()
    {
        if (Count == 0) throw new Exception("History list is empty and cannot be popped");
        var elem = Get(0);
        _start = GMath.Mod(_start - 1, _arr.Length);
        _count--;
        return elem;
    }

    public T this[int i] => Get(i);
}