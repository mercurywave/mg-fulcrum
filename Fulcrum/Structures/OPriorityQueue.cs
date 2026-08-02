using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

// lowest priority comes first
// there's one of these in .net now, so this might not be needed
public class OPriorityQueue<Priority, T>
{
    private SortedDictionary<Priority, Queue<T>> list = new SortedDictionary<Priority, Queue<T>>();
    public void Enqueue(Priority priority, T value)
    {
        Queue<T> q;
        if (!list.TryGetValue(priority, out q))
        {
            q = new Queue<T>();
            list.Add(priority, q);
        }
        q.Enqueue(value);
    }

    //grab smallest priority
    public T DequeueLowest()
    {
        // will throw if there isn’t any first element!
        var pair = list.First();
        var v = pair.Value.Dequeue();
        if (pair.Value.Count == 0) // nothing left of the top priority.
            list.Remove(pair.Key);
        return v;
    }

    public T DequeueHighest()
    {
        // will throw if there isn’t any last element!
        var pair = list.Last();
        var v = pair.Value.Dequeue();
        if (pair.Value.Count == 0) // nothing left of the top priority.
            list.Remove(pair.Key);
        return v;
    }

    public Queue<T> DequeueAllLowest()
    {
        var low = LowestPriority();
        var ret = list[low];
        list.Remove(low);
        return ret;
    }

    public Priority LowestPriority()
    {
        return list.First().Key;
    }

    //note: destructive (duh?)
    public IEnumerable<T> DequeueAll()
    {
        while (list.Any())
            yield return DequeueLowest();
    }

    public IEnumerable<T> Iterate()
    {
        foreach (var l in list)
            foreach (var i in l.Value)
                yield return i;
    }

    public List<T> Flatten() => Iterate().ToList();
    public int Count() => list.Sum(l => l.Value.Count());

    public bool Any(Func<T, bool> test) => list.Any(l => l.Value.Any(test));

    public void Clear() => list.Clear();

    public bool IsEmpty
    {
        get { return !list.Any(); }
    }

    public int CountNodes()
    {
        int tot = 0;
        foreach (Queue<T> q in list.Values)
            tot += q.Count();
        return tot;
    }
}
public class OPriorityQueueCustom<Priority, T>
{
    private SortedDictionary<Priority, Queue<T>> list = new SortedDictionary<Priority, Queue<T>>();
    public void Enqueue(Priority priority, T value)
    {
        Queue<T> q;
        if (!list.TryGetValue(priority, out q))
        {
            q = new Queue<T>();
            list.Add(priority, q);
        }
        q.Enqueue(value);
    }

    //grab smallest priority
    public T DequeueLowest()
    {
        // will throw if there isn’t any first element!
        var pair = list.First();
        var v = pair.Value.Dequeue();
        if (pair.Value.Count == 0) // nothing left of the top priority.
            list.Remove(pair.Key);
        return v;
    }

    public T DequeueHighest()
    {
        // will throw if there isn’t any last element!
        var pair = list.Last();
        var v = pair.Value.Dequeue();
        if (pair.Value.Count == 0) // nothing left of the top priority.
            list.Remove(pair.Key);
        return v;
    }

    public Queue<T> DequeueAllLowest()
    {
        var low = LowestPriority();
        var ret = list[low];
        list.Remove(low);
        return ret;
    }

    public Priority LowestPriority()
    {
        return list.First().Key;
    }

    //note: destructive (duh?)
    public IEnumerable<T> DequeueAll()
    {
        while (list.Any())
            yield return DequeueLowest();
    }

    public IEnumerable<T> Iterate()
    {
        foreach (var l in list)
            foreach (var i in l.Value)
                yield return i;
    }

    public List<T> Flatten() => Iterate().ToList();
    public int Count() => list.Sum(l => l.Value.Count());

    public bool Any(Func<T, bool> test) => list.Any(l => l.Value.Any(test));

    public void Clear() => list.Clear();

    public bool IsEmpty
    {
        get { return !list.Any(); }
    }

    public int CountNodes()
    {
        int tot = 0;
        foreach (Queue<T> q in list.Values)
            tot += q.Count();
        return tot;
    }
}