using System.Diagnostics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

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
    private readonly IFramework framework;private readonly IGameGui gui;private readonly IPluginLog log;private readonly Func<bool> developer;
    private IReadOnlySet<uint> wanted=new HashSet<uint>();private long wantedUntil,nextRead;
    private IReadOnlyList<StorageCellRect> cells=[];private bool disposed,warned;private string logged="";

    public StorageHighlightReader(IFramework framework,IGameGui gui,IPluginLog log,Func<bool> developer)
    {this.framework=framework;this.gui=gui;this.log=log;this.developer=developer;TenshoAssistant.Competitive.CompetitiveGate.Add(framework,Update);}

    /// <summary>The icons to find (item icons, HQ ones too), for the next half second.</summary>
    public void Want(IReadOnlySet<uint> icons){Volatile.Write(ref wanted,icons);Volatile.Write(ref wantedUntil,Stopwatch.GetTimestamp()+Stopwatch.Frequency/2);}
    public IReadOnlyList<StorageCellRect> Cells=>Volatile.Read(ref cells);

    private void Update(IFramework unused)
    {
        if(disposed)return;
        var now=Stopwatch.GetTimestamp();
        if(now<nextRead)return;
        nextRead=now+Stopwatch.Frequency/5;
        var icons=Volatile.Read(ref wanted);
        if(now>Volatile.Read(ref wantedUntil) || icons.Count==0){Volatile.Write(ref cells,Array.Empty<StorageCellRect>());return;}
        try
        {
            var found=new List<StorageCellRect>();var seen=new List<string>();
            foreach(var name in Windows)
            {
                var handle=gui.GetAddonByName(name);
                if(handle.IsNull || !handle.IsVisible)continue;
                var unit=(AtkUnitBase*)handle.Address;
                var shown=new List<(uint Icon,nint Node)>();
                FindIcons(&unit->UldManager,0,shown);
                seen.Add($"{name} {shown.Count}");
                foreach(var (icon,node) in shown)
                    if(icons.Contains(icon) && found.Count<200)
                    {
                        var cell=(AtkResNode*)node;
                        found.Add(new(icon,cell->ScreenX,cell->ScreenY,cell->Width*unit->Scale,cell->Height*unit->Scale));
                    }
            }
            Volatile.Write(ref cells,found.ToArray());
            var line=string.Join(", ",seen)+$"; {found.Count} cells marked";
            if(developer() && seen.Count>0 && line!=logged){logged=line;log.Information("Storage highlight: {0}.",line);}
        }
        catch(Exception error){Volatile.Write(ref cells,Array.Empty<StorageCellRect>());if(!warned){warned=true;log.Warning(error,"The storage windows could not be read for the highlight.");}}
    }

    private static void FindIcons(AtkUldManager* manager,int depth,List<(uint Icon,nint Node)> found)
    {
        if(manager==null || depth>4 || manager->NodeList==null || manager->NodeListCount>1024 || found.Count>=400)return;
        for(var index=0;index<manager->NodeListCount;index++)
        {
            var node=manager->NodeList[index];
            if(node==null || (ushort)node->Type<1000 || !node->IsVisible())continue;
            var component=((AtkComponentNode*)node)->Component;
            if(component==null)continue;
            if(component->GetComponentType()==ComponentType.Icon && ((AtkComponentIcon*)component)->IconId is var icon and not 0)found.Add(((uint)icon,(nint)node));
            FindIcons(&component->UldManager,depth+1,found);
        }
    }

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,Update);
    }
}
