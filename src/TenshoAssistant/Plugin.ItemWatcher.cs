using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1941, GPT review of 11/11, accepted by the user): the discoveries' item watcher out of Plugin.cs, after the inventory's readers
// (0.1940). The watcher (where the items got come from, and the fish caught) is created in one install path and released in one release
// path of the plugin's registrations, in this file; Plugin.cs calls it where its block stood, so the order of the registrations is the one
// of before; the watcher is handed back (the constructor gives it its sources afterwards, as before). ItemWatcherFamily1941Tests counts
// those paths.
public sealed partial class Plugin
{
    /// <summary>Where the items got come from, and the fish caught (Discoveries/ItemDiscoveryWatcher). Returns the watcher.</summary>
    private Discoveries.ItemDiscoveryWatcher? RegisterItemWatcher(Configuration configuration,IDataManager data,IFramework framework,IClientState client,IGameGui gameGui,
        IObjectTable objects,ITargetManager targets,ICondition condition,IGameInventory inventory,Discoveries.DiscoveryStore discoveries,IChatGui chat)
    {
        // X-74 (0.860): where the items got come from, and the fish caught (Discoveries/ItemDiscoveryWatcher).
        HashSet<uint>? fishItems=null;var territoryDuties=new Dictionary<uint,uint>();
        Discoveries.ItemDiscoveryWatcher? itemWatcher=null;
        registrations.Register(()=>itemWatcher=new Discoveries.ItemDiscoveryWatcher(framework,client,gameGui,objects,targets,condition,inventory,discoveries,log,
            ()=>configuration.Discovery,()=>configuration.DeveloperMode && configuration.Discovery.DebugLog,
            territory=>territoryDuties.TryGetValue(territory,out var duty) ? duty:territoryDuties[territory]=data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>().GetRowOrDefault(territory)?.ContentFinderCondition.RowId ?? 0,
            item=>(fishItems??=data.GetExcelSheet<Lumina.Excel.Sheets.FishParameter>().Where(row=>row.Item.RowType==typeof(Lumina.Excel.Sheets.Item)).Select(row=>row.Item.RowId).ToHashSet()).Contains(item),
            item=>data.GetExcelSheet<Lumina.Excel.Sheets.Item>().GetRowOrDefault(item)?.ItemAction.RowId is > 0,chat),()=>itemWatcher?.Dispose());
        return itemWatcher;
    }
}
