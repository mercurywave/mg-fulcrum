
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public class Grid<T>
{
    public int W, H;
    T _def;
    T[,] _arr;

    public delegate bool CellMeetsCriteriaDelegate(T cell);

    public Grid(int w, int h, T def)
    {
        _def = def;
        W = w;
        H = h;
        _arr = new T[w, h];
    }

    public T Get(int x, int y) { return _Get(x, y); }
    public T Get(Point pt) { return _Get(pt.X, pt.Y); }
    public T GetUnsafe(Point pos) { return _Get(pos); }
    T _Get(Point pos)
    {
        if (!InGrid(pos)) return _def;
        return _arr[pos.X, pos.Y];
    }
    T _Get(int x, int y)
    {
        if (!InGrid(x, y)) return _def;
        return _arr[x, y];
    }

    public T GetRelative(Point pt, int dx, int dy) => _Get(pt.X + dx, pt.Y + dy);
    public bool InGrid(Point pt) => InGrid(pt.X, pt.Y);
    public bool InGrid(int x, int y)
        => x >= 0 && x < W && y >= 0 && y < H;
    public bool OnEdge(Point pos)
        => pos.X == 0 || pos.X == W - 1 || pos.Y == 0 || pos.Y == H - 1;

    public T GetSafe(Point pos)
        => _arr[pos.X, pos.Y];
    public IEnumerable<Point> AllPos()
    {
        List<Point> list = new List<Point>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                list.Add(new Point(x, y));
        return list;
    }
    public IEnumerable<Tuple<Point, T>> AllCells()
    {
        List<Tuple<Point, T>> list = new List<Tuple<Point, T>>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                list.Add(new Tuple<Point, T>(new Point(x, y), _arr[x, y]));
        return list;
    }
    public IEnumerable<Tuple<Point, T>> AllCells(Predicate<T> where)
    {
        List<Tuple<Point, T>> list = new List<Tuple<Point, T>>();
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                var obj = _arr[x, y];
                if (where(obj))
                    list.Add(new Tuple<Point, T>(new Point(x, y), obj));
            }
        return list;
    }
    //step through in some direction - don't assume the x/y progression
    public IEnumerable<Point> AllPos(bool xAsc, bool yAsc)
    {
        int x0 = xAsc ? 0 : W - 1;
        int xEnd = xAsc ? W : -1;
        int dx = xAsc ? 1 : -1;
        int y0 = yAsc ? 0 : H - 1;
        int yEnd = yAsc ? H : -1;
        int dy = yAsc ? 1 : -1;
        for (int x = x0; x != xEnd; x += dx)
            for (int y = y0; y != yEnd; y += dy)
                yield return new Point(x, y);
    }

    //scanlines going from start to end
    public IEnumerable<Point> PosInRegionDiagonal(Rectangle region)
        => PosInRegionDiagonal(region.GetTopLeft(), region.GetBottomRight());
    public IEnumerable<Point> PosInRegionDiagonal(Point start, Point end)
    {
        int dx = Math.Sign(end.X - start.X);
        int dy = Math.Sign(end.Y - start.Y);
        int diagx = -dx;
        int diagy = dy;

        foreach (var x in LoopRangeDir(start.X, end.X, dx))
            for (int off = 0; off <= Math.Min(Math.Abs(start.Y - end.Y), Math.Abs(start.X - x)); off++)
                yield return new Point(x + diagx * off, start.Y + diagy * off);
        foreach (var y in LoopRangeDir(start.Y + dy, end.Y, dy))
            for (int off = 0; off <= Math.Min(Math.Abs(start.Y - end.Y) - 1, Math.Abs(end.Y - y)); off++)
                yield return new Point(end.X + diagx * off, y + diagy * off);
    }

    static IEnumerable<int> LoopRangeDir(int start, int end, int dx)
    {
        if (dx > 0)
            for (int i = start; i <= end; i += dx)
                yield return i;
        else
            for (int i = start; i >= end; i += dx)
                yield return i;
    }

    public IEnumerable<Point> FilteredPos(CellMeetsCriteriaDelegate criteria)
    {
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                if (criteria(_arr[x, y]))
                    yield return new Point(x, y);
    }

    // manhatten adjacent cells
    public IEnumerable<Point> SafePosAdjacent(Point start)
    {
        if (InGrid(start.X - 1, start.Y))
            yield return new Point(start.X - 1, start.Y);
        if (InGrid(start.X, start.Y - 1))
            yield return new Point(start.X, start.Y - 1);
        if (InGrid(start.X + 1, start.Y))
            yield return new Point(start.X + 1, start.Y);
        if (InGrid(start.X, start.Y + 1))
            yield return new Point(start.X, start.Y + 1);
    }

    public IEnumerable<Point> PosInRegion(Point start, int w, int h)
    {
        for (int x = start.X; x < W && x < w + start.X; x++)
            for (int y = start.Y; y < H && y < h + start.Y; y++)
                yield return new Point(x, y);
    }
    public IEnumerable<T> DataInRegion(Point start, int w, int h)
    {
        for (int x = start.X; x < W && x < w + start.X; x++)
            for (int y = start.Y; y < H && y < h + start.Y; y++)
                yield return GetSafe(new Point(x, y));
    }
    public IEnumerable<Point> PosInRegion(Point start, Point end)
    {
        if (start.X > end.X || start.Y > end.Y)
            throw new Exception("start and end not in expected order : PosInRegion");
        for (int x = start.X; x <= end.X; x++)
            for (int y = start.Y; y <= end.Y; y++)
                yield return new Point(x, y);
    }
    public IEnumerable<Point> PosInSafeRegion(Point start, int w, int h)
    {
        for (int x = GMath.Max(start.X, 0); x < W && x < w + start.X; x++)
            for (int y = GMath.Max(start.Y, 0); y < H && y < h + start.Y; y++)
                yield return new Point(x, y);
    }

    public void Set(Point pos, T val) => _arr[pos.X, pos.Y] = val;

    public void Set(int x, int y, T val) => _arr[x, y] = val;

    public void Fill(T val)
    {
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
                _arr[x, y] = val;
    }

    //simple manhattan style flood fill of matching cells
    public void FloodFill(T val, Point start, Predicate<Point> match)
    {
        Grid<bool> visited = new Grid<bool>(W, H, false);
        FloodHelp(val, new Point(start.X, start.Y), match, visited);
    }
    void FloodHelp(T val, Point start, Predicate<Point> match, Grid<bool> visited)
    {
        if (!InGrid(start)) return;
        var sfpos = new Point(start.X, start.Y);
        if (visited.Get(sfpos)) return;
        if (!match(sfpos)) return;

        visited.Set(sfpos, true);
        Set(sfpos, val);

        FloodHelp(val, new Point(sfpos.X - 1, sfpos.Y), match, visited);
        FloodHelp(val, new Point(sfpos.X, sfpos.Y - 1), match, visited);
        FloodHelp(val, new Point(sfpos.X + 1, sfpos.Y), match, visited);
        FloodHelp(val, new Point(sfpos.X, sfpos.Y + 1), match, visited);
    }
    public List<Point> FloodGetTiles(Point start, Predicate<T> match)
        => FloodGetTilesPos(start, p => match(Get(p)));
    public List<Point> FloodGetTilesPos(Point start, Predicate<Point> match)
    {
        if (!match(start)) return new List<Point>();
        Grid<bool> visited = new Grid<bool>(W, H, false);
        Queue<Point> toProc = new Queue<Point>();
        List<Point> list = new List<Point>();
        visited.Set(start, true);
        toProc.Enqueue(start);
        list.Add(start);
        while (toProc.Count > 0)
        {
            Point cell = toProc.Dequeue();
            foreach (var neigh in IterSafeNeighbors(cell))
            {
                if (visited.Get(neigh)) continue;
                visited.Set(neigh, true);
                if (match(neigh))
                {
                    toProc.Enqueue(neigh);
                    list.Add(neigh);
                }
            }
        }
        return list;
    }
    public Grid<int> CalcDistanceGrid(Point start, Predicate<T> walkable, int cap = int.MaxValue)
    {
        // calculates a walkable distance grid from a point outward
        var dist = new Grid<int>(W, H, int.MaxValue);
        dist.Fill(int.MaxValue);
        dist.Set(start, 0);
        // this takes advantage of FloodGetTilesPos returning points in effectively breadth first order
        foreach (var tile in FloodGetTilesPos(start, p => walkable(Get(p))).Skip(1)) // skip the start tile
        {
            var near = IterSafeNeighbors(tile).ToList();
            var min = near.Min(p => dist.Get(p));
            if (min == int.MaxValue && !tile.Equals(start))
                throw new Exception("made bad assumptions about how tiles are walked");
            dist.Set(tile, min + 1);
        }
        return dist;
    }

    public bool Any(Predicate<T> match)
    {
        foreach (Point pos in AllPos())
            if (match(Get(pos))) return true;
        return false;
    }

    public delegate T GridConstructorHandler(int x, int y);
    public void Fill(GridConstructorHandler hook)
    {
        foreach (Point pos in AllPos())
            Set(pos, hook(pos.X, pos.Y));
    }
    public void FillRegion(T val, Point start, Point end)
    {
        foreach (Point pos in PosInRegion(start, end))
            Set(pos, val);
    }

    //replace every mathing cell with replace
    public void ReplaceWhere(Predicate<T> match, T replace)
    {
        foreach (Point pos in AllPos())
            if (match(GetSafe(pos)))
                Set(pos, replace);
    }

    public IEnumerable<Point> IterSafeNeighbors(Point pos)
    {
        if (pos.X > 0) yield return new Point(pos.X - 1, pos.Y);
        if (pos.Y > 0) yield return new Point(pos.X, pos.Y - 1);
        if (pos.X < W - 1) yield return new Point(pos.X + 1, pos.Y);
        if (pos.Y < H - 1) yield return new Point(pos.X, pos.Y + 1);
    }

    #region pathfinding
    //attempt to path in the grid, does not venture outside
    //assumes manhattan movement
    // queue does not include from, but does include to
    // that means that you may have to go out of your way to make sure the target cell is traversable :(
    // cap search does not mean that this direction will actually ultimately lead to the target
    public Queue<Point> TryPathTo(Point from, Point to, Func<Point, float> hookCost, Func<Point, bool> hookCanTraverse, int? capSearch = null)
    {
        Grid<float> distances = new Grid<float>(W, H, float.MaxValue);
        return TryPathToDebug(from, to, hookCost, hookCanTraverse, capSearch, out distances);
    }
    public Queue<Point> TryPathToDebug(Point from, Point to, Func<Point, float> hookCost, Func<Point, bool> hookCanTraverse, int? capSearch, out Grid<float> distances)
    {
        distances = new Grid<float>(W, H, float.MaxValue);
        OPriorityQueue<float, Point> _nextSteps = new OPriorityQueue<float, Point>();
        distances.Fill(-1);
        distances.Set(from, 0);
        PathAddNextSteps(from, to, distances, _nextSteps, hookCost, hookCanTraverse);
        while (!_nextSteps.IsEmpty && distances.Get(to) < 0)
        {
            var next = _nextSteps.DequeueLowest();
            var dist = PathAddNextSteps(next, to, distances, _nextSteps, hookCost, hookCanTraverse);
            // if the closest possible next step is more than capSearch away, we aren't going to find a path
            // NOTE: historically, this used to work more like a count of processed cells, but that's really unintuitive
            if (capSearch.HasValue && dist > capSearch)
                break;
        }
        if (distances.Get(to) < 0) return null; // never made it!
        List<Point> steps = new List<Point>();
        steps.Add(to); // TODO: this API says it includes the to, so why wasn't it?
        PathWalk(from, to, distances, steps);
        return new Queue<Point>(steps.Reverse<Point>());
    }
    float PathAddNextSteps(Point pos, Point to, Grid<float> _distances, OPriorityQueue<float, Point> _nextSteps, Func<Point, float> hookCost, Func<Point, bool> hookCanTraverse)
    {
        var baseCost = _distances.Get(pos);
        PathTryQueuePos(new Point(pos.X + 1, pos.Y), to, _distances, baseCost, _nextSteps, hookCost, hookCanTraverse);
        PathTryQueuePos(new Point(pos.X, pos.Y + 1), to, _distances, baseCost, _nextSteps, hookCost, hookCanTraverse);
        PathTryQueuePos(new Point(pos.X - 1, pos.Y), to, _distances, baseCost, _nextSteps, hookCost, hookCanTraverse);
        PathTryQueuePos(new Point(pos.X, pos.Y - 1), to, _distances, baseCost, _nextSteps, hookCost, hookCanTraverse);
        return baseCost;
    }
    void PathTryQueuePos(Point pos, Point to, Grid<float> _distances, float baseCost, OPriorityQueue<float, Point> _nextSteps, Func<Point, float> hookCost, Func<Point, bool> hookCanTraverse)
    {
        if (!InGrid(pos)) return;
        if (!hookCanTraverse(pos)) return;
        var cost = hookCost(pos) + baseCost;

        var currCost = _distances.Get(pos);
        if (currCost >= 0 && cost >= currCost) return; // already processed that cell cheaper

        _distances.Set(pos, cost);

        var dist = pos.BirdDistanceTo(to);
        _nextSteps.Enqueue(dist + cost, pos);
    }
    void PathWalk(Point from, Point to, Grid<float> _distances, List<Point> steps)
    {
        OPriorityQueue<float, Point> surround = new OPriorityQueue<float, Point>();
        PathTryStep(new Point(to.X + 1, to.Y), _distances, surround);
        PathTryStep(new Point(to.X, to.Y + 1), _distances, surround);
        PathTryStep(new Point(to.X - 1, to.Y), _distances, surround);
        PathTryStep(new Point(to.X, to.Y - 1), _distances, surround);
        var pick = surround.DequeueLowest();
        if (pick.Equals(from)) return;
        steps.Add(pick);
        PathWalk(from, pick, _distances, steps);
    }
    void PathTryStep(Point to, Grid<float> _distances, OPriorityQueue<float, Point> surround)
    {
        if (!InGrid(to)) return;
        var dist = _distances.Get(to);
        if (dist < 0) return;
        surround.Enqueue(dist, to);
    }
    #endregion

    public Grid<T> CopyRegion(int x, int y, int w, int h)
    {
        Debug.Assert(InGrid(x, y));
        Debug.Assert(InGrid(x + w - 1, y + h - 1));
        Point start = new Point(x, y);
        Grid<T> copy = new Grid<T>(w, h, Get(start));
        foreach (var cell in PosInRegion(start, w, h))
            copy.Set(cell.X - x, cell.Y - y, Get(cell));
        return copy;
    }

    #region find closest
    // manhattan walk, assumes walkable-ness is not directional
    // returns all cells at the shortest walk distance that match
    // returns empty list if there is no walkable route
    // if the start cell matches, it will be returned
    public List<Point> FindAllClosestWalk(Point start, Predicate<T> match, Predicate<T> walkable)
        => FindAllClosestWalkPos(start, p => match(GetSafe(p)), p => walkable(GetSafe(p)));

    public List<Point> FindAllClosestWalkPos(Point start, Predicate<Point> match, Predicate<Point> walkable)
    {
        if (match(start)) return new List<Point>() { start };
        Grid<bool> peeked = new Grid<bool>(W, H, false);
        List<Point> queue = new List<Point>();
        peeked.Set(start, true);
        queue.Add(start);
        List<Point> found = new List<Point>();
        while (queue.Any() && !found.Any())
        {
            var todo = queue;
            queue = new List<Point>();
            foreach (var curr in todo)
            {
                if (match(curr)) found.Add(curr);
                if (!found.Any())
                {
                    foreach (var neighbor in IterSafeNeighbors(curr))
                    {
                        if (!peeked.Get(neighbor))
                        {
                            peeked.Set(neighbor, true);
                            if (walkable(neighbor))
                                queue.Add(neighbor);
                        }
                    }
                }
            }
        }
        return found;
    }
    #endregion
}