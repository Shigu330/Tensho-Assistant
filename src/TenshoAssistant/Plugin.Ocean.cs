using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Gathering;

namespace TenshoAssistant;

// X-153 (0.1417, ninth slice, GPT review of 11/11, accepted by the user): the ocean fishing family out of Plugin.cs, after the desktop
// (0.1225), persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403), map (0.1404) and gathering
// (0.1412) families. The voyage data (routes, places, fish of each spot, missions, bonuses) and the timetable, read off the frame on first use;
// the voyage reader, its record kept beside the settings; the play state for the developer's scripts, which reads that voyage. The reader and
// the play state are each created in one install path and released in one release path of the plugin's registrations, all in this file, in
// the order of before. Plugin.cs calls the family once, at the same place (after the sheet loaders). OceanFamily1417Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The ocean fishing family: voyage data and timetable off the frame, the voyage reader and the play state; and what the main window reads from them.</summary>
    private void RegisterOcean(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IClientState client,ICondition condition)
    {
        var ocean=new Lazy<Gathering.OceanSchedule?>(()=>mainWindow.OceanTimetable?.Invoke());
        // g, R-07 (0.418): the voyage in progress and the game's ocean fishing data (routes, places, fish of each spot, missions).
        var oceanVoyageData=new Lazy<Gathering.OceanVoyageData?>(()=>
        {
            try
            {
                var routes=data.GetExcelSheet<Lumina.Excel.Sheets.IKDRoute>().Where(row=>!row.Name.IsEmpty)
                    .ToDictionary(row=>row.RowId,row=>(row.Spot.Select(spot=>spot.RowId).Where(id=>id!=0).ToArray(),row.Time.Select(time=>time.RowId).ToArray()));
                var spots=data.GetExcelSheet<Lumina.Excel.Sheets.IKDSpot>().Where(row=>row.PlaceName.RowId!=0)
                    .ToDictionary(row=>row.RowId,row=>new Gathering.OceanSpot(row.RowId,row.PlaceName.ValueNullable?.Name.ExtractText() ?? "",row.SpotMain.RowId,row.SpotSub.RowId));
                var fishingSpots=data.GetExcelSheet<Lumina.Excel.Sheets.FishingSpot>();
                var spotFish=new Dictionary<uint,IReadOnlyList<uint>>();var territories=new HashSet<uint>();
                foreach(var id in spots.Values.SelectMany(spot=>new[]{spot.MainSpot,spot.SpectralSpot}).Where(id=>id!=0).Distinct())
                    if(fishingSpots.GetRowOrDefault(id) is { } fishingSpot)
                    {
                        spotFish[id]=fishingSpot.Item.Select(item=>item.RowId).Where(item=>item!=0).ToArray();
                        if(fishingSpot.TerritoryType.RowId!=0)territories.Add(fishingSpot.TerritoryType.RowId);
                    }
                // X-85 V3 (0.961): each mission's criterion from the English texts (species words matched to the bonus rows), each fish's species.
                var words=Gathering.OceanMissionRules.SpeciesWords(data.GetExcelSheet<Lumina.Excel.Sheets.IKDContentBonus>(Dalamud.Game.ClientLanguage.English)
                    .Select(row=>(row.RowId,row.Requirement.ExtractText())));
                var englishMissions=data.GetExcelSheet<Lumina.Excel.Sheets.IKDPlayerMissionCondition>(Dalamud.Game.ClientLanguage.English);
                var missions=data.GetExcelSheet<Lumina.Excel.Sheets.IKDPlayerMissionCondition>().Where(row=>!row.Unknown0.IsEmpty)
                    .ToDictionary(row=>row.RowId,row=>new Gathering.OceanMission(row.Unknown0.ExtractText(),row.Unknown1,
                        englishMissions.GetRowOrDefault(row.RowId) is { } english ? Gathering.OceanMissionRules.Parse(english.Unknown0.ExtractText(),words):null));
                var fishBonuses=new Dictionary<uint,uint[]>();
                foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.IKDFishParam>())
                    if(row.Fish.ValueNullable?.Item.RowId is { } item and not 0)
                        fishBonuses[item]=row.IKDContentBonus.Select(bonus=>bonus.RowId).Where(id=>id!=0).ToArray();
                return new Gathering.OceanVoyageData(routes,spots,spotFish,missions){Territories=territories,FishBonuses=fishBonuses};
            }
            catch(Exception error){log.Warning(error,"Ocean fishing voyage data unavailable.");return null;}
        });
        // 0.1095 (user report: OceanVoyage 75 to 90 ms on the first frame after each reload): the sheets are read in the background; the panel
        // and the schedule check wait for them (null until then) instead of reading them in the frame.
        mainWindow.OceanVoyageData=InBackground(oceanVoyageData);
        mainWindow.OceanTimetableNow=InBackground(ocean);
        OceanVoyageReader? oceanVoyageReader=null;
        // 0.1094 (user report: seven reloads during one voyage): the voyage kept beside the settings, and the play state for the developer's scripts.
        var voyageWriter=new BackgroundWriter((error,message)=>{log.Warning(error,message);mainWindow.NoteWriteFailed(message);});
        registrations.Register(()=>mainWindow.OceanVoyage=oceanVoyageReader=new OceanVoyageReader(framework,client,log,
            Path.Combine(pluginInterface.GetPluginConfigDirectory(),"OceanVoyage.json"),voyageWriter){Developer=()=>configuration.DeveloperMode,
            CatchPoints=(item,points)=>{if(configuration.Discovery.Enabled && configuration.Discovery.Fishing)mainWindow.DiscoveryStore?.SeeSource(Discoveries.DiscoveryKind.FishPoints,item,0,Math.Min(points,100_000),DateTimeOffset.UtcNow,client.TerritoryType);}},()=>oceanVoyageReader?.Dispose());
        Diagnostics.PlayStateWriter? playState=null;
        registrations.Register(()=>playState=new Diagnostics.PlayStateWriter(framework,condition,pluginInterface.GetPluginConfigDirectory(),
            ()=>pluginInterface.IsDev || configuration.DeveloperMode,()=>oceanVoyageReader?.Frame is not null,voyageWriter),()=>playState?.Dispose());
    }
}
