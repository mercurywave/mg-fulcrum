using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

public interface IComponentContainer
{
    internal List<IComponent> _childComponents { get; set; }

    public void Register(IComponent component)
    {
        _childComponents.Add(component);
        component.ParentContainer = this;
    }
    public void Unregister(IComponent component)
    {
        component.ParentContainer = null;
        _childComponents.Remove(component);
    }

    public IEnumerable<IComponent> Children => _childComponents;

    public void WalkTree(Action<IComponent> action)
    {
        foreach (var child in Children.ToArray())
        {
            action(child);
            if (child is IComponentContainer container)
                container.WalkTree(action);
        }
    }
    public void WalkTree<T>(Action<T> action)
    {
        foreach (var child in Children.ToArray())
        {
            if(child is T tChild)
                action(tChild);
            if (child is IComponentContainer container)
                container.WalkTree(action);
        }
    }
}

public class IComponent
{
    public IComponentContainer ParentContainer;
}