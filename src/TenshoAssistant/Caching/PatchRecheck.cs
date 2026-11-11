using System.Text.Json;
using TenshoAssistant.Discoveries;

namespace TenshoAssistant.Caching;

/// <summary>X-117 part 2b (0.1048): the discoveries to recheck after a game patch and why (place: a changed map position in or next to
/// the fact's 50-yalm cell; vendor: the NPC's vendor row changed).</summary>
public sealed record PatchRecheckResult(int Facts,int Places,int Vendors,int ChangedCells,int ChangedVendors)
{
    public static readonly PatchRecheckResult None=new(0,0,0,0,0);
}

/// <summary>
/// X-117 part 2b (0.1048): the facts seen last before the first load of the new client, in a place or about a vendor the patch changed.
/// Same rule as tools/discoveries-merge.py recheck (cell and subject parts), fed by the catalogue comparison of DataCache: the map
/// positions (world X/Z, Map row) of the rows added, removed or changed, and the vendors (ENpc) whose row changed. Pure: no game, no lock.
/// </summary>
public static class PatchRecheck
{
    public const string PositionsFamily="catalog-map-positions";
    public const string VendorsFamily="catalog-npc-vendors";

    public static PatchRecheckResult Find(IEnumerable<PatchCatalogReport> reports,IEnumerable<DiscoveryFact> facts)
    {
        var cells=new HashSet<(uint Map,int X,int Z)>();var vendors=new HashSet<uint>();DateTimeOffset? before=null;
        foreach(var report in reports)
        {
            if(report.Skipped is not null || report.Family is not (PositionsFamily or VendorsFamily))continue;
            before=before is { } known && known<report.At ? known:report.At;
            foreach(var array in report.Arrays)
                foreach(var key in array.AddedKeys.Concat(array.RemovedKeys).Concat(array.ChangedKeys))
                {
                    if(Row(key) is not { } row)continue;
                    if(report.Family==VendorsFamily){if(Number(row,"NpcId") is { } npc and >0)vendors.Add(npc);continue;}
                    if(Number(row,"MapId") is { } map and >0 && Coordinate(row,"X") is { } x && Coordinate(row,"Z") is { } z)
                    {var cell=DiscoveryStore.Cell(x,z);cells.Add((map,cell.X,cell.Z));}
                }
        }
        if(before is not { } patchSeen || cells.Count==0 && vendors.Count==0)return PatchRecheckResult.None with {ChangedCells=cells.Count,ChangedVendors=vendors.Count};
        int total=0,places=0,byVendor=0;
        foreach(var fact in facts)
        {
            if(fact.Last>=patchSeen)continue;
            var inCell=fact.Map!=0 && DiscoveryStore.HasPlace(fact.Kind) && NearChanged(cells,fact);
            var vendor=fact.Kind==DiscoveryKind.NpcPlace && vendors.Contains(fact.Subject);
            if(inCell)places++;
            if(vendor)byVendor++;
            if(inCell || vendor)total++;
        }
        return new(total,places,byVendor,cells.Count,vendors.Count);
    }

    private static bool NearChanged(HashSet<(uint Map,int X,int Z)> cells,DiscoveryFact fact)
    {
        for(var dx=-1;dx<=1;dx++)
            for(var dz=-1;dz<=1;dz++)
                if(cells.Contains((fact.Map,fact.CellX+dx,fact.CellZ+dz)))return true;
        return false;
    }

    // A key is the row's own JSON text when the list has no identifier (DataCache compares those rows by text); other keys are skipped.
    private static JsonElement? Row(string key)
    {
        if(key.Length==0 || key[0]!='{')return null;
        try{using var document=JsonDocument.Parse(key);return document.RootElement.Clone();}
        catch(JsonException){return null;}
    }
    private static uint? Number(JsonElement row,string name)=>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetUInt32(out var number) ? number:null;
    private static float? Coordinate(JsonElement row,string name)=>row.TryGetProperty(name,out var value) && value.ValueKind==JsonValueKind.Number && value.TryGetSingle(out var number) && float.IsFinite(number) ? number:null;
}
