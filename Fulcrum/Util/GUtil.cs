
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

public static class GUtil
{
    static Random _rand = new Random();

    public static void Shuffle<A>(A[] list, Random seed = null)
    {
        if (seed == null) seed = _rand;
        for (int i = 0; i < list.Length; i++) //shufuffle!
        {
            A temp = list[i];
            int rand = seed.Next(list.Length);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }
    public static void Shuffle<A>(List<A> list, Random seed = null)
    {
        if (seed == null) seed = _rand;
        for (int i = 0; i < list.Count; i++) //shufuffle!
        {
            A temp = list[i];
            int rand = seed.Next(list.Count);
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    #region Quick picks

    public static I QuickPick<I>(params I[] arr)
    {
        return arr[_rand.Next(arr.Length)];
    }

    public static I QuickPick<I>(List<I> list)
    {
        return list[_rand.Next(list.Count)];
    }

    public static I Pick<I>(Random rand, params I[] arr)
    {
        int r = rand.Next(arr.Length);
        return arr[r];
    }
    public static I Pick<I>(Random rand, List<I> list)
    {
        int r = rand.Next(list.Count);
        return list[r];
    }
    public static I Pick<I>(Random rand, IEnumerable<I> list)
    {
        return Pick(rand, list.ToList());
    }
    public static List<I> QuickPickMulti<I>(int count, params I[] arr)
        => PickMulti(count, _rand, arr.ToList());
    public static List<I> QuickPickMulti<I>(int count, List<I> list)
        => PickMulti(count, _rand, list);
    public static List<I> PickMulti<I>(int count, Random rand, params I[] arr)
        => PickMulti(count, rand, arr.ToList());
    public static List<I> PickMulti<I>(int count, Random rand, List<I> list)
    {
        if (list.Count == 0) return new List<I>() { };
        var output = new List<I>(count);
        while (output.Count < count)
        {
            var copy = list.ToList();
            Shuffle(copy, rand);
            for (int i = 0; i < copy.Count && output.Count < count; i++)
                output.Add(copy[i]);
        }
        return output;
    }

    public static I PickBest<I>(IEnumerable<I> list, Func<I, int> bigFuncIsBestFunc)
    {
        List<I> poss = new List<I>();
        int comp = int.MinValue;
        foreach (var item in list)
        {
            var value = bigFuncIsBestFunc(item);
            if (value > comp)
            {
                comp = value;
                poss.Clear();
            }
            if (value == comp)
                poss.Add(item);
        }
        return QuickPick(poss);
    }

    public static I QuickPick<I>(IEnumerable<I> list)
    {
        return QuickPick(list.ToList());
    }

    public static I QuickPick<I>(Enum enumeration)
    {
        return QuickPick((I[])Enum.GetValues(enumeration.GetType()));
    }

    // caller needs to explicitly use <myEnum> and pass typeof(myEnum) for this to work
    public static I QuickPickEnum<I>(Type enumeration)
    {
        return QuickPick((I[])Enum.GetValues(enumeration));
    }

    public static I QuickPickEnum<I>(Random rand, Type enumeration)
    {
        return Pick(rand, (I[])Enum.GetValues(enumeration));
    }
    #endregion

    #region String stuff


    //append another piece to end (useful for multi-line output when some lines are blank)
    public static string AppendPiece(string start, string splitter, string append)
    {
        if (start == "") return append;
        return start + splitter + append;
    }

    // always returns at least one, I don't kno why...
    public static string RepeatString(string str, int count, string delimiter = "")
    {
        var output = str;
        for (int i = 0; i < count - 1; i++)
            output += delimiter + str;
        return output;
    }

    //safe and lazy sub string - so if you go over the length it returns up to the end
    public static string BoundedSubstr(string str, int start, int length)
    {
        if (start < 0) { length += start; start = 0; }
        if (start + length > str.Length) length = str.Length - start;
        if (length <= 0) return "";
        return str.Substring(start, length);
    }
    public static string BoundedSubstr(string str, int start)
        => BoundedSubstr(str, start, str.Length - start);

    public static string AppendLine(string start, string append) { return AppendPiece(start, "\n", append); }

    public static string Piece(string str, string delim, int pc, int pcTo = -1)
    {
        int start, end;
        if (pcTo < 0) pcTo = pc;
        start = DelimPos(str, delim, pc - 1);
        end = DelimPos(str, delim, pcTo, start, pc - 1);
        if (start == end) return "";
        return str.Substring(start + 1, end - start - 1);
    }
    static int DelimPos(string str, string delim, int pc, int start = -1, int startPc = 0)
    {
        if (pc <= startPc) return start;
        for (; startPc < pc && start < str.Length; startPc++)
        {
            start = str.IndexOf(delim, start + 1);
            if (start < 0) return str.Length;
        }
        return start;
    }

    #endregion
}