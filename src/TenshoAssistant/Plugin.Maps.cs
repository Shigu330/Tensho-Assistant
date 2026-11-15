using Dalamud.Plugin.Services;

namespace TenshoAssistant;

// X-153 (0.1404, seventh slice, GPT review of 11/11, accepted by the user): the map family out of Plugin.cs, after the desktop, persistence,
// server bar, panels, discoveries and company voyages families. The live map reader, the map positions' loader (its index) and the game's
// map markers warmed off the frame: each reader created in one install path and released in one release path of the plugin's registrations,
// all in this file, in the order of before; Plugin.cs calls the family once, at the same place. MapsFamily1404Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The live map reader, asked later by other readers (item hover, discoveries): null until installed or once released.</summary>
    private Maps.MapLiveReader? mapLive;

    /// <summary>The map family: live reader, positions' index and loader, game markers (warmed off the frame, stopped with the plugin); the map view configured with them.</summary>
    private void RegisterMaps(IFramework framework,IClientState client,IObjectTable objects,IDataManager data,ICondition condition,IFateTable fateTable,IPartyList partyList)
    {
        registrations.Register(()=>mapLive=new Maps.MapLiveReader(framework,client,objects,data,condition,fateTable,partyList,log),()=>mapLive?.Dispose());
        var mapLocal=new Maps.MapLocalIndexService();
        Maps.MapLocalIndexLoader? mapLocalLoader=null;
        registrations.Register(()=>mapLocalLoader=new Maps.MapLocalIndexLoader(mapLocal,data,framework,log,dataCache),()=>mapLocalLoader?.Dispose());
        var mapMarkers=new Maps.MapMarkerSource(data,log);
        mainWindow.ConfigureMapView(mapLive!,mapMarkers,mapLocal);
        // X-01 C5 (0.1206): the names of every map and the aetheryte rows built off the frame once the loading's own work is done, so the
        // first region map only reads them (it took 1 311 ms on the game's thread); stopped with the plugin.
        _=Task.Run(async()=>
        {
            try{await Task.Delay(MapMarkersWarmDelay,gameDataCancel.Token).ConfigureAwait(false);mapMarkers.Warm(gameDataCancel.Token);}
            catch(OperationCanceledException){}
        });
    }
}
