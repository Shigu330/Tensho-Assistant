using System.Diagnostics;
using Dalamud.Game.Inventory;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using TenshoAssistant.UI;

namespace TenshoAssistant.Inventory;

/// <summary>X-54 (0.820): one cell of a storage window that shows a wanted icon, in screen pixels.</summary>
public readonly record struct StorageCellRect(uint Icon,float X,float Y,float Width,float Height);

/// <summary>
/// X-54 (0.820, user request: when a storage other than the bags is open — retainer, chocobo saddlebag, company chest — mark the items the crafting and workshop
/// lists need). The game's storage windows show their items as icon components; the cells whose icon is that of a wanted item are found (bounded walk, framework
/// thread, five times a second while wanted). Window names as Tensho's bank capture reads them. Icons, not items: two items sharing an icon both match.
/// </summary>
public sealed unsafe class StorageHighlightReader : IDisposable
{
    public static readonly string[] Windows=["InventoryRetainerLarge","InventoryRetainer","RetainerGrid0","RetainerGrid1","RetainerGrid2","RetainerGrid3","RetainerGrid4",
        "InventoryBuddy","InventoryBuddy2","FreeCompanyChest"];
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IPluginLog log;private readonly Func<bool> developer;private readonly IGameInventory inventory;
    private IReadOnlySet<uint> wanted=new HashSet<uint>();private Func<uint,uint> iconOf=_=>0;private long wantedUntil,nextRead;
    // 0.839 (X-66): the containers each window shows, read to tell apart items that share an icon.
    private static readonly GameInventoryType[] RetainerPages=[GameInventoryType.RetainerPage1,GameInventoryType.RetainerPage2,GameInventoryType.RetainerPage3,
        GameInventoryType.RetainerPage4,GameInventoryType.RetainerPage5,GameInventoryType.RetainerPage6,GameInventoryType.RetainerPage7];
    private static readonly GameInventoryType[] SaddleBags=[GameInventoryType.SaddleBag1,GameInventoryType.SaddleBag2,GameInventoryType.PremiumSaddleBag1,GameInventoryType.PremiumSaddleBag2];
    private static readonly GameInventoryType[] CompanyPages=[GameInventoryType.FreeCompanyPage1,GameInventoryType.FreeCompanyPage2,GameInventoryType.FreeCompanyPage3,
        GameInventoryType.FreeCompanyPage4,GameInventoryType.FreeCompanyPage5];
    private static GameInventoryType[] ContainersOf(string window)=>window.StartsWith("InventoryBuddy",StringComparison.Ordinal) ? SaddleBags
        :window=="FreeCompanyChest" ? CompanyPages:RetainerPages;
    private IReadOnlyList<StorageCellRect> cells=[];private bool disposed,warned;private string logged="";

    public StorageHighlightReader(IFramework framework,IGameGui gui,IPluginLog log,Func<bool> developer,IGameInventory inventory)
    {this.framework=framework;this.gui=gui;this.log=log;this.developer=developer;this.inventory=inventory;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    /// <summary>The items to find, for the next half second; 0.839: items, no longer icons, with the icon of any item (0: unknown).</summary>
    public void Want(IReadOnlySet<uint> items,Func<uint,uint> icons){Volatile.Write(ref iconOf,icons);Volatile.Write(ref wanted,items);Volatile.Write(ref wantedUntil,Stopwatch.GetTimestamp()+Stopwatch.Frequency/2);}
    public IReadOnlyList<StorageCellRect> Cells=>Volatile.Read(ref cells);

    private void Update(IFramework unused)
    {
        if(disposed)return;
        var now=Stopwatch.GetTimestamp();
        if(now<nextRead)return;
        nextRead=now+Stopwatch.Frequency/5;
        var items=Volatile.Read(ref wanted);var icon=Volatile.Read(ref iconOf);
        if(now>Volatile.Read(ref wantedUntil) || items.Count==0){Volatile.Write(ref cells,Array.Empty<StorageCellRect>());return;}
        var icons=new HashSet<uint>();
        foreach(var item in items)if(icon(item) is var shown and not 0){icons.Add(shown);icons.Add(1_000_000+shown);}
        try
        {
            var found=new List<StorageCellRect>();var seen=new List<string>();
            foreach(var name in Windows)
            {
                var handle=gui.GetAddonByName(name);
                if(handle.IsNull || !handle.IsVisible)continue;
                var unit=(AtkUnitBase*)handle.Address;
                var shown=new List<(uint Icon,nint Node,int? Quantity)>();
                FindIcons(&unit->UldManager,0,shown);
                var stacks=Stacks(name,items,icon);var unsure=0;
                seen.Add($"{name} {shown.Count} cells, {stacks.Count} stacks");
                foreach(var (cellIcon,node,quantity) in shown)
                    if(icons.Contains(cellIcon) && found.Count<200)
                    {
                        if(!StorageCellMatch.Marks(cellIcon,quantity,stacks,icons)){unsure++;continue;}
                        var cell=(AtkResNode*)node;
                        found.Add(new(cellIcon,cell->ScreenX,cell->ScreenY,cell->Width*unit->Scale,cell->Height*unit->Scale));
                    }
                if(unsure>0)seen.Add($"{unsure} cells of a wanted icon left out");
            }
            Volatile.Write(ref cells,found.ToArray());
            var line=string.Join(", ",seen)+$"; {found.Count} cells marked";
            if(developer() && seen.Count>0 && line!=logged){logged=line;log.Information("Storage highlight: {0}.",line);}
        }
        catch(Exception error){Volatile.Write(ref cells,Array.Empty<StorageCellRect>());if(!warned){warned=true;log.Warning(error,"The storage windows could not be read for the highlight.");}}
    }

    /// <summary>0.839 (X-66): the stacks of the containers the window shows (framework thread), with the icon each shows; empty when not read.</summary>
    private List<StorageCellMatch.Stack> Stacks(string window,IReadOnlySet<uint> items,Func<uint,uint> icon)
    {
        var stacks=new List<StorageCellMatch.Stack>();
        foreach(var type in ContainersOf(window))
            foreach(var slot in inventory.GetInventoryItems(type))
            {
                if(slot.IsEmpty || slot.Quantity<=0 || slot.BaseItemId==0 || stacks.Count>=1000)continue;
                if(icon(slot.BaseItemId) is var shown and not 0)stacks.Add(new((slot.IsHq ? 1_000_000u:0u)+shown,slot.Quantity,items.Contains(slot.BaseItemId)));
            }
        return stacks;
    }

    /// <summary>0.839 (X-66, user report: the frame stayed when another bag tab was chosen): the node and every parent shown; the tabs hide a parent, not the cells.</summary>
    private static bool Shown(AtkResNode* node)
    {
        for(var depth=0;node!=null && depth<32;depth++,node=node->ParentNode)if(!node->IsVisible())return false;
        return true;
    }

    private static void FindIcons(AtkUldManager* manager,int depth,List<(uint Icon,nint Node,int? Quantity)> found)
    {
        if(depth>4 || !UiNodes.Walkable(manager) || manager->NodeListCount>1024 || found.Count>=400)return;
        for(var index=0;index<manager->NodeListCount;index++)
        {
            var node=manager->NodeList[index];
            if(node==null || (ushort)node->Type<1000 || !node->IsVisible())continue;
            var component=((AtkComponentNode*)node)->Component;
            if(component==null)continue;
            // 0.839 (X-66): an empty cell keeps its last icon with its image hidden; the cell, its image and their parents must be shown; the quantity it shows.
            if(component->GetComponentType()==ComponentType.Icon && ((AtkComponentIcon*)component)->IconId is var icon and not 0)
            {
                var cell=(AtkComponentIcon*)component;
                if(cell->IconImage!=null && cell->IconImage->IsVisible() && Shown(node))
                    found.Add(((uint)icon,(nint)node,cell->QuantityText!=null && cell->QuantityText->IsVisible() ? StorageCellMatch.Quantity(cell->QuantityText->NodeText.ToString()):null));
            }
            FindIcons(&component->UldManager,depth+1,found);
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
    }
}
