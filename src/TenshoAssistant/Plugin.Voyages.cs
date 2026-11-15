using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1403, sixth slice, GPT review of 11/11, accepted by the user): the company voyages family out of Plugin.cs, after the desktop
// (0.1225), persistence (0.1239), server bar (0.1240), panels (0.1249) and discoveries (0.1250) families. The voyage watcher (submersibles
// and airships, their loot for the discoveries base) with its loot history, the airship voyage table read from the game's functions and the
// developer probe of the submarine functions: each created in one install path and released in one release path of the plugin's registrations,
// all in this file, in the order of before; Plugin.cs calls the family once, at the same place. VoyagesFamily1403Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The company voyages family: voyage watcher and history, airship voyage table, submarine functions probe; and what the main window reads from them.</summary>
    private void RegisterVoyages(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IClientState client,
        TenshoDataFile tenshoData,Action saveVoyages)
    {
        FreeCompany.VoyageWatcher? voyages=null;
        // g, M-10 (0.379): voyage loot history, 180 days; since 0.1148 (X-173) in TenshoData.db, the file of before (VoyageHistory.json)
        // converted at its first load and kept aside.
        var voyageHistory=new FreeCompany.VoyageHistoryStore(Path.Combine(pluginInterface.GetPluginConfigDirectory(),"VoyageHistory.json"),(error,message)=>log.Warning(error,message),tenshoData);
        registrations.Register(()=>voyages=new FreeCompany.VoyageWatcher(framework,data,configuration,saveVoyages,log){Observed=voyageHistory.Observe,
            // X-73.8 (0.862): each submersible voyage's loot by sector and the sectors it unlocked, for the discoveries base.
            AirshipLoot=(item,sector,count)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.AirshipDrop,item%1_000_000,sector,count,DateTimeOffset.UtcNow,hq:item>=1_000_000);},
            SectorLoot=(item,sector,count,hq)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageDrop,item,sector,count,DateTimeOffset.UtcNow,hq:hq);},
            SectorVisit=(sector,twice,surveillance)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageVisit,sector,twice ? 1u:0u,surveillance,DateTimeOffset.UtcNow);},
            SectorUnlock=(unlocked,sector)=>{if(configuration.Discovery.Enabled && configuration.Discovery.ItemSources)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.VoyageUnlock,unlocked,sector,1,DateTimeOffset.UtcNow);}},()=>voyages?.Dispose());
        mainWindow.VoyageHistorySource=voyageHistory;
        mainWindow.SubmarineCatalog=()=>voyages?.Catalog;
        mainWindow.VesselParts=()=>voyages?.PartCatalog;
        mainWindow.SubmarineCatalogFailed=()=>voyages?.CatalogFailed==true;
        mainWindow.VesselPartsFailed=()=>voyages?.PartCatalogFailed==true;
        mainWindow.AirshipCatalog=()=>voyages?.AirshipCatalog;
        mainWindow.AirshipCatalogFailed=()=>voyages?.AirshipCatalogFailed==true;
        // 0.337 (DEC-023 3): airship leg times and distances from the game's own functions, on the framework thread, on request.
        FreeCompany.AirshipVoyageTable? airshipTable=null;
        registrations.Register(()=>airshipTable=new FreeCompany.AirshipVoyageTable(framework,client,log),()=>airshipTable?.Dispose());
        mainWindow.AirshipTable=()=>airshipTable?.State;
        // DEC-043 (0.1006): developer-mode probe of the game's submarine functions against the formula, once per session (log only).
        FreeCompany.SubmarineVoyageProbe? submarineProbe=null;
        registrations.Register(()=>submarineProbe=new FreeCompany.SubmarineVoyageProbe(framework,client,data,log,()=>configuration.DeveloperMode),()=>submarineProbe?.Dispose());
        mainWindow.RequestAirshipTable=(speed,points)=>airshipTable?.Request(speed,points);
        // X-201 (0.1248): the other speeds (one per registered airship) and their tables, for the fleet list.
        mainWindow.RequestAirshipSpeeds=speeds=>airshipTable?.RequestAlso(speeds);
        mainWindow.AirshipTableFor=speed=>airshipTable?.TableFor(speed);
        mainWindow.VoyageReadUnavailable=()=>voyages?.ReadUnavailable==true;
    }
}
