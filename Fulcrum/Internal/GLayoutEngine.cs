using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public static class GLayoutEngine
{
    internal static bool Dirty = false;
    public static void Layout(ComponentTree container)
    {
        for (int i = 0; i < 50; i++)
        {
            Dirty = false;
            container.WalkAllNodes<ILayout>(layout => layout.Layout.ClearBinds());
            container.WalkTree<ILayout>(layout => layout.OnLayout());
            if (!Dirty) return;
        }
        Debug.Assert(false, "Layout engine failed to converge after 50 iterations.");
    }
    
    // transform is from screen space to real local space of the component
    static void WalkUiTransform(Action<IComponent, Vector2> action, TreeNode node, Vector2 transform)
    {
        action(node.Component, transform);
        var childTransform = transform;
        if(node.Component is ILayout layout)
            childTransform = layout.Layout.TransformedLayout.TopLeft;
        if(node.Component is IViewport view)
            childTransform -= view.Viewport.TransformedLayout.TopLeft;
        foreach (var child in node.Children)
            WalkUiTransform(action, child, childTransform);
    }
}