using System.Diagnostics;

namespace Fulcrum;

public static class GLayoutEngine
{
    internal static bool Dirty = false;
    public static void Layout(IComponentContainer container)
    {
        Dirty = false;
        for (int i = 0; i < 50; i++)
        {
            container.WalkTree<ILayout>(layout => layout.OnLayout());
            if(!Dirty) return;
        }
        Debug.Assert(false, "Layout engine failed to converge after 50 iterations.");
    }
}