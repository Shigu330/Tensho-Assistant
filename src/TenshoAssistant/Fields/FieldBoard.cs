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
    /// <summary>The four Eureka zones, Bozjan Southern Front, Zadnor. TerritoryType, checked on 2026-11-07.</summary>
    public static readonly IReadOnlyList<uint> Territories=[732,763,795,827,920,975];

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

    public static bool Matches(string query,params string[] texts)=>query.Length==0 || texts.Any(text=>text.Contains(query,StringComparison.CurrentCultureIgnoreCase));
}
