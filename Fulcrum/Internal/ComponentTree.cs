using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public class ComponentTree
{
    private TreeNode _root;
    Dictionary<IComponent, TreeNode> _nodes = new Dictionary<IComponent, TreeNode>();
    public ComponentTree(IComponentContainer rootComponent)
    {
        _root = new TreeNode(null, rootComponent);
        _nodes[rootComponent] = _root;
    }

    public void AddChild(IComponent parent, IComponent child)
    {
        Debug.Assert(parent is IComponentContainer, "Parent must be a component container");
        if (!_nodes.TryGetValue(parent, out var parentNode))
            throw new Exception("Parent not found in tree");
        if (_nodes.ContainsKey(child))
            throw new Exception("Child already exists in tree");
        var node = new TreeNode(parentNode, child);
        parentNode.Children.Add(node);
        _nodes[child] = node;
        ApplySpokes(child);
        if(parent is IComponentContainer container)
            container.OnAddChild(child);
    }
    public void AddTree(IComponent parent, ComponentTree tree)
    {
        if (!_nodes.TryGetValue(parent, out var parentNode))
            throw new Exception("Parent not found in tree");
        var node = new TreeNode(parentNode, tree.Root);
        parentNode.Children.Add(node);
        RegisterRecursive(tree._root);
    }
    void RegisterRecursive(TreeNode node) =>
        WalkTreeNode(node, c =>
        {
            _nodes.Add(c, node);
            c.Tree = this;
        });


    public void Remove(IComponent component)
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        foreach (var child in node.Children.ToList())
            Remove(child.Component);
        UnregisterRecursive(node);
        WalkTreeNode(node, c => RemoveSpokes(c));
        if (node.Parent != null)
            node.Parent.Children.Remove(node);
        _nodes.Remove(component);
        RemoveSpokes(component);
        if(node.Parent?.Component is IComponentContainer container)
            container.OnRemoveChild(component);
    }
    public ComponentTree Slice(IComponent component)
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        var newTree = new ComponentTree(component as IComponentContainer);
        UnregisterRecursive(node);
        return newTree;
    }
    void UnregisterRecursive(TreeNode node) =>
        WalkTreeNode(node, c =>
        {
            _nodes.Remove(c);
            c.Tree = null;
        });

    public IComponent Root => _root.Component;
    public IEnumerable<IComponent> GetChildren(IComponent component)
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        return node.Children.Select(n => n.Component);
    }

    public void WalkAllNodes(Action<IComponent> action) =>
        _nodes.Values.ToList().ForEach(node => action(node.Component));

    public void WalkAllNodes<T>(Action<T> action) =>
        _nodes.Values.ToList().ForEach(node =>
        {
            if (node.Component is T tComponent)
                action(tComponent);
        });

    public void WalkTree(IComponent component, Action<IComponent> action)
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        WalkTreeNode(node, action);
    }

    private void WalkTreeNode(TreeNode node, Action<IComponent> action)
    {
        action(node.Component);
        foreach (var child in node.Children.ToList())
        {
            WalkTreeNode(child, action);
        }
    }

    public void WalkTree<T>(Action<T> action) =>
        WalkTreeNode<T>(_root, action);
    public void WalkTree<T>(IComponent component, Action<T> action)
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        WalkTreeNode<T>(node, action);
    }

    private void WalkTreeNode<T>(TreeNode node, Action<T> action)
    {
        if (node.Component is T tComponent)
            action(tComponent);
        foreach (var child in node.Children.ToList())
        {
            WalkTreeNode<T>(child, action);
        }
    }

    public T ScanUpTree<T>(IComponent component) where T : IComponent
    {
        if (!_nodes.TryGetValue(component, out var node))
            throw new Exception("Component not found in tree");
        while (node != null)
        {
            if (node.Component is T tComponent)
                return tComponent;
            node = node.Parent;
        }
        throw new Exception($"No component of type {typeof(T).Name} found in tree");
    }

    #region Spokes
    static Dictionary<Type, SpokeInfo> _spokes = new Dictionary<Type, SpokeInfo>();
    internal static void RegisterSpoke(Type spoke)
    {
        var onAdd = spoke.GetMethod("SpokeOnAdd", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var onRemove = spoke.GetMethod("SpokeOnRemove", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        _spokes.Add(spoke, new SpokeInfo
        {
            OnAdd = i => onAdd?.Invoke(null, [Convert.ChangeType(i, spoke)]),
            OnRemove = i => onRemove?.Invoke(null, [Convert.ChangeType(i, spoke)])
        });
    }
    struct SpokeInfo
    {
        public Action<IComponent> OnAdd;
        public Action<IComponent> OnRemove;
    }
    public void ApplySpokes(IComponent component)
    {
        foreach(var (Key, Value) in _spokes)
        {
            if (Key.IsAssignableFrom(component.GetType()))
                Value.OnAdd?.Invoke(component);
        }
    }
    public void RemoveSpokes(IComponent component)
    {
        foreach(var (Key, Value) in _spokes)
        {
            if (Key.IsAssignableFrom(component.GetType()))
                Value.OnRemove?.Invoke(component);
        }
    }
    #endregion
}

class TreeNode
{
    public IComponent Component;
    public TreeNode Parent;
    public List<TreeNode> Children;
    public TreeNode(TreeNode parent, IComponent component)
    {
        Component = component;
        Parent = parent;
        Children = (component is IComponentContainer) ? new List<TreeNode>() : null;
    }
}