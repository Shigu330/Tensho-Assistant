using System.Numerics;

namespace TenshoAssistant.Maps;

/// <summary>An Aetheryte row: a main aetheryte (Main) or an aethernet shard of its group; World from its Level row, null when not placed.</summary>
public sealed record AetheryteRow(uint Id,bool Main,byte Group,uint Territory,string Name,Vector3? World);
/// <summary>A map next to the target, with the ways into the target on it (its link markers) and its aetheryte markers.</summary>
public sealed record ZoneEntryLink(MapDefinition Neighbour,IReadOnlyList<Vector2> Exits,IReadOnlyList<(uint AetheryteId,Vector2 Pixel)> Aetherytes);
/// <summary>Where to teleport for a zone with no aetheryte: the aetheryte, then the aethernet shard to take (none: walk), and whether its town has no aethernet.</summary>
public sealed record ZoneEntry(uint Aetheryte,uint? Shard,bool NoAethernet,uint ViaMapId);

/// <summary>
/// 0.833 (X-63, user request: the Dravanian Hinterlands have no aetheryte, Idyllshire's leads there). Data (tools/MapMaskProbe --markers, docs/evidence/0.833-zone-entries.md):
/// Idyllshire's Prologue and Epilogue gates are aethernet shards whose territory is the Hinterlands; zone maps carry link markers (DataType 1) to their neighbours.
/// First an aethernet shard placed in the zone itself (nearest the point); else the cheapest attuned aetheryte of a neighbouring map, with the shard of its
/// aethernet nearest the way into the zone, or « no aethernet » to say before the teleport.
/// </summary>
public static class ZoneEntries
{
    public static ZoneEntry? Plan(MapDefinition target,Vector2 point,IReadOnlyList<AetheryteRow> rows,IReadOnlyList<ZoneEntryLink> links,IReadOnlyDictionary<uint,uint>? costs,
        Func<Vector3,MapDefinition,Vector2> pixel)
    {
        bool Known(uint id)=>costs is null || costs.ContainsKey(id);
        AetheryteRow? MainOf(byte group)=>group==0 ? null:rows.FirstOrDefault(row=>row.Main && row.Group==group);
        var inZone=rows.Where(row=>!row.Main && row.Group!=0 && row.Territory==target.TerritoryId && row.World is not null && MainOf(row.Group) is { } main && Known(main.Id))
            .OrderBy(row=>Vector2.Distance(pixel(row.World!.Value,target),point)).FirstOrDefault();
        if(inZone is not null)return new(MainOf(inZone.Group)!.Id,inZone.Id,false,target.MapId);
        var best=links.Where(link=>link.Exits.Count>0).SelectMany(link=>link.Aetherytes.Where(aetheryte=>Known(aetheryte.AetheryteId)).Select(aetheryte=>(link,aetheryte)))
            .OrderBy(pair=>costs?.GetValueOrDefault(pair.aetheryte.AetheryteId,uint.MaxValue) ?? 0u)
            .ThenBy(pair=>pair.link.Exits.Min(exit=>Vector2.Distance(exit,pair.aetheryte.Pixel))).Select(pair=>((ZoneEntryLink Link,(uint AetheryteId,Vector2 Pixel) Aetheryte)?)pair).FirstOrDefault();
        if(best is not { } chosen)return null;
        var map=chosen.Link.Neighbour;var id=chosen.Aetheryte.AetheryteId;
        var group=rows.FirstOrDefault(row=>row.Id==id)?.Group ?? 0;
        if(group==0)return new(id,null,true,map.MapId);
        float ToExit(Vector2 at)=>chosen.Link.Exits.Min(exit=>Vector2.Distance(exit,at));
        var shard=rows.Where(row=>!row.Main && row.Group==group && row.Territory==map.TerritoryId && row.World is not null)
            .Select(row=>(row.Id,Distance:ToExit(pixel(row.World!.Value,map)))).OrderBy(row=>row.Distance).Select(row=>((uint Id,float Distance)?)row).FirstOrDefault();
        return new(id,shard is { } near && near.Distance<ToExit(chosen.Aetheryte.Pixel) ? near.Id:null,false,map.MapId);
    }
}
