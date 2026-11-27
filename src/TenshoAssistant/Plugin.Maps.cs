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

    // X-153 (0.1900): the rest of the map family, each block moved as it was and called where it stood (MapsFamily1900Tests).

    /// <summary>The game's own map replaced by Tensho's when the player asks it (map settings), and opened again on request.</summary>
    private void RegisterGameMap(Configuration configuration,IFramework framework,IGameGui gameGui,IClientState client,ICondition condition)
    {
        Maps.NativeMapReplacer? mapReplacer=null;
        registrations.Register(()=>mapReplacer=new Maps.NativeMapReplacer(framework,gameGui,client,condition,()=>configuration.MapView,mainWindow.ShowMapFromGame,()=>mainWindow.MapViewOpen,()=>mainWindow.MapViewVisible(),mainWindow.CloseMapFromGame,log),()=>mapReplacer?.Dispose());
        mainWindow.ConfigureGameMapOpener(()=>mapReplacer?.OpenGameMap());
    }

    /// <summary>X-747 (0.1772): the furniture placed in a house and its event objects, in the log (developer mode only), before any code.</summary>
    private void RegisterHousingProbe(Configuration configuration,IFramework framework,IClientState client,IObjectTable objects)
    {
        Maps.HousingFurnitureProbe? housingProbe=null;
        registrations.Register(()=>housingProbe=new Maps.HousingFurnitureProbe(framework,client,objects,log,()=>configuration.DeveloperMode),()=>housingProbe?.Dispose());
    }

    /// <summary>g, M-14 (0.384): the deciphered treasure map's spot (game functions checked, TreasureSpot → Level). Returns the reader,
    /// which the discoveries' item watcher reads.</summary>
    private Maps.TreasureMapReader? RegisterTreasureMap(IFramework framework,IClientState client,IDataManager data)
    {
        Maps.TreasureMapReader? treasureMap=null;
        registrations.Register(()=>treasureMap=new Maps.TreasureMapReader(framework,client,data,log),()=>treasureMap?.Dispose());
        mainWindow.TreasureMap=()=>treasureMap?.Spot;
        return treasureMap;
    }
}
