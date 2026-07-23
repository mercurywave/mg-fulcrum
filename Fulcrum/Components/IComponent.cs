using System;
using System.Collections.Generic;
using System.Linq;

namespace Fulcrum;

public interface IComponentContainer : IComponent
{
    public void OnAddChild(IComponent component) { }
    public void OnRemoveChild(IComponent component) { }
    public bool PauseChildUpdates() => false;
    public bool PauseChildDraws() => false;
}

public interface IComponent
{
    public ComponentTree Tree { get; set; }
}

public class BaseComponent : IComponent
{
    public ComponentTree Tree { get; set; }
}

public interface IUpdate : IComponent
{
    public void OnUpdate(Tick frameTime);
}

public interface IDraw : IComponent
{
    public void OnRender(Tick frameTime) { }
    // Before any controls are drawn
    public void OnPreDraw(Tick frameTime) { }
    // Draw occurs before children are drawn
    public void OnDraw(Tick frameTime);
    // After all normal draws
    public void OnPostDraw(Tick frameTime) { }
}