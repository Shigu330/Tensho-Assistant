using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.Inventory;

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
    private IReadOnlyList<InventoryWindowRect> above=[];
    /// <summary>0.535 (user request): the game windows drawn above the inventory, so that the sort button can leave when one covers it. HUD parts (« _… ») and the inventory's own grids are left out.</summary>
    public IReadOnlyList<InventoryWindowRect> Above{get{lock(gate)return above;}}
    private string? aboveLogged;

    public InventoryWindowReader(IFramework framework,IGameGui gui,Func<bool> enabled,IPluginLog log)
    {
        this.framework=framework;this.gui=gui;this.enabled=enabled;this.log=log;
        framework.Update+=OnUpdate;
    }

    private void OnUpdate(IFramework _)
    {
        if(disposed)return;
        InventoryWindowRect? found=null;
        IReadOnlyList<InventoryWindowRect> covers=[];
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
                    covers=WindowsAbove(unit);
                    break;
                }
            warned=false;
        }
        catch(Exception error)
        {
            found=null;covers=[];
            if(!warned){warned=true;log.Warning(error,"The inventory window could not be read.");}
        }
        lock(gate){rect=found;above=covers;}
    }

    // The game draws its layers in order, and in a layer its list in order: what comes after the inventory is above it.
    private List<InventoryWindowRect> WindowsAbove(AtkUnitBase* inventory)
    {
        var result=new List<InventoryWindowRect>();
        var manager=RaptureAtkUnitManager.Instance();
        if(manager==null)return result;
        var layers=manager->AtkUnitManager.DepthLayers;
        var passed=false;List<string>? names=null;
        for(var layer=0;layer<layers.Length;layer++)
        {
            ref var list=ref layers[layer];
            var count=Math.Min((int)list.Count,list.Entries.Length);
            for(var index=0;index<count;index++)
            {
                AtkUnitBase* unit=list.Entries[index].Value;
                if(unit==null)continue;
                if(unit==inventory){passed=true;continue;}
                if(!passed || !unit->IsVisible || unit->RootNode==null)continue;
                var name=unit->NameString;
                if(name.Length==0 || name[0]=='_' || name.StartsWith("Inventory",StringComparison.Ordinal))continue;
                result.Add(new(unit->X,unit->Y,unit->RootNode->Width*unit->Scale,unit->RootNode->Height*unit->Scale,unit->Scale));
                (names??=[]).Add(name);
            }
        }
        // Said when it changes, to see in the log which windows count.
        var said=names is null ? "":string.Join(",",names);
        if(said!=aboveLogged){aboveLogged=said;log.Debug("Windows above the inventory: {Names}",said);}
        return result;
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        framework.Update-=OnUpdate;
    }
}
