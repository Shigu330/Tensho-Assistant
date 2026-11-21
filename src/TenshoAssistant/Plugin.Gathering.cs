using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using TenshoAssistant.Caching;

namespace TenshoAssistant;

// X-153 (0.1412, eighth slice, GPT review of 11/11, accepted by the user): the gathering and fishing family out of Plugin.cs, after the desktop
// (0.1225), persistence (0.1239), server bar (0.1240), panels (0.1249), discoveries (0.1250), voyages (0.1403) and map (0.1404) families. The
// community fish services (GatherBuddy fish, bite times), the notebooks' reader, the bait reader and the player's fishing copy on the
// framework: each created in one install path and released in one release path of the plugin's registrations, all in this file, in the order
// of before; with the fishing spots, the Bountiful bases and the node effects built off the frame on first use. Plugin.cs calls the family
// once, at the same place. GatheringFamily1412Tests counts those paths.
public sealed partial class Plugin
{
    /// <summary>The gathering and fishing family: fish services, notebooks, bait, the fishing copy, spots, Bountiful bases and node effects; and what the main window reads from them.</summary>
    private void RegisterGathering(IDalamudPluginInterface pluginInterface,Configuration configuration,IDataManager data,IFramework framework,IClientState client,
        IObjectTable objects,ICondition condition)
    {
        var gatherBuddyFish=new Community.GatherBuddyFishService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityGatherBuddy};
        registrations.Register(()=>{},gatherBuddyFish.Dispose);
        mainWindow.ConfigureGatherBuddyFish(gatherBuddyFish);
        var biteTimes=new Community.FishBiteTimeService(()=>configuration.Cache.Duration(CacheArea.Sources),dataCache){Enabled=()=>configuration.CommunityTeamcraftBites};
        registrations.Register(()=>{},biteTimes.Dispose);
        mainWindow.ConfigureBiteTimes(biteTimes);
        // Fishing help (0.226): the FishingSpot of the player's territory nearest to the player. FishingSpot X/Z are map pixels
        // (0–2048, probe PROBE_FISHSPOT: spot 35 X 668 Z 1120 at scale 200 = map 7.7, 12.2 as Garland): world = (pixel − 1024) / scale − offset.
        // g, R-05 (0.402): fishing, spearfishing and gathering notebooks, read while the Gathering page shows them.
        Gathering.NotebookReader? notebooks=null;
        registrations.Register(()=>notebooks=new Gathering.NotebookReader(framework,client,data,log),()=>notebooks?.Dispose());
        mainWindow.NotebookSource=notebooks;
        mainWindow.NotebookExportFolder=()=>Path.Combine(pluginInterface.GetPluginConfigDirectory(),"Exports");
        // g, R-04 (0.398): the bait in hand for the fishing help.
        Gathering.FishingBaitReader? baitReader=null;
        registrations.Register(()=>baitReader=new Gathering.FishingBaitReader(framework,objects,log),()=>baitReader?.Dispose());
        mainWindow.FishingBait=()=>baitReader?.Bait;
        mainWindow.FishingIntuitionLeft=()=>baitReader?.IntuitionLeft;
        mainWindow.FishingCastAt=()=>baitReader?.CastAt;
        // X-701 (0.1685): the whole last cast (bite, strength, Chum) for the stopwatch frozen at the bite.
        mainWindow.FishingLastCast=()=>baitReader?.LastCast;
        // 0.604 (C35): the player copied on the framework thread, again only when it changed; the two tables built off the frame on first use, nothing
        // shown until they are ready, a failure logged once.
        System.Runtime.CompilerServices.StrongBox<(uint Job,uint Territory,float X,float Z,bool Fishing)?> fishingNowCopy=new(null);
        IFramework.OnUpdateDelegate copyFishingNow=_=>
        {
            var local=objects.LocalPlayer;
            (uint Job,uint Territory,float X,float Z,bool Fishing)? seen=local is null ? null
                :(local.ClassJob.RowId,(uint)client.TerritoryType,local.Position.X,local.Position.Z,condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Fishing]);
            if(seen!=Volatile.Read(ref fishingNowCopy).Value)Volatile.Write(ref fishingNowCopy,new(seen));
        };
        registrations.Register(()=>TenshoAssistant.Competitive.CompetitiveGate.Add(framework,copyFishingNow),()=>TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,copyFishingNow));
        Task<Dictionary<uint,List<(uint Id,string Name,float X,float Z,uint[] Fish)>>>? fishingSpotsTask=null;
        mainWindow.ConfigureFishingHelp(()=>Volatile.Read(ref fishingNowCopy).Value,(territory,x,z)=>
        {
            fishingSpotsTask??=Task.Run(()=>
            {
                var spots=new Dictionary<uint,List<(uint Id,string Name,float X,float Z,uint[] Fish)>>();
                try
                {
                    foreach(var spot in data.GetExcelSheet<Lumina.Excel.Sheets.FishingSpot>())
                    {
                        if(spot.TerritoryType.RowId==0 || !spot.TerritoryType.IsValid)continue;
                        var map=spot.TerritoryType.Value.Map.Value;var scale=Math.Max(1,(int)map.SizeFactor)/100f;
                        var fish=spot.Item.Where(item=>item.RowId!=0).Select(item=>item.RowId).ToArray();
                        if(fish.Length==0)continue;
                        if(!spots.TryGetValue(spot.TerritoryType.RowId,out var list))spots[spot.TerritoryType.RowId]=list=[];
                        list.Add((spot.RowId,spot.PlaceName.IsValid ? spot.PlaceName.Value.Name.ExtractText():"",(spot.X-1024)/scale-map.OffsetX,(spot.Z-1024)/scale-map.OffsetY,fish));
                    }
                }
                catch(Exception error){log.Warning(error,"Fishing spots unavailable (perhaps after a game update).");}
                return spots;
            });
            if(!fishingSpotsTask.IsCompletedSuccessfully)return null;
            return fishingSpotsTask.Result.TryGetValue(territory,out var candidates) && candidates.Count>0
                ? candidates.OrderBy(spot=>(spot.X-x)*(spot.X-x)+(spot.Z-z)*(spot.Z-z)).First():null;
        });
        // Bountiful II base of a gathered item (0.224): ItemLevel.Gathering at its GatheringItemLevelConvertTable row, highest of its
        // gathering rows; 0.604: built off the frame on first use (docs/evidence/0.224-bountiful-base.md).
        Task<Dictionary<uint,int>>? bountifulBases=null;
        mainWindow.ConfigureBountifulBase(item=>
        {
            bountifulBases??=Task.Run(()=>
            {
                var bases=new Dictionary<uint,int>();
                try
                {
                    var levels=data.GetExcelSheet<Lumina.Excel.Sheets.ItemLevel>();
                    foreach(var row in data.GetExcelSheet<Lumina.Excel.Sheets.GatheringItem>())
                        if(row.Item.RowId!=0 && levels.TryGetRow(row.GatheringItemLevel.RowId,out var level) && level.Gathering>0)
                            bases[row.Item.RowId]=Math.Max(bases.GetValueOrDefault(row.Item.RowId),(int)level.Gathering);
                }
                catch(Exception error){log.Warning(error,"Bountiful bases unavailable (perhaps after a game update).");}
                return bases;
            });
            return bountifulBases.IsCompletedSuccessfully && bountifulBases.Result.TryGetValue(item,out var value) ? value:null;
        });
        // 0.515: a node's effects, read once per node (docs/evidence/0.515-node-effects.md). R-14: an unreadable sheet means no effect line, not a broken panel.
        var nodeEffects=new Dictionary<uint,IReadOnlyList<Gathering.GatherNodeEffect>>();
        mainWindow.ConfigureNodeEffects(node=>
        {
            if(nodeEffects.TryGetValue(node,out var known))return known;
            var found=new List<Gathering.GatherNodeEffect>();
            try
            {
                var bonuses=data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPointBonus>();
                if(data.GetExcelSheet<Lumina.Excel.Sheets.GatheringPoint>().TryGetRow(node,out var point))
                    foreach(var link in point.GatheringPointBonus)
                        if(link.RowId!=0 && bonuses.TryGetRow(link.RowId,out var bonus) && Gathering.GatherNodeEffects.From(bonus.Condition.RowId,bonus.ConditionValue,bonus.Condition.ValueNullable?.Text.ExtractText(),bonus.BonusType.RowId,bonus.BonusValue,bonus.BonusType.ValueNullable?.Text.ExtractText()) is { } effect)
                            found.Add(effect);
            }
            catch(Exception error){log.Warning(error,"Node effects unavailable (perhaps after a game update).");}
            nodeEffects[node]=found;
            return found;
        });
    }
}
