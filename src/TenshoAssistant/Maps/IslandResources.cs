namespace TenshoAssistant.Maps;

/// <summary>N-07 (0.667): a place of the Island Sanctuary with what can be gathered there (MJIGatheringItem, docs/evidence/0.667-island-resources.md).</summary>
public sealed record IslandPoint(int X,int Y,int Radius,IReadOnlyList<uint> Items);

public static class IslandResources
{
    /// <summary>TerritoryType 1055, « h1m2 », map 773.</summary>
    public const uint Territory=1055;
    /// <summary>Several items share a place: one point each. Rows without an item or a place are left out.</summary>
    public static IReadOnlyList<IslandPoint> Points(IEnumerable<(uint Item,int X,int Y,int Radius)> rows)
        =>rows.Where(row=>row.Item!=0 && (row.X!=0 || row.Y!=0)).GroupBy(row=>(row.X,row.Y))
            .Select(group=>new IslandPoint(group.Key.X,group.Key.Y,group.Max(row=>row.Radius),group.Select(row=>row.Item).Distinct().ToArray())).ToArray();
}
