namespace Fulcrum;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

public class Distributor<A>
{
    public Dictionary<A, int> _dic = new Dictionary<A, int>();
    public static Random Seeder = new Random();
    Random _seed;
    int total;
    A _default;

    public Distributor(A defaultValue)
    {
        _seed = Seeder;
        _default = defaultValue;
    }

    public Distributor(A defaultValue, Random seed)
    {
        _seed = seed;
        _default = defaultValue;
    }

    public void AddNode(A key, int value = 1)
    {
        if (value < 0) return; // negatives are worse than nothing
        if (!_dic.ContainsKey(key)) _dic[key] = 0;
        _dic[key] += value;
        total += value;
    }
    public void AddMultiple(int value, params A[] keys)
    {
        foreach (var k in keys)
            AddNode(k, value);
    }

    public void RemoveNode(A key)
    {
        if (_dic.ContainsKey(key)) total -= _dic[key];
        _dic.Remove(key);
    }

    //increase the likelyhood of the default scenario (null)
    //only works on random picker
    public void SetDefaultChance(int num)
    {
        total += num;
    }

    public void Reset()
    {
        _dic.Clear();
        total = 0;
    }

    public IEnumerable<A> Nodes()
    {
        foreach (KeyValuePair<A, int> pair in _dic)
            yield return pair.Key;
    }

    public IEnumerable<KeyValuePair<A, int>> NodeWeights()
    {
        foreach (KeyValuePair<A, int> pair in _dic)
            yield return pair;
    }

    public int Count { get { return _dic.Count; } }
    public int TotalChance => total;

    //always check count before calling!
    public A GetRandomNode(Random seed = null)
    {
        int temp;
        return GetRandomNode(out temp, seed);
    }

    public A GetRandomNode(out int value, Random seed = null)
    {
        value = 0;
        if (_dic.Count == 0) return _default;
        int rand;
        if (seed == null) seed = Seeder;
        rand = seed.Next(total);
        foreach (KeyValuePair<A, int> pair in _dic)
        {
            rand -= pair.Value;
            if (rand < 0)
            {
                value = pair.Value;
                return pair.Key;
            }
        }
        return _default;
    }

    //highest value
    public A GetHighestNode(Random seed = null)
    {
        if (_dic.Count == 0) return _default;
        List<A> bestof = new List<A>();
        int best = int.MinValue;
        foreach (KeyValuePair<A, int> pair in _dic)
        {
            if (pair.Value > best)
            {
                bestof = new List<A>();
                best = pair.Value;
            }
            if (pair.Value == best)
            {
                bestof.Add(pair.Key);
            }
        }
        if (seed == null) seed = Seeder;
        int rand = seed.Next(bestof.Count);
        //int rand = bestof.Count - 1;
        return bestof[rand];
    }

    public IEnumerable<A> LoopHighestNodes()
    {
        if (_dic.Count == 0) yield return _default;
        else
        {
            OPriorityQueue<double, A> queue = new OPriorityQueue<double, A>();
            foreach (KeyValuePair<A, int> pair in _dic)
                queue.Enqueue(-pair.Value + Seeder.NextDouble(), pair.Key);
            foreach (A node in queue.DequeueAll())
                yield return node;
        }
    }

    public IEnumerable<A> LoopLowestNodes()
    {
        if (_dic.Count == 0) yield return _default;
        else
        {
            OPriorityQueue<double, A> queue = new OPriorityQueue<double, A>();
            foreach (KeyValuePair<A, int> pair in _dic)
                queue.Enqueue(pair.Value + Seeder.NextDouble(), pair.Key);
            foreach (A node in queue.DequeueAll())
                yield return node;
        }
    }

    //lowest value
    public A GetLowestNode(Random seed = null)
    {
        if (_dic.Count == 0) return _default;
        List<A> worstof = new List<A>();
        int worst = int.MaxValue;
        foreach (KeyValuePair<A, int> pair in _dic)
        {
            if (pair.Value < worst)
            {
                worstof = new List<A>();
                worst = pair.Value;
            }
            if (pair.Value == worst)
            {
                worstof.Add(pair.Key);
            }
        }
        if (seed == null) seed = Seeder;
        int rand = seed.Next(worstof.Count);
        //int rand = worstof.Count - 1;
        return worstof[rand];
    }

    //merge in a second distributor to this one
    public void MergeIn(Distributor<A> combine)
    {
        foreach (KeyValuePair<A, int> pair in combine.NodeWeights())
            AddNode(pair.Key, pair.Value);
    }
    //low to high
    public List<A> ToOrderedList()
    {
        List<A> list = new List<A>();
        OPriorityQueue<int, A> pri = new OPriorityQueue<int, A>();
        foreach (KeyValuePair<A, int> pair in _dic)
            pri.Enqueue(pair.Value, pair.Key);
        foreach (A node in pri.DequeueAll())
            list.Add(node);
        return list;
    }

    public Distributor<A> Clone()
    {
        Distributor<A> copy = new Distributor<A>(_default);
        int count = 0;
        foreach (var pair in _dic)
        {
            copy.AddNode(pair.Key, pair.Value);
            count += pair.Value;
        }
        copy.SetDefaultChance(total - count);
        return copy;
    }

    public bool HasNode(A node)
    {
        return _dic.ContainsKey(node);
    }
    public int GetNodeWeight(A node)
    {
        return _dic[node];
    }
}

//handle a common pattern of difficulty or complexity ramping up as the game goes one, this is like a distributor that changes as the level increases
//strongly encourage you to not grab the distributor and remove nodes... this caches that distributor
public class LeveledDistributor<T>
{
    List<Possible> _dic = new List<Possible>();
    T _default;
    KeyValuePair<int, Distributor<T>>? _cache = null;

    public LeveledDistributor(T defaultValue)
    {
        _default = defaultValue;
    }

    public void AddChanceConstant(T item, int frequency = 1)
    {
        AddChanceBetween(int.MinValue, int.MaxValue, item, frequency);
    }

    public void AddChanceAfter(int minLevel, T item, int frequency = 1)
    {
        AddChanceBetween(minLevel, int.MaxValue, item, frequency);
    }

    public void AddChanceBefore(int maxLevel, T item, int frequency = 1)
    {
        AddChanceBetween(int.MinValue, maxLevel, item, frequency);
    }

    public void AddChanceBetween(int minLevel, int maxLevel, T item, int frequency = 1)
    {
        Possible p = new Possible(item, minLevel, maxLevel, frequency);
        _dic.Add(p);
    }

    public Distributor<T> GetDistributorForLevel(int level)
    {
        return _GetDistributorForLevel(level).Clone();
    }

    internal Distributor<T> _GetDistributorForLevel(int level)
    {
        if (_cache == null || _cache.Value.Key != level)
        {
            Distributor<T> dist = new Distributor<T>(_default);
            foreach (var p in _dic)
                if (level >= p.MinLevel && level <= p.MaxLevel)
                    dist.AddNode(p.Item, p.Frequency);
            _cache = new KeyValuePair<int, Distributor<T>>(level, dist);
        }
        return _cache.Value.Value;
    }

    public T GetRandomNode(int level, Random seed = null) { return _GetDistributorForLevel(level).GetRandomNode(seed); }
    public T GetHighestNode(int level, Random seed = null) { return _GetDistributorForLevel(level).GetHighestNode(seed); }
    public T GetLowestNode(int level, Random seed = null) { return _GetDistributorForLevel(level).GetLowestNode(seed); }

    struct Possible
    {
        public int MinLevel;
        public int MaxLevel;
        public T Item;
        public int Frequency;
        public Possible(T item, int minlevel, int maxlevel, int frequency)
        {
            MinLevel = minlevel;
            MaxLevel = maxlevel;
            Item = item;
            Frequency = frequency;
        }
    }
}

public class MassSpringDistributor
{
    public static Random Seeder;
    Random _seed;

    class SpringNode
    {
        public double x;
        public double y;
        public double sprDown; // 0,0 is top left
        public double sprRight;

        public SpringNode(double x, double y, Random seed)
        {
            this.x = x;
            this.y = y;
            sprDown = seed.NextDouble() * 1.5 + .5;
            sprRight = seed.NextDouble() * 1.5 + .5;
        }
    }

    SpringNode[,] _grid;
    int _iterate;
    int _w, _h;
    public MassSpringDistributor(int horizontalPoints, int verticalPoints)
    {
        if (Seeder == null) Seeder = new Random();
        _seed = Seeder;
        _w = horizontalPoints;
        _h = verticalPoints;
        Setup();
    }

    public MassSpringDistributor(int horizontalPoints, int verticalPoints, Random seed)
    {
        _seed = seed;
        _w = horizontalPoints;
        _h = verticalPoints;
        Setup();
    }

    void Setup()
    {
        _iterate = _h;
        _grid = new SpringNode[_w, _h];
        for (int i = 0; i < _w; i++)
            for (int j = 0; j < _h; j++)
                _grid[i, j] = new SpringNode(i, j, _seed);
    }

    public async Task Compute()
    {
        for (int iter = 0; iter < _iterate; iter++)
        {
            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    var curr = _grid[x, y];
                    //vertical
                    double yAbove, yBelow;
                    double dAbove, dBelow;
                    if (y == 0)
                        yAbove = _grid[x, _h - 1].y - _h;
                    else
                        yAbove = _grid[x, y - 1].y;
                    if (y == _h - 1)
                        yBelow = _grid[x, 0].y + _h;
                    else
                        yBelow = _grid[x, y + 1].y;
                    dAbove = _grid[x, GMath.Mod(y - 1, _h)].sprDown;
                    dBelow = curr.sprDown;

                    curr.y = yAbove + (yBelow - yAbove) * dAbove / (dAbove + dBelow);

                    double xLeft, xRight;
                    double dLeft, dRight;
                    if (x == 0)
                        xLeft = _grid[_w - 1, y].x - _w;
                    else
                        xLeft = _grid[x - 1, y].x;
                    if (x == _w - 1)
                        xRight = _grid[0, y].x + _w;
                    else
                        xRight = _grid[x + 1, y].x;
                    dLeft = _grid[GMath.Mod(x - 1, _w), y].sprRight;
                    dRight = curr.sprRight;

                    curr.x = xLeft + (xRight - xLeft) * dLeft / (dLeft + dRight);
                }
            }
            await Task.Yield();
        }
    }

    public Point ProjectNormalized(Point vertex, int w, int h)
    {
        return ProjectNormalized(vertex.X, vertex.Y, w, h);
    }
    public Point ProjectNormalized(int x, int y, int w, int h)
    {
        var pt = _grid[x, y];
        return new Point(GMath.Mod((int)((pt.x + .5) * w / _w), w), GMath.Mod((int)((pt.y + .5) * h / _h), h));
    }

    public IEnumerable<Point> NormalizedToMap(int w, int h)
    {
        for (int x = 0; x < _w; x++)
        {
            for (int y = 0; y < _h; y++)
            {
                yield return ProjectNormalized(x, y, w, h);
            }
        }
    }
}
public class IntEvaluator<T>
{
    // optimized distributor for best-of comparison, where scoring each value is expensive
    private readonly Func<T, int> _scorer;
    public T Best;
    public int BestScore = int.MinValue;
    public bool HasValue => BestScore > int.MinValue; // has any comparison been done?
    public IntEvaluator(Func<T, int> scorer = null)
    {
        _scorer = scorer;
    }
    public bool Check(T value, int score, bool overwriteMatch = false)
    {
        if (score > BestScore || (overwriteMatch && score == BestScore))
        {
            Best = value;
            BestScore = score;
            return true;
        }
        return false;
    }
    public bool Check(T value, bool overwriteMatch = false)
        => Check(value, _scorer(value), overwriteMatch);
}
public class FloatEvaluator<T>
{
    // optimized distributor for best-of comparison, where scoring each value is expensive
    private readonly Func<T, float> _scorer;
    public T Best;
    public float BestScore = float.MinValue;
    public bool HasValue => BestScore > float.MinValue; // has any comparison been done?
    public FloatEvaluator(Func<T, float> scorer = null)
    {
        _scorer = scorer;
    }
    public bool Check(T value, float score)
    {
        if (score > BestScore)
        {
            Best = value;
            BestScore = score;
            return true;
        }
        return false;
    }
    // this assumes you have a scoring function
    public bool Check(T value)
        => Check(value, _scorer(value));
}
public class Evaluator<T>
{
    // implements a generic "Best of" pattern, which is hopefully cheaper than a distributor when you just need a simple best result
    private readonly Comparison<T> _comparer;
    public T Best;
    public bool HasValue = false; // has any comparison been done?
    public Evaluator(Comparison<T> comparer)
    {
        _comparer = comparer;
    }
    public Evaluator(Func<T, int> evaluation)
    {
        _comparer = (a, b) => evaluation(a).CompareTo(evaluation(b));
    }
    public bool Check(T value, bool overwriteMatch = false)
    {
        // returns true if this is currently the best
        if (!HasValue)
        {
            Best = value;
            HasValue = true;
            return true;
        }
        var comp = _comparer(Best, value);
        if (comp > 0 || (comp == 0 && overwriteMatch))
        {
            Best = value;
            return true;
        }
        return false;
    }
}

// simple bag of random elements that will be pulled from until empty, then refilled
public class RandBag<T>
{
    Random _seed;
    T[] _arr;
    int idx = 0;
    public RandBag(Random seed, params T[] poss)
    {
        _seed = seed;
        _arr = poss.ToArray();
        Reset();
    }
    public RandBag(params T[] poss) : this(new Random(), poss) { }

    public void Reset()
    {
        idx = 0;
        GUtil.Shuffle(_arr, _seed);
    }

    public T Pull()
    {
        var pop = _arr[idx++];
        if (idx >= _arr.Length)
            Reset();
        return pop;
    }

    public static RandBag<int> MakeRange(int incMin, int incMax)
    {
        var len = incMax - incMin + 1;
        var arr = new int[len];
        for (int i = 0; i < len; i++)
            arr[i] = i + incMin;
        return new RandBag<int>(arr);
    }
}