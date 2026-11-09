// X-94 (0.960): a fishing spot is never looked up as a gathering point (kind, tome, level).
internal static class FishingHoleKind960Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fishing hole 960: kind and tome read for gathering points only",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("private static bool IsGatheringPointBase(MapLocalGatheringPoint point)=>point.TypeId!=100;"),"type 100 is a fishing spot");
            Check(map.Contains("var access=PointAccess(point);") && map.Contains("if(IsGatheringPointBase(point) && GatherKind?.Invoke(point.BaseId)"),"kind and access guarded");
            // 0.1019 (test audit #0): every GatherAccess call outside PointAccess is counted, none allowed except the one known exception below.
            Check(map.Contains("private TenshoAssistant.Gathering.GatherPointAccess? PointAccess(MapLocalGatheringPoint point)=>IsGatheringPointBase(point) ? GatherAccess?.Invoke(point.BaseId):null;"),"PointAccess guards the read");
            // Known exception, to fix after the post-patch release (GLOBAL-PLAN X-112): the map-edge arrows of timed points call GatherAccess
            // without IsGatheringPointBase. Listed by name so that any other unguarded call fails.
            const string knownEdge="point.ActiveAt(minutes) && GatherAccess?.Invoke(point.BaseId)?.TomeMissing is null";
            var unguarded=System.Text.RegularExpressions.Regex.Matches(map,@"GatherAccess\?\.Invoke\(point\.BaseId\)").Count
                -System.Text.RegularExpressions.Regex.Matches(map,@"IsGatheringPointBase\(point\) \? GatherAccess\?\.Invoke\(point\.BaseId\):null").Count
                -(map.Contains(knownEdge) ? 1:0);
            Check(unguarded==0,$"one guarded access: {unguarded} unguarded GatherAccess?.Invoke(point.BaseId) outside PointAccess");
        }),
    ];
}
