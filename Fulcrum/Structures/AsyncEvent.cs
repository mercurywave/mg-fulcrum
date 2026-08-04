
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fulcrum;

//assumes one consumer, many writers from various places
// allows raising points to queue up events in order
public class AsyncEventListener<T>
{
    TaskCompletionSource<bool> _wait = null;
    Queue<T> _events = new Queue<T>();

    public async Task<T> WaitForEvent()
    {
        int count;
        lock (_events)
        {
            count = _events.Count;
            if (count == 0)
                _wait = new TaskCompletionSource<bool>();
        }
        if (count == 0) await _wait.Task;
        lock (_events)
        {
            T temp = _events.Dequeue();
            return temp;
        }
    }

    public void RaiseEvent(T e)
    {
        lock (_events)
        {
            int count = _events.Count;
            _events.Enqueue(e);
            if (count == 0 && _wait != null)
                _wait.TrySetResult(false);
        }
    }

    //avoid double events and such
    public void FlushAll()
    {
        lock (_events)
        {
            _events.Clear();
        }
    }
}

//assumes one listener, any number of triggers. Newer triggers overwrite any previous triggers
// so like a simple event loop that can ignore other interactions once a user picks one
// or a button that raises an event that someone may or may not be waiting for
public class AsyncEvent<T>
{
    TaskCompletionSource<T> _wait = null;
    T _last;
    Tick _lastWaiting = Tick.Zero;
    object _lock = new object();

    // debounce is the number of ticks to allow old events to count
    public async Task<T> WaitForEvent(bool clearPreceeding = true, int debounce = 15)
    {
        lock (_lock)
        {
            if (clearPreceeding) _lastWaiting = GCore.FrameStart - Tick.Ms(debounce) - Tick.Ms(1);
            if (_lastWaiting + Tick.Ms(debounce) > GCore.FrameStart)
            {
                _lastWaiting = Tick.Zero;
                return _last;
            }
            _wait = new TaskCompletionSource<T>();
        }
        T result = await _wait.Task;
        lock (_lock)
        {
            _wait = null;
            return result;
        }
    }

    public bool TryGetEvent(ref T ev, int debounce = 15)
    {
        lock (_lock)
        {
            if (_lastWaiting + Tick.Ms(debounce) < GCore.FrameStart)
                return false;
            ev = _last;
            _lastWaiting = Tick.Zero;
        }
        return true;
    }

    // if you have an event queued up, kill it
    public void ClearEvent()
    {
        lock (_lock)
            _lastWaiting = Tick.Zero;
    }

    public void RaiseEvent(T e)
    {
        lock (_lock)
        {
            if (_wait != null)
                _wait.TrySetResult(e);
            else
            {
                _last = e;
                _lastWaiting = GCore.FrameStart;
            }
        }
    }

    // cancel any pending events and put in a new one
    public void OverwriteEvent(T e)
    {
        ClearEvent();
        RaiseEvent(e);
    }
}

//meant for like a button that can constantly raise an event and an async listener may or may not be there
public class AsyncEvent
{
    TaskCompletionSource<bool> _wait = null;
    Tick _lastWaiting = Tick.Zero;
    object _lock = new object();

    // debounce is the number of ticks to allow old events to count
    public async Task WaitForEvent(bool clearPreceeding = true, int debounceMs = 250)
    {
        lock (_lock)
        {
            if (clearPreceeding) _lastWaiting = GCore.FrameStart - Tick.Ms(debounceMs) - Tick.Ms(1);
            if (_lastWaiting + Tick.Ms(debounceMs) > GCore.FrameStart)
            {
                _lastWaiting = Tick.Zero;
                return;
            }
            _wait = new TaskCompletionSource<bool>();
        }
        var result = await _wait.Task;
        lock (_lock)
        {
            _wait = null;
            return;
        }
    }

    public void RaiseEvent()
    {
        lock (_lock)
        {
            if (_wait != null)
                _wait.TrySetResult(true);
            else
            {
                _lastWaiting = GCore.FrameStart;
            }
        }
    }
}