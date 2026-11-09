namespace TenshoAssistant.Fields;

public enum FieldEventKind { Fate,Engagement }
/// <summary>W-14 (0.717): something that happens in a field, where the zone's layout puts it. Level null: the sheet gives none. docs/evidence/0.717-eureka-bozja.md.</summary>
public sealed record FieldEvent(FieldEventKind Kind,uint Id,string Name,string Description,int? Level,int Participants,float X,float Z);
public sealed record FieldZone(uint Territory,uint Map,string Name,IReadOnlyList<FieldEvent> Events);
public sealed record FieldRecord(uint Id,int Number,string Name,string Description,int Rarity,string Quest,uint Icon);
public sealed record FieldAction(uint Id,int Order,string Name,string Category,int Most,string Description,uint Icon);
public sealed record FieldCatalog(IReadOnlyList<FieldZone> Zones,IReadOnlyList<FieldRecord> Records,IReadOnlyList<FieldAction> Actions);
/// <summary>A layout entry, or a sheet row that a layout entry can place.</summary>
public readonly record struct FieldLayoutEntry(uint Instance,float X,float Z);
public sealed record FieldEventRow(FieldEventKind Kind,uint Id,uint Instance,string Name,string Description,int? Level,int Participants);

public static class FieldBoard
{
    /// <summary>
    /// X-90 / X-92 (0.975): the field operation zones are the TerritoryType rows of these uses: 41 Eureka, 48 Bozja, 61 Occult Crescent
    /// (docs/evidence/0.975-field-zones.md; the six zones fixed in 0.717 are exactly uses 41 and 48). A new zone of the same family is listed without code.
    /// </summary>
    public static readonly IReadOnlyList<uint> FieldZoneUses=[41,48,61];

    /// <summary>The rows the layout places, once each, the FATEs first by level. A row with no entry in this layout belongs to another zone.</summary>
    public static IReadOnlyList<FieldEvent> Place(IEnumerable<FieldLayoutEntry> entries,IEnumerable<FieldEventRow> rows)
    {
        var at=new Dictionary<uint,FieldLayoutEntry>();
        foreach(var entry in entries)at.TryAdd(entry.Instance,entry);
        return rows.Where(row=>row.Name.Length>0 && at.ContainsKey(row.Instance))
            .DistinctBy(row=>(row.Kind,row.Id))
            .Select(row=>new FieldEvent(row.Kind,row.Id,row.Name,row.Description,row.Level,row.Participants,at[row.Instance].X,at[row.Instance].Z))
            .OrderBy(row=>row.Kind).ThenBy(row=>row.Level ?? int.MaxValue).ThenBy(row=>row.Id).ToArray();
    }

    /// <summary>TerritoryIntendedUse 1: the open zones. Other uses repeat their layouts for quests and instances. docs/evidence/0.721-fates.md.</summary>
    public const uint OpenZoneUse=1;

    /// <summary>A place found by search, wherever it is, or a zone's own list.</summary>
    public static IReadOnlyList<(FieldZone Zone,FieldEvent Row)> Find(IReadOnlyList<FieldZone> zones,int zone,string query)
    {
        if(query.Trim().Length>=2)
            return zones.SelectMany(place=>place.Events.Where(row=>Matches(query.Trim(),row.Name,row.Description)).Select(row=>(place,row))).ToArray();
        return zone>=0 && zone<zones.Count ? zones[zone].Events.Select(row=>(zones[zone],row)).ToArray():[];
    }

    public static int ZoneOf(IReadOnlyList<FieldZone> zones,uint territory)
    {
        for(var index=0;index<zones.Count;index++)if(zones[index].Territory==territory)return index;
        return 0;
    }

    public static bool Matches(string query,params string[] texts)=>query.Length==0 || texts.Any(text=>text.Contains(query,StringComparison.CurrentCultureIgnoreCase));
}
