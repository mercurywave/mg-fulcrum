
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public static class GPerf
{
    internal enum eMajorTraceType { Update, Sync, Render, Draw, Other, KeyEvent, Async }
    internal enum eEventPhase { Begin, End, Event }
    const int FRAMES = 360;
    static HistoryList<FrameInfo> _recentFrames = new HistoryList<FrameInfo>(FRAMES);
    static HistoryList<TraceEvent> _events = new HistoryList<TraceEvent>(20000);
    static int _tick;

    public static bool Enabled = true;


    // calculate time diff in ms from stopwatch values
    public static double DurationMs(long start, long end) => 1000.0 * (end - start) / Stopwatch.Frequency;
    public static double DurationMs(long length) => 1000.0 * length / Stopwatch.Frequency;
    public static long MsToTicks(long ms) => ms * Stopwatch.Frequency / 1000;
    public static long MsToTicks(double ms) => (long)(ms * Stopwatch.Frequency / 1000.0);
    public static long Now() => Stopwatch.GetTimestamp();



    internal static void _BeginFrame(int tick)
    {
        for (int i = Math.Max(_tick, tick - 5); i <= tick; i++)
            CompileFrame(i);
        _tick = tick;
    }
    static void CompileFrame(int tick)
    {
        var active = GetEventsForFrame(tick, false)
            .ToList();

        if (active.Count > 0)
        {
            var frameEnd = new Tick();
            Tick calcDur(eMajorTraceType type)
            {
                var start = frameEnd;
                var end = frameEnd;
                foreach (var ev in active)
                {
                    if (ev.MajorType != type) continue;
                    if (ev.Phase == eEventPhase.Begin) start = ev.TimeStamp;
                    if (ev.Phase == eEventPhase.End) end = ev.TimeStamp;
                }
                return end - start;
            }
            var ul = calcDur(eMajorTraceType.Update);
            var rl = calcDur(eMajorTraceType.Render);
            var dl = calcDur(eMajorTraceType.Draw);
            string key = "";
            foreach (var ev in active)
                if (ev.MajorType == eMajorTraceType.KeyEvent)
                    key = GUtil.AppendPiece(key, "; ", ev.Key);
            _recentFrames.Push(new FrameInfo(tick, ul, rl, dl, key));
        }
    }
    // for main thread only!
    internal static void BeginBlock(eMajorTraceType ev) => _Log(ev.ToString(), ev, eEventPhase.Begin);
    internal static void EndBlock(eMajorTraceType type) => _Log("", type, eEventPhase.End);

    public static void BeginBlock(string key) => _Log(key, eMajorTraceType.Other, eEventPhase.Begin);
    public static void EndBlock() => _Log("", eMajorTraceType.Other, eEventPhase.End);
    internal static void _BeginAsyncBlock(string key, int jobId) => _Log(key, eMajorTraceType.Async, eEventPhase.Begin, jobId);
    internal static void _EndAsyncBlock(int jobId) => _Log("", eMajorTraceType.Async, eEventPhase.End, jobId);
    public static void LogText(string text) => _Log(text, eMajorTraceType.Other, eEventPhase.Event);
    public static void MarkKeyFrame(string text) => _Log(text, eMajorTraceType.KeyEvent, eEventPhase.Event);
    public static void SplitBlock(string key)
    {
        if (Enabled)
        {
            EndBlock();
            BeginBlock(key);
        }
    }
    static void _Log(string key, eMajorTraceType majorType, eEventPhase phase, int jobId = 0)
    {
        if (!Enabled) return;
        lock (_events)
        {
            _events.Push(new TraceEvent(_tick, key, majorType, phase, jobId));
        }
    }


    static List<TraceEvent> GetEventsForFrame(FrameInfo frame, bool incAsync)
        => GetEventsForFrame(frame.Frame, incAsync);
    static List<TraceEvent> GetEventsForFrame(int tick, bool incAsync)
    {
        // caution: events are in reverse-chronological order
        List<TraceEvent> active = new List<TraceEvent>(20);
        lock (_events)
        {
            if (tick < GetEarliestEventFrame()) return active;
            for (int i = 0; i < _events.Count; i++)
            {
                var ev = _events.Get(i);
                if (ev.Frame < tick - 1) break; // definitely too old
                if (ev.Frame != tick) continue; // not work considering
                if (!incAsync && ev.JobId != 0) continue;
                active.Add(ev);
            }
        }
        return active;
    }
    static int GetEarliestEventFrame()
    {
        lock (_events)
        {
            if (_events.Count == 0) return FulcrumGame._Frame + 1;
            return _events.Get(_events.Count - 1).Frame + 1;
        }
    }
}

internal struct FrameInfo
{
    public enum eProblemLevel { Normal, Warn, High, Extreme }

    public int Frame;
    public Tick UpdateLength;
    public Tick RenderLength;
    public Tick DrawLength;
    public eProblemLevel Abnormal;
    public string KeyFrame;
    public FrameInfo(int frame, Tick update, Tick render, Tick draw, string keyFrame)
    {
        Frame = frame;
        UpdateLength = update;
        RenderLength = render;
        DrawLength = draw;
        var dur = (update + render + draw).Milliseconds;
        if (dur > 25)
            Abnormal = eProblemLevel.Extreme;
        else if (dur > 14)
            Abnormal = eProblemLevel.High;
        else if (dur > 10)
            Abnormal = eProblemLevel.Warn;
        else
            Abnormal = eProblemLevel.Normal;
        KeyFrame = keyFrame;
    }
    public Color GetColor()
    {
        switch (Abnormal)
        {
            case eProblemLevel.Warn: return Color.Orange;
            case eProblemLevel.High: return Color.DeepPink;
            case eProblemLevel.Extreme: return Color.Red;
            default: return Color.White;
        }
    }
    public string GetToolTip()
    {
        return KeyFrame;
    }
}
internal struct TraceEvent
{
    public int Frame;
    public int JobId; // 0 for non-job
    public Tick TimeStamp;
    public GPerf.eMajorTraceType MajorType;
    public GPerf.eEventPhase Phase;
    public string Key;
    public TraceEvent(int frame, string key, GPerf.eMajorTraceType majorType, GPerf.eEventPhase phase, int jobId = 0)
    {
        Frame = frame;
        TimeStamp = new Tick();
        Key = key;
        Phase = phase;
        MajorType = majorType;
        JobId = jobId;
    }
    public string ToDisplayString()
        => $"{JobId} {MajorType} {Phase} {Key}";
    public override string ToString()
        => $"ev {ToDisplayString()}";
}