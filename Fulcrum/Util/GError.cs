using System;

namespace Fulcrum;
public static class GError
{
    public static void RaiseError(Exception e)
    {
        throw e;
    }
}