namespace TenshoAssistant.Gathering;

/// <summary>
/// R-14 (0.416): the gathering list of a crafting chain, by zone. Each needed item goes to one zone: the zone that gathers the most
/// items still to place is taken first (greedy, fewer trips; ties: the zone named first), and in that zone a point without an ET window
/// (unrestricted or not copied) before a timed one, then the lowest level. Items with no gathering source stay apart (bought, dropped, crafted…).
/// </summary>
public sealed record GatheringRouteRow(uint ItemId,long Amount,GatheringSource Source);
public sealed record GatheringRouteZone(uint TerritoryId,string Zone,IReadOnlyList<GatheringRouteRow> Rows);
public sealed record GatheringRoute(IReadOnlyList<GatheringRouteZone> Zones,IReadOnlyList<(uint ItemId,long Amount)> Elsewhere);

public static class GatheringRoutes
{
    public static GatheringRoute Build(IEnumerable<(uint ItemId,long Amount)> needs,Func<uint,IReadOnlyList<GatheringSource>> sources)
    {
        var wanted=needs.Where(need=>need.ItemId!=0 && need.Amount>0).GroupBy(need=>need.ItemId)
            .Select(group=>(ItemId:group.Key,Amount:group.Sum(need=>need.Amount))).ToArray();
        var byItem=wanted.ToDictionary(need=>need.ItemId,need=>sources(need.ItemId).Where(source=>source.TerritoryId!=0).ToArray());
        var elsewhere=wanted.Where(need=>byItem[need.ItemId].Length==0).ToArray();
        var left=wanted.Where(need=>byItem[need.ItemId].Length>0).ToDictionary(need=>need.ItemId,need=>need.Amount);
        var zones=new List<GatheringRouteZone>();
        while(left.Count>0)
        {
            var best=left.Keys.SelectMany(item=>byItem[item].Select(source=>(source.TerritoryId,source.Zone,Item:item)))
                .GroupBy(entry=>(entry.TerritoryId,entry.Zone))
                .Select(group=>(group.Key.TerritoryId,group.Key.Zone,Items:group.Select(entry=>entry.Item).Distinct().ToArray()))
                .OrderByDescending(zone=>zone.Items.Length).ThenBy(zone=>zone.Zone,StringComparer.CurrentCultureIgnoreCase).ThenBy(zone=>zone.TerritoryId)
                .First();
            var rows=best.Items.Select(item=>new GatheringRouteRow(item,left[item],byItem[item].Where(source=>source.TerritoryId==best.TerritoryId)
                    .OrderBy(source=>source.Timing.Kind==GatheringScheduleKind.Limited ? 1:0).ThenBy(source=>source.Level).ThenBy(source=>source.PointId).First()))
                .OrderBy(row=>row.Source.Place,StringComparer.CurrentCultureIgnoreCase).ThenBy(row=>row.ItemId).ToArray();
            foreach(var item in best.Items)left.Remove(item);
            zones.Add(new(best.TerritoryId,best.Zone,rows));
        }
        return new(zones,elsewhere);
    }
}
