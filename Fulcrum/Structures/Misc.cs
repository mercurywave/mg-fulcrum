using System;

namespace Fulcrum;

public class ODisposable : IDisposable
{
    Action _clean;
    public ODisposable(Action startup, Action cleanup)
    {
        startup();
        _clean = cleanup;
    }

    public void Dispose()
    {
        _clean();
    }
}