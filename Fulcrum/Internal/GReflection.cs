using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fulcrum;

//Debugging tips:
// make sure every static asset is public - this can't access private assets
// Game is responsible for ensuring game data is loaded - use load.Preload load.MakeDynamicBundle

public static class GReflection
{
    static List<Assembly> _assemblies = new List<Assembly>(); // to prevent duplicates

    internal static void Scan()
    {
        Scan(typeof(GReflection).GetTypeInfo().Assembly);
    }
    public static void Scan(Assembly assembly)
    {
        using (var log = GPerf.GetAsyncLogger("GReflection Load - " + assembly.FullName))
        {
            if (!_assemblies.Contains(assembly))
            {
                _assemblies.Add(assembly);

                var types = assembly.GetTypes();
                foreach (var t in types)
                    ScanType(t);
            }
        }
    }

    static void ScanType(Type t)
    {
        var name = t.Name;
        var autoInit = t.GetTypeInfo().GetCustomAttribute<AutoInitialize>();

        if (autoInit != null)
        {
            // TODO: for asset loading
            // // methods are individually flagged for execution
            // foreach (var meth in t.GetMethods().Where(f => f.IsStatic))
            // {
            //     var attr = meth.GetCustomAttribute<AutoInitialize>();
            //     if (attr != null)
            //     {
            //         load.Queue(new ActionAsset(() => meth.Invoke(null, null)), attr);
            //     }
            //     if (Core.DebuggerAttached)
            //     {
            //         var dact = meth.GetCustomAttribute<DebugAction>();
            //         if (dact != null)
            //             DebugMenu.Register(new DebugMenu.Button(dact.Name, () => meth.Invoke(null, null)));

            //         var dtog = meth.GetCustomAttribute<DebugToggle>();
            //         if (dtog != null)
            //             DebugMenu.Register(new DebugMenu.Toggle(dtog.Name, b => meth.Invoke(null, new object[] { b }), dtog.InitialState));
            //     }
            // }

            // // static assets are individually flagged, or use default
            // foreach (var f in t.GetFields().Where(f => f.IsStatic))
            // {
            //     if (typeof(IAsset).IsAssignableFrom(f.FieldType))
            //     {
            //         var fAttr = f.GetCustomAttribute<AutoInitialize>();
            //         var useAttr = GetHierarchicalAttribute(autoInit, fAttr);

            //         if (useAttr != null)
            //         {
            //             var ass = f.GetValue(null) as IAsset;
            //             if (ass != null) // presumably a placeholder to use later?
            //                 load.Queue(ass, useAttr);
            //         }
            //     }
            // }
        }


        var autoSpoke = t.GetTypeInfo().GetCustomAttribute<Spoke>();
        if(autoSpoke != null)
        {
            ComponentTree.RegisterSpoke(t);
        }
    }

    //helper - individual asset overwrites base class method
    static AutoInitialize GetHierarchicalAttribute(AutoInitialize autoInit, AutoInitialize itemAttr)
    {
        if (itemAttr != null) return itemAttr;
        if (autoInit != null) return autoInit;
        return null;
    }
}

//add to a public static function to run function at stage
//add to a public static asset to load at stage
//add to a class to load all public static assets
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method)]
public class AutoInitialize : Attribute
{
    public enum eLoadBy { Launch, Menu, Game, Key };
    public eLoadBy LoadBy;
    public string Key = "";
    public int Priority = 1;

    public AutoInitialize(eLoadBy loadBy = eLoadBy.Menu, string key = "", int priority = 1)
    {
        LoadBy = loadBy;
        Key = key;
        Priority = priority;
    }
    public AutoInitialize(eLoadBy loadBy, int priority) : this(loadBy, "", priority) { }
}

// register a component spoke
[AttributeUsage(AttributeTargets.Interface)]
public class Spoke : Attribute
{
    // Spokes are expected to have a static method with signature: 
    // public static void SpokeOnAdd(SpokeTypeName component) {}
    // public static void SpokeOnRemove(SpokeTypeName component) {}
    public Spoke() { }
}

// NOTE: not loaded if debug is disabled at launch

// allows a static method to be called via checkbox in debug menu, passing state
[AttributeUsage(AttributeTargets.Method)]
public class DebugToggle : Attribute
{
    public string Name;
    public bool InitialState;
    public DebugToggle(string name, bool initialState = false)
    {
        Name = name;
        InitialState = initialState;
    }
}

// register a static method to be called from button in debug menu
[AttributeUsage(AttributeTargets.Method)]
public class DebugAction : Attribute
{
    public string Name;
    public DebugAction(string name)
    {
        Name = name;
    }
}