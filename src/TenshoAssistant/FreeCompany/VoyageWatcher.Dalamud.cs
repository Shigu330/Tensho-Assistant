using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using SubmarineExploration = Lumina.Excel.Sheets.SubmarineExploration;
using SubmarineMap = Lumina.Excel.Sheets.SubmarineMap;
using Item = Lumina.Excel.Sheets.Item;
using SubmarinePart = Lumina.Excel.Sheets.SubmarinePart;
using SubmarineRank = Lumina.Excel.Sheets.SubmarineRank;
using AirshipExplorationPart = Lumina.Excel.Sheets.AirshipExplorationPart;
using AirshipExplorationLevel = Lumina.Excel.Sheets.AirshipExplorationLevel;
using SubmarineSpecCategory = Lumina.Excel.Sheets.SubmarineSpecCategory;
using AirshipExplorationParamType = Lumina.Excel.Sheets.AirshipExplorationParamType;
using AirshipExplorationPoint = Lumina.Excel.Sheets.AirshipExplorationPoint;

namespace TenshoAssistant.FreeCompany;

/// <summary>
/// R-07 (0.307): while the player is in the free company workshop, copies its submarines and airships (name, rank, experience,
/// registration and return times, submarine route and last finds) every 3 seconds on the framework thread, and saves them when
/// they changed. Outside the workshop the game gives none of this: the saved copy is what the page shows. Removed on dispose.
/// Also read: submarine stats and route sectors (0.309); part rows, airship stats and the company's unlocked submarine sectors,
/// saved per house (0.315). Built once from the game sheets: the route catalog (sectors, maps, NPC sell prices + embedded
/// FFXIVGachaSpreadsheet loot, MIT) and the part catalog (parts, ranks + embedded SubmarineTracker breakpoints, MIT; licence
/// notices in FreeCompany/Data/).
/// </summary>
public sealed unsafe class VoyageWatcher : IDisposable
{
    private readonly IFramework framework;
    private readonly IDataManager data;
    private readonly Configuration configuration;
    private readonly Action save;
    private readonly IPluginLog log;
    private DateTimeOffset nextRead;
    private bool disposed,catalogTried,airshipsWarned,unlockedWarned;
    // 0.331 (audit): house whose read was saved during the current workshop visit (null outside the workshop).
    private ulong? visitSaved;
    private SubmarineCatalog? catalog;
    private VesselPartCatalog? partCatalog;

    /// <summary>0.379 (g, M-10): called on the framework thread with the vessels of a workshop read that changed (voyage history).</summary>
    public Action<IReadOnlyList<VoyageVessel>>? Observed { get; set; }
    /// <summary>0.315 (R-11): vessel parts, ranks, stat names and the embedded submarine breakpoints for the build planner (built once).</summary>
    public VesselPartCatalog? PartCatalog=>Volatile.Read(ref partCatalog);

    /// <summary>0.309: sectors, maps, embedded community loot and NPC sell prices for the route planner (built once).</summary>
    public SubmarineCatalog? Catalog=>Volatile.Read(ref catalog);

    /// <summary>0.331 (audit): the route catalog could not be built (no retry this session): the pages say « unavailable », not « loading ».</summary>
    public bool CatalogFailed { get; private set; }

    /// <summary>0.331 (audit): the part catalog could not be built (no retry this session).</summary>
    public bool PartCatalogFailed { get; private set; }

    private AirshipCatalog? airshipCatalog;
    /// <summary>0.336 (DEC-023, point 3): airship sectors (game data) with the embedded LuminaSupplemental unlock tree and drops (built once).</summary>
    public AirshipCatalog? AirshipCatalog=>Volatile.Read(ref airshipCatalog);
    public bool AirshipCatalogFailed { get; private set; }

    /// <summary>0.331 (audit, R-14): the last workshop read failed in whole or in part (game update?); the saved values were kept. Cleared by a clean read.</summary>
    public bool ReadUnavailable { get; private set; }

    public VoyageWatcher(IFramework framework,IDataManager data,Configuration configuration,Action save,IPluginLog log)
    {
        this.framework=framework;this.data=data;this.configuration=configuration;this.save=save;this.log=log;
        TenshoAssistant.Competitive.CompetitiveGate.Add(framework,OnUpdate);
    }

    private void OnUpdate(IFramework _)
    {
        var now=DateTimeOffset.UtcNow;
        if(disposed || now<nextRead)return;
        nextRead=now.AddSeconds(3);
        if(!catalogTried)
        {
            catalogTried=true;
            // 0.525 (user report: lag when the plugin loads; Dalamud logged 85 to 175 ms here at each load): sheets and embedded files only, so off the framework thread.
            Task.Run(()=>
            {
                try{Volatile.Write(ref catalog,BuildCatalog());}
                catch(Exception error){CatalogFailed=true;log.Warning(error,"Submarine route data could not be built.");}
                try{Volatile.Write(ref partCatalog,BuildPartCatalog());}
                catch(Exception error){PartCatalogFailed=true;log.Warning(error,"Vessel part data could not be built.");}
                try{Volatile.Write(ref airshipCatalog,BuildAirshipCatalog());}
                catch(Exception error){AirshipCatalogFailed=true;log.Warning(error,"Airship sector data could not be built.");}
            });
        }
        var reading=true;
        try
        {
            var housing=HousingManager.Instance();
            if(housing==null || housing->WorkshopTerritory==null){visitSaved=null;return;}
            var workshop=housing->WorkshopTerritory;
            var houseId=workshop->HouseId.Id;
            var read=new List<VoyageVessel>();
            var points=data.GetExcelSheet<SubmarineExploration>();
            var slot=0;
            foreach(ref var sub in workshop->Submersible.Data)
            {
                slot++;
                if(sub.RankId==0)continue;
                var route=new List<string>();
                foreach(var point in sub.CurrentExplorationPoints)
                    if(point!=0 && points.GetRowOrDefault(point) is { } row)route.Add(row.Destination.ExtractText());
                var loot=new List<VoyageLoot>();
                foreach(var found in sub.GatheredData)
                {
                    if(found.ItemIdPrimary!=0)loot.Add(new(found.ItemIdPrimary,found.ItemCountPrimary));
                    if(found.ItemIdAdditional!=0)loot.Add(new(found.ItemIdAdditional,found.ItemCountAdditional));
                }
                var ids=new List<uint>();
                foreach(var point in sub.CurrentExplorationPoints)if(point!=0)ids.Add(point);
                // Stats: parts (« Base ») plus the rank's bonus, as the game's submersible window adds them.
                read.Add(new VoyageVessel{HouseId=houseId,Kind=VesselKind.Submarine,Slot=slot,Name=sub.NameString,Rank=sub.RankId,Exp=sub.CurrentExp,NextExp=sub.NextLevelExp,
                    RegisteredAt=Time(sub.RegisterTime),ReturnAt=Time(sub.ReturnTime),Route=route,Points=ids,Loot=loot,ReadAt=now,
                    Surveillance=sub.SurveillanceBase+sub.SurveillanceBonus,Retrieval=sub.RetrievalBase+sub.RetrievalBonus,Speed=sub.SpeedBase+sub.SpeedBonus,
                    Range=sub.RangeBase+sub.RangeBonus,Favor=sub.FavorBase+sub.FavorBonus,
                    // 0.315: the four part rows (FFXIVClientStructs: « SubmarinePart Key »), placed by their sheet slot.
                    Parts=PartsBySlot(VesselKind.Submarine,[sub.HullId,sub.SternId,sub.BowId,sub.BridgeId])});
            }
            // 0.331 (audit, R-14): airships and unlocked sectors are read apart. A failure keeps their saved values and is logged once
            // until that part reads again, so the submarines just read are still merged and saved.
            var partial=false;
            var airRead=new List<VoyageVessel>();
            try
            {
                var airships=workshop->Airship.Data;
                for(var index=0;index<Math.Min(workshop->Airship.AirshipCount,airships.Length);index++)
                {
                    ref var air=ref airships[index];
                    if(air.RankId==0)continue;
                    var loot=new List<VoyageLoot>();
                    foreach(var found in air.GatheredData)
                    {
                        if(found.ItemIdPrimary!=0)loot.Add(new(found.ItemIdPrimary,found.ItemCountPrimary));
                        if(found.ItemIdAdditional!=0)loot.Add(new(found.ItemIdAdditional,found.ItemCountAdditional));
                    }
                    // 0.315: airship stats (single totals: airship ranks add no bonus) and part rows (« AirshipExplorationPart Key »).
                    airRead.Add(new VoyageVessel{HouseId=houseId,Kind=VesselKind.Airship,Slot=index+1,Name=air.NameString,Rank=air.RankId,Exp=air.CurrentExp,NextExp=air.NextLevelExp,
                        RegisteredAt=Time(air.RegisterTime),ReturnAt=Time(air.ReturnTime),Loot=loot,ReadAt=now,
                        Surveillance=air.Surveillance,Retrieval=air.Retrieval,Speed=air.Speed,Range=air.Range,Favor=air.Favor,
                        Parts=PartsBySlot(VesselKind.Airship,[air.HullId,air.SternId,air.BowId,air.BridgeId])});
                }
                airshipsWarned=false;
            }
            catch(Exception error)
            {
                partial=true;airRead=(configuration.Voyages ?? []).Where(vessel=>vessel.HouseId==houseId && vessel.Kind==VesselKind.Airship).ToList();
                if(!airshipsWarned){airshipsWarned=true;log.Warning(error,"Free company airships could not be read; the saved ones are kept.");}
            }
            read.AddRange(airRead);
            // 0.315: submarine sectors this company has unlocked (HousingManager.IsSubmarineExplorationUnlocked), saved per house.
            var unlockedChanged=false;
            try
            {
                var unlocked=points.Where(row=>row.Map.RowId!=0 && !row.StartingPoint && row.RowId<=byte.MaxValue && HousingManager.IsSubmarineExplorationUnlocked((byte)row.RowId))
                    .Select(row=>row.RowId).OrderBy(id=>id).ToList();
                if(unlocked.Count>0)
                {
                    var known=configuration.SubmarineUnlocked??=[];
                    var key=houseId.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if(!known.TryGetValue(key,out var previous) || !previous.SequenceEqual(unlocked)){known[key]=unlocked;unlockedChanged=true;}
                }
                unlockedWarned=false;
            }
            catch(Exception error)
            {
                partial=true;
                if(!unlockedWarned){unlockedWarned=true;log.Warning(error,"Unlocked submarine sectors could not be read; the saved ones are kept.");}
            }
            reading=false;ReadUnavailable=partial;
            // Nothing registered yet (or the workshop data not received): keep the saved copy.
            if(read.Count==0){if(unlockedChanged)save();return;}
            var merged=Voyages.Merge(configuration.Voyages,houseId,read);
            // 0.331 (audit): the first read of a visit is saved even when nothing changed, so « Read in the workshop » shows this visit;
            // the later 3 s reads that change nothing are not saved again.
            if(visitSaved==houseId && configuration.Voyages is { } saved && Voyages.SameAs(saved,merged)){if(unlockedChanged)save();return;}
            configuration.Voyages=merged;
            save();
            visitSaved=houseId;
            try{Observed?.Invoke(merged.Where(vessel=>vessel.HouseId==houseId).ToArray());}
            catch(Exception error){log.Warning(error,"Voyage history could not be updated.");}
        }
        catch(Exception error)
        {
            // 0.331 (audit): only a failed read (not a failed save) shows « workshop read unavailable » on the Vessels tab.
            if(reading)ReadUnavailable=true;
            log.Warning(error,"Free company voyages could not be read.");
            nextRead=now.AddSeconds(60);
        }
    }

    // 0.336: sheet rows 0 to 24 are the sectors (row 22 is the old Diadem, carries passengers, not offered); unnamed rows are skipped.
    private AirshipCatalog BuildAirshipCatalog()
    {
        IReadOnlyDictionary<uint,AirshipUnlock> unlocks;IReadOnlyDictionary<uint,IReadOnlyList<uint>> drops;
        using(var stream=typeof(VoyageWatcher).Assembly.GetManifestResourceStream("TenshoAssistant.FreeCompany.Data.airship-unlock.csv")
            ?? throw new InvalidOperationException("Embedded airship unlocks are missing."))
        using(var reader=new StreamReader(stream))unlocks=AirshipSectors.ReadUnlocks(reader);
        using(var stream=typeof(VoyageWatcher).Assembly.GetManifestResourceStream("TenshoAssistant.FreeCompany.Data.airship-drop.csv")
            ?? throw new InvalidOperationException("Embedded airship drops are missing."))
        using(var reader=new StreamReader(stream))drops=AirshipSectors.ReadDrops(reader);
        var sectors=data.GetExcelSheet<AirshipExplorationPoint>()
            .Where(row=>!row.Name.IsEmpty && !row.Passengers && row.RankReq>0)
            .Select(row=>new AirshipSector(row.RowId,row.Name.ExtractText(),row.NameShort.ExtractText(),row.RankReq,row.CeruleumTankReq,row.SurveyDistance,
                row.SurveyDurationmin,row.ExpReward,unlocks.GetValueOrDefault(row.RowId)))
            .OrderBy(sector=>sector.Id).ToArray();
        return new AirshipCatalog{Sectors=sectors,Drops=drops};
    }

    private SubmarineCatalog BuildCatalog()
    {
        var sectors=data.GetExcelSheet<SubmarineExploration>()
            .Where(row=>row.Map.RowId!=0)
            .Select(row=>new SubmarineSector(row.RowId,row.Map.RowId,row.StartingPoint,row.RankReq,new System.Numerics.Vector3(row.X,row.Y,row.Z),row.SurveyDistance,
                row.SurveyDurationmin,row.ExpReward,row.Destination.ExtractText()))
            .ToDictionary(sector=>sector.Id);
        var maps=data.GetExcelSheet<SubmarineMap>().Where(row=>row.RowId!=0 && !row.Name.IsEmpty).Select(row=>(row.RowId,row.Name.ExtractText())).ToArray();
        using var stream=typeof(VoyageWatcher).Assembly.GetManifestResourceStream("TenshoAssistant.FreeCompany.Data.submarine-loot.json")
            ?? throw new InvalidOperationException("Embedded submarine loot is missing.");
        var (loot,source,built)=VoyagePlanner.ReadLoot(stream);
        var items=data.GetExcelSheet<Item>();
        var prices=loot.Values.SelectMany(sector=>sector.Items).Select(item=>item.ItemId).Distinct()
            .ToDictionary(id=>id,id=>items.GetRowOrDefault(id) is { } row ? row.PriceLow:0u);
        // 0.335: same rule as the item catalog's IsMarketable (tradable and listed in a market search category).
        var marketable=prices.Keys.Where(id=>items.GetRowOrDefault(id) is { } row && !row.IsUntradable && row.ItemSearchCategory.RowId!=0).ToHashSet();
        return new SubmarineCatalog{Sectors=sectors,Maps=maps,Loot=loot,SellPrices=prices,Marketable=marketable,LootSource=source,LootBuilt=built};
    }

    // Part rows in sheet slot order (0..3); an id the sheet does not know is dropped (0 = unknown).
    private List<uint> PartsBySlot(VesselKind kind,ushort[] ids)
    {
        var parts=new List<uint>{0,0,0,0};
        foreach(var id in ids)
        {
            if(id==0)continue;
            int? slot=kind==VesselKind.Submarine ? data.GetExcelSheet<SubmarinePart>().GetRowOrDefault(id) is { } sub ? sub.Slot:null
                :data.GetExcelSheet<AirshipExplorationPart>().GetRowOrDefault(id) is { } air ? air.Slot:null;
            if(slot is >= 0 and < 4)parts[slot.Value]=id;
        }
        return parts;
    }

    private VesselPartCatalog BuildPartCatalog()
    {
        // Part items: Item.FilterGroup 36 (submarine) / 28 (airship) with AdditionalData = part row (docs/evidence/0.315-vessel-builds.md).
        var items=new Dictionary<(VesselKind,uint),(uint Id,string Name)>();
        foreach(var item in data.GetExcelSheet<Item>())
        {
            if(item.FilterGroup is not (36 or 28) || item.AdditionalData.RowId==0)continue;
            items[(item.FilterGroup==36 ? VesselKind.Submarine:VesselKind.Airship,item.AdditionalData.RowId)]=(item.RowId,item.Name.ExtractText());
        }
        var parts=new Dictionary<(VesselKind,uint),VesselPart>();
        foreach(var row in data.GetExcelSheet<SubmarinePart>())
        {
            if(row.RowId==0 || row.Rank==0)continue;
            var item=items.GetValueOrDefault((VesselKind.Submarine,row.RowId));
            parts[(VesselKind.Submarine,row.RowId)]=new(row.RowId,VesselKind.Submarine,row.Class,row.Slot,row.Rank,row.Components,row.RepairMaterials,
                new(row.Surveillance,row.Retrieval,row.Speed,row.Range,row.Favor),item.Id,item.Name ?? "");
        }
        foreach(var row in data.GetExcelSheet<AirshipExplorationPart>())
        {
            if(row.RowId==0 || row.Rank==0)continue;
            var item=items.GetValueOrDefault((VesselKind.Airship,row.RowId));
            parts[(VesselKind.Airship,row.RowId)]=new(row.RowId,VesselKind.Airship,row.Class,row.Slot,row.Rank,row.Components,row.RepairMaterials,
                new(row.Surveillance,row.Retrieval,row.Speed,row.Range,row.Favor),item.Id,item.Name ?? "");
        }
        var subRanks=data.GetExcelSheet<SubmarineRank>().Where(row=>row.RowId!=0)
            .Select(row=>new VesselRank((int)row.RowId,row.Capacity,row.ExpToNext,new(row.SurveillanceBonus,row.RetrievalBonus,row.SpeedBonus,row.RangeBonus,row.FavorBonus))).ToArray();
        var airRanks=data.GetExcelSheet<AirshipExplorationLevel>().Where(row=>row.RowId!=0)
            .Select(row=>new VesselRank((int)row.RowId,row.Capacity,row.ExpToNext,default)).ToArray();
        using var stream=typeof(VoyageWatcher).Assembly.GetManifestResourceStream("TenshoAssistant.FreeCompany.Data.submarine-breakpoints.csv")
            ?? throw new InvalidOperationException("Embedded submarine breakpoints are missing.");
        using var reader=new StreamReader(stream);
        return new VesselPartCatalog
        {
            Parts=parts,SubmarineRanks=subRanks,AirshipRanks=airRanks,Breakpoints=VesselBuilds.ReadBreakpoints(reader),
            SubmarineStatNames=data.GetExcelSheet<SubmarineSpecCategory>().Take(5).Select(row=>row.Unknown0.ExtractText()).ToArray(),
            AirshipStatNames=data.GetExcelSheet<AirshipExplorationParamType>().Take(5).Select(row=>row.Name.ExtractText()).ToArray(),
        };
    }

    private static DateTimeOffset? Time(uint unix)=>unix==0 ? null:DateTimeOffset.FromUnixTimeSeconds(unix);

    public void Dispose()
    {
        if(disposed)return;
        disposed=true;
        TenshoAssistant.Competitive.CompetitiveGate.Remove(framework,OnUpdate);
    }
}
