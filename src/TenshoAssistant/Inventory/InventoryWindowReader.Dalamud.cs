using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Inventory;

/// <summary>Screen rectangle of the game's inventory window (whichever of its three layouts is shown).</summary>
public readonly record struct InventoryWindowRect(float X,float Y,float Width,float Height,float Scale=1);

/// <summary>
/// 0.388 (user request: a sort button on the inventory window): every frame on the framework thread, while the button is on, copies the
/// rectangle of the visible inventory window (« InventoryExpansion », « InventoryLarge » or « Inventory »); null when none is shown or the
/// game UI is hidden. Native pointers never reach Draw.
/// </summary>
public sealed unsafe class InventoryWindowReader : IDisposable
{
    private static readonly string[] Addons=["InventoryExpansion","InventoryLarge","Inventory"];
    private readonly IFramework framework;
    private readonly IGameGui gui;
    private readonly Func<bool> enabled;
    private readonly IPluginLog log;
    private readonly object gate=new();
    private InventoryWindowRect? rect;
    private bool disposed,warned;
    public InventoryWindowRect? Rect{get{lock(gate)return rect;}}

    public InventoryWindowReader(IFramework framework,IGameGui gui,Func<bool> enabled,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.enabled=enabled;this.log=log;
        framework.Update+=OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        InventoryWindowRect? found=null;
        try
        {
            if(enabled() && !gui.GameUiHidden)
                foreach(var name in Addons)
                {
                    var handle=gui.GetAddonByName(name);
                    if(handle.IsNull || !handle.IsReady || !handle.IsVisible)continue;
                    var unit=(AtkUnitBase*)handle.Address;
                    if(unit->RootNode==null)continue;
                    found=new(unit->X,unit->Y,unit->RootNode->Width*unit->Scale,unit->RootNode->Height*unit->Scale,unit->Scale);
                    break;
                }
            warned=false;
        }
        catch(Exception error)
        {
            found=null;
            if(!warned){warned=true;log.Warning(error,"The inventory window could not be read.");}
        }
        lock(gate)rect=found;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
