using System.Diagnostics;
using System.Numerics;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace TenshoAssistant.Npcs;

/// <summary>
/// Copies shop, NPC and position sheets in bounded framework batches (an iterator resumed each tick), then indexes on a worker.
/// The finished rows are kept in DataCache until the client data, the language or the copy shape changes.
/// </summary>
internal sealed class NpcVendorLoader : IDisposable
{
    private const uint GilItemId=1,FirstSealItemId=19;
    private readonly object gate=new();
    private readonly NpcVendorService service;
    private readonly IDataManager data;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private readonly Caching.DataCache? cache;
    private readonly CancellationTokenSource cancellation=new();
    private IEnumerator<int>? copy;
    private Task<(NpcVendorCatalog? Catalog,Exception? Error,bool FromCache)>? build;
    private Task<Caching.CachedValue<NpcVendorCacheData>?>? cacheRead;
    private bool cacheChecked;
    private string? dataVersion,cacheVersion,cacheScope;
    private NpcVendorCacheData? toPersist;
    private readonly List<NpcShopSale> sales=[];
    private readonly Dictionary<uint,List<uint>> vendorShops=[];
    private readonly Dictionary<uint,List<NpcLocation>> locations=[];
    private readonly Dictionary<uint,string> names=[];
    private bool stopped,disposed;

    public NpcVendorLoader(NpcVendorService service,IDataManager data,IFramework framework,IPluginLog log,Caching.DataCache? cache=null)
    {
        this.service=service;this.data=data;this.framework=framework;this.log=log;this.cache=cache;
        try{framework.Update+=OnUpdate;}catch{cancellation.Dispose();throw;}
    }

    private void OnUpdate(IFramework _)
    {
        if(!framework.IsInFrameworkUpdateThread || framework.IsFrameworkUnloading)return;
        lock(gate)
        {
            if(stopped)return;
            try
            {
                if(build is not null)
                {
                    if(!build.IsCompleted)return;
                    var result=build.GetAwaiter().GetResult();build=null;
                    if(result.Error is not null)
                    {
                        if(!result.FromCache)throw result.Error;
                        log.Warning(result.Error,"Tensho NPC vendor cache rejected; rebuilding from game data.");
                        return;
                    }
                    service.Publish(result.Catalog!);
                    if(toPersist is { } persisted && cache is not null && cacheScope is not null && cacheVersion is not null)
                    {var pending=cache.WriteAsync(NpcVendorCache.Family,cacheScope,cacheVersion,persisted,DateTimeOffset.UtcNow);}
                    Stop();return;
                }
                if(dataVersion is null)
                {
                    dataVersion=string.Join(";",data.GameData.Repositories.Values.OrderBy(r=>r.Name).Select(r=>$"{r.Name}={r.Version}"));
                    cacheScope=data.Language.ToString();
                    cacheVersion=NpcVendorCache.Version($"{dataVersion};schema={typeof(GilShop).Assembly.GetName().Version}");
                }
                if(!cacheChecked && cache is not null)
                {
                    cacheRead??=cache.ReadAsync<NpcVendorCacheData>(NpcVendorCache.Family,cacheScope!,cacheVersion!,cancellation.Token);
                    if(!cacheRead.IsCompleted)return;
                    var cached=cacheRead.GetAwaiter().GetResult();cacheRead=null;cacheChecked=true;
                    if(cached?.Value is { } snapshot)
                    {
                        var expected=dataVersion;var token=cancellation.Token;
                        build=Task.Run<(NpcVendorCatalog?,Exception?,bool)>(()=>
                        {
                            try
                            {
                                if(!NpcVendorCache.Valid(snapshot,expected))throw new InvalidDataException("NPC vendor cache does not match the current game data.");
                                return (NpcVendorCatalog.Build(snapshot.Vendors,snapshot.Sales,expected,token),null,true);
                            }
                            catch(Exception error){return (null,error,true);}
                        });
                        return;
                    }
                }
                copy??=Copy().GetEnumerator();
                var started=Stopwatch.GetTimestamp();var progress=0;var done=false;
                while(Stopwatch.GetElapsedTime(started)<TimeSpan.FromMilliseconds(2))
                {
                    if(!copy.MoveNext()){done=true;break;}
                    progress=copy.Current;
                }
                service.Progress(progress,0);
                if(!done)return;
                copy.Dispose();copy=null;
                var vendors=vendorShops.Select(pair=>new NpcVendor(pair.Key,names.GetValueOrDefault(pair.Key) ?? "",Array.AsReadOnly(pair.Value.Distinct().Order().ToArray()),
                    Array.AsReadOnly((locations.GetValueOrDefault(pair.Key) ?? []).ToArray()))).ToArray();
                var rows=sales.ToArray();var version=dataVersion!;var buildToken=cancellation.Token;
                sales.Clear();vendorShops.Clear();locations.Clear();names.Clear();
                if(cache is not null)toPersist=new(version,Array.AsReadOnly(vendors),Array.AsReadOnly(rows));
                build=Task.Run<(NpcVendorCatalog?,Exception?,bool)>(()=>
                {
                    try{return (NpcVendorCatalog.Build(vendors,rows,version,buildToken),null,false);}
                    catch(Exception error){return (null,error,false);}
                });
            }
            catch(Exception error)
            {
                log.Error(error,"Tensho NPC vendor catalog could not be built.");
                service.Fail(error);Stop();
            }
        }
    }


    // Each yield is one copied row; the value is a running count for progress only.
    private IEnumerable<int> Copy()
    {
        var count=0;
        var items=data.GetExcelSheet<Item>();
        var shops=new HashSet<uint>();
        // Gil shops: price is the item's NPC buy price.
        var gilItems=data.GetSubrowExcelSheet<GilShopItem>();
        foreach(var shop in data.GetExcelSheet<GilShop>())
        {
            if(gilItems.TryGetRow(shop.RowId,out var entries))
            {
                var name=shop.Name.ExtractText();
                foreach(var entry in entries)
                    if(entry.Item.RowId!=0 && items.TryGetRow(entry.Item.RowId,out var item))
                    {sales.Add(new(shop.RowId,NpcShopKind.Gil,name,entry.Item.RowId,1,false,[new(GilItemId,item.PriceMid)]));shops.Add(shop.RowId);}
            }
            yield return ++count;
        }
        // Grand Company quartermasters: seals of the company that owns the shop (items 20-22 verified locally).
        var gcCategories=data.GetExcelSheet<GCScripShopCategory>().Select(category=>(category.RowId,Company:category.GrandCompany.RowId)).ToArray();
        var gcItems=data.GetSubrowExcelSheet<GCScripShopItem>();
        foreach(var shop in data.GetExcelSheet<GCShop>())
        {
            var company=shop.GrandCompany.RowId;
            if(company is >=1 and <=3)
                foreach(var category in gcCategories.Where(category=>category.Company==company))
                    if(gcItems.TryGetRow(category.RowId,out var entries))
                        foreach(var entry in entries)
                            if(entry.Item.RowId!=0)
                            {sales.Add(new(shop.RowId,NpcShopKind.GrandCompany,shop.GrandCompany.Value.Name.ExtractText(),entry.Item.RowId,1,false,[new(FirstSealItemId+company,entry.CostGCSeals)]));shops.Add(shop.RowId);}
            yield return ++count;
        }
        // Special shops: a direct item currency is exact; encoded currencies (UseCurrencyType set, small id) stay unresolved.
        foreach(var shop in data.GetExcelSheet<SpecialShop>())
        {
            var name=shop.Name.ExtractText();var encoding=shop.UseCurrencyType;
            foreach(var entry in shop.Item)
            {
                var costs=entry.ItemCosts.Where(cost=>cost.ItemCost.RowId!=0 && cost.CurrencyCost>0)
                    .Select(cost=>NpcShopLinks.Currency(encoding,cost.ItemCost.RowId,cost.CurrencyCost,cost.CostType)).ToArray();
                foreach(var receive in entry.ReceiveItems)
                    if(receive.Item.RowId!=0 && receive.ReceiveCount>0)
                    {sales.Add(new(shop.RowId,NpcShopKind.Special,name,receive.Item.RowId,receive.ReceiveCount,receive.ReceiveHq,Array.AsReadOnly(costs)));shops.Add(shop.RowId);}
            }
            yield return ++count;
        }
        // Indirections from an NPC to its shops.
        var links=new Dictionary<uint,IReadOnlyList<uint>>();
        foreach(var topic in data.GetExcelSheet<TopicSelect>())
        {links[topic.RowId]=topic.Shop.Select(shop=>shop.RowId).Where(id=>id!=0).ToArray();yield return ++count;}
        foreach(var handler in data.GetExcelSheet<PreHandler>())
        {if(handler.Target.RowId!=0)links[handler.RowId]=[handler.Target.RowId];yield return ++count;}
        foreach(var talk in data.GetExcelSheet<CustomTalk>())
        {
            var arguments=talk.Script.Select(script=>script.ScriptArg).Where(id=>id!=0).ToArray();
            if(arguments.Length>0)links[talk.RowId]=arguments;
            yield return ++count;
        }
        // Scrip exchanges (probe 2026-10-30): NPC → PreHandler → InclusionShop → InclusionShopCategory → InclusionShopSeries → SpecialShop.
        var series=data.GetSubrowExcelSheet<InclusionShopSeries>();var categories=data.GetExcelSheet<InclusionShopCategory>();
        foreach(var inclusion in data.GetExcelSheet<InclusionShop>())
        {
            var targets=inclusion.Category.Where(category=>category.RowId!=0 && categories.HasRow(category.RowId))
                .Select(category=>categories.GetRow(category.RowId).InclusionShopSeries.RowId)
                .SelectMany(id=>series.TryGetRow(id,out var rows) ? rows.Select(row=>row.SpecialShop.RowId).ToArray():[])
                .Where(id=>id!=0).Distinct().ToArray();
            if(targets.Length>0)links[inclusion.RowId]=targets;
            yield return ++count;
        }
        // Script-only shop menus observed in game (see NpcShopLinks.ObservedTalkShops).
        foreach(var observed in NpcShopLinks.ObservedTalkShops)
            links[observed.Key]=links.TryGetValue(observed.Key,out var current) ? [..current,..observed.Value]:observed.Value;
        // FATE gemstone traders: FateShop rows (keyed like the NPC) list their special shops.
        foreach(var fate in data.GetExcelSheet<FateShop>())
        {
            var targets=fate.SpecialShop.Select(shop=>shop.RowId).Where(id=>id!=0).ToArray();
            if(targets.Length>0)links[fate.RowId]=links.TryGetValue(fate.RowId,out var known) ? [..known,..targets]:targets;
            yield return ++count;
        }
        foreach(var npc in data.GetExcelSheet<ENpcBase>())
        {
            var resolved=NpcShopLinks.Resolve(npc.ENpcData.Select(value=>value.RowId).Prepend(npc.RowId),shops,links);
            if(resolved.Count>0)vendorShops[npc.RowId]=[..resolved];
            yield return ++count;
        }
        // Positions: Level rows of type 8 (event NPC) that point at a vendor.
        foreach(var level in data.GetExcelSheet<Level>())
        {
            if(level.Type==8 && vendorShops.ContainsKey(level.Object.RowId) && level.Map.RowId!=0 && level.Map.IsValid)
            {
                var map=level.Map.Value;
                var zone=map.PlaceName.RowId!=0 ? map.PlaceName.Value.Name.ExtractText():"";
                if(!locations.TryGetValue(level.Object.RowId,out var list))locations[level.Object.RowId]=list=[];
                if(list.Count<8)list.Add(new(level.Territory.RowId,map.RowId,zone,
                    NpcShopLinks.MapCoordinate(level.X,map.OffsetX,map.SizeFactor),NpcShopLinks.MapCoordinate(level.Z,map.OffsetY,map.SizeFactor),level.X,level.Y,level.Z));
            }
            yield return ++count;
        }
        // Most vendors are placed by territory layouts, not the Level sheet: read planevent.lgb, one territory per step,
        // only for vendors still without a position (probe: docs/evidence/0.98.0-lgb-probe.log).
        var missing=vendorShops.Keys.Where(id=>!locations.ContainsKey(id)).ToHashSet();
        if(missing.Count>0)
            foreach(var territory in data.GetExcelSheet<TerritoryType>())
            foreach(var kind in LayoutKinds)
            {
                if(territory.Map.RowId!=0 && territory.Map.IsValid && Layout(territory,kind) is { } file)
                {
                    var map=territory.Map.Value;
                    var zone=map.PlaceName.RowId!=0 ? map.PlaceName.Value.Name.ExtractText():"";
                    foreach(var layer in file.Layers)
                    foreach(var instance in layer.InstanceObjects)
                    {
                        if(instance.AssetType!=Lumina.Data.Parsing.Layer.LayerEntryType.EventNPC
                           || instance.Object is not Lumina.Data.Parsing.Layer.LayerCommon.ENPCInstanceObject npc
                           || !missing.Contains(npc.ParentData.ParentData.BaseId))continue;
                        var id=npc.ParentData.ParentData.BaseId;var position=instance.Transform.Translation;
                        var x=NpcShopLinks.MapCoordinate(position.X,map.OffsetX,map.SizeFactor);var y=NpcShopLinks.MapCoordinate(position.Z,map.OffsetY,map.SizeFactor);
                        if(!locations.TryGetValue(id,out var list))locations[id]=list=[];
                        // Several territories share one layout (e.g. instanced copies): keep one entry per map and spot.
                        if(list.Count<8 && !list.Any(known=>known.MapId==map.RowId && Math.Abs(known.X-x)<0.1f && Math.Abs(known.Y-y)<0.1f))
                            list.Add(new(territory.RowId,map.RowId,zone,x,y,position.X,position.Y,position.Z));
                    }
                }
                yield return ++count;
            }
        var residents=data.GetExcelSheet<ENpcResident>();
        foreach(var id in vendorShops.Keys.ToArray())
        {names[id]=residents.TryGetRow(id,out var resident) ? resident.Singular.ExtractText():"";yield return ++count;}
    }

    // Event NPCs are placed by these two layout kinds (probe: docs/evidence/0.99.0-consigneur-probe.log).
    private static readonly string[] LayoutKinds=["planevent","planner"];

    // A missing, unexpected or unreadable layout only means no extra position for that territory.
    private Lumina.Data.Files.LgbFile? Layout(TerritoryType territory,string kind)
    {
        var bg=territory.Bg.ExtractText();var cut=bg.IndexOf("/level/",StringComparison.Ordinal);
        if(cut<0)return null;
        var path=$"bg/{bg[..(cut+1)]}level/{kind}.lgb";
        try
        {
            // Lumina's LGB parser trusts its counts: an empty or differently laid-out file made the probe run out of memory.
            // Only files with the LGB1 header, a matching declared size and the LGP1 chunk at offset 12 are parsed.
            if(!data.FileExists(path) || data.GetFile(path)?.Data is not { } bytes || !NpcShopLinks.ValidLayout(bytes))return null;
            return data.GetFile<Lumina.Data.Files.LgbFile>(path);
        }
        catch(Exception error){log.Debug(error,"Territory layout unreadable: {0}",path);return null;}
    }

    private void Stop()
    {
        stopped=true;framework.Update-=OnUpdate;copy?.Dispose();copy=null;cacheRead=null;toPersist=null;
        sales.Clear();vendorShops.Clear();locations.Clear();names.Clear();
    }
    public void Dispose()
    {
        lock(gate)
        {
            if(disposed)return;
            disposed=true;cancellation.Cancel();if(!stopped)Stop();service.Clear();cancellation.Dispose();
        }
    }
}
