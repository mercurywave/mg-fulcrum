using System;

namespace Fulcrum;
public static class GError
{
    public static void RaiseError(Exception e)
    {
        Console.WriteLine("Error: " + e.Message);
        Console.WriteLine("Error: " + e.StackTrace);
        throw e;
    }
}