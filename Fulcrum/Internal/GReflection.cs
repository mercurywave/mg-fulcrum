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
            // methods are individually flagged for execution
            foreach (var meth in t.GetMethods().Where(f => f.IsStatic))
            {
                var attr = meth.GetCustomAttribute<AutoInitialize>();
                var useAttr = GetHierarchicalAttribute(autoInit, attr);
                if (useAttr != null)
                {
                    GLoad.Queue(new ActionAsset(() => meth.Invoke(null, null)), useAttr);
                }
                if (GCore.CanUseDebug)
                {
                    // var dact = meth.GetCustomAttribute<DebugAction>();
                    // if (dact != null)
                    //     DebugMenu.Register(new DebugMenu.Button(dact.Name, () => meth.Invoke(null, null)));

                    // var dtog = meth.GetCustomAttribute<DebugToggle>();
                    // if (dtog != null)
                    //     DebugMenu.Register(new DebugMenu.Toggle(dtog.Name, b => meth.Invoke(null, new object[] { b }), dtog.InitialState));
                }
            }

            // static assets are individually flagged, or use default
            foreach (var f in t.GetFields().Where(f => f.IsStatic))
            {
                if (typeof(IAsset).IsAssignableFrom(f.FieldType))
                {
                    var fAttr = f.GetCustomAttribute<AutoInitialize>();
                    var useAttr = GetHierarchicalAttribute(autoInit, fAttr);

                    if (useAttr != null)
                    {
                        var ass = f.GetValue(null) as IAsset;
                        if(useAttr.Location != eAssetLocation.Unspecified)
                            ass.Location = useAttr.Location;
                        if (ass != null) // presumably a placeholder to use later?
                            GLoad.Queue(ass, useAttr);
                    }
                }
            }
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
        if (itemAttr == null) return autoInit;
        if (autoInit == null) return itemAttr;
        var loadBy = itemAttr.LoadBy > autoInit.LoadBy ? itemAttr.LoadBy : autoInit.LoadBy;
        var isContent = (itemAttr.Location != eAssetLocation.Unspecified) ? itemAttr.Location : autoInit.Location;
        var key = string.IsNullOrEmpty(itemAttr.Key) ? autoInit.Key : itemAttr.Key;
        var priority = Math.Max(itemAttr.Priority, autoInit.Priority);
        return new AutoInitialize(loadBy, isContent, key, priority);
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
    public eAssetLocation Location = eAssetLocation.Unspecified;

    public AutoInitialize(eLoadBy loadBy = eLoadBy.Menu, eAssetLocation location = eAssetLocation.Unspecified, string key = "", int priority = 1)
    {
        LoadBy = loadBy;
        Location = location;
        Key = key;
        Priority = priority;
    }
    public AutoInitialize(eLoadBy loadBy, eAssetLocation location, int priority) : this(loadBy, location, "", priority) { }
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