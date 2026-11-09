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
            Check(!System.Text.RegularExpressions.Regex.IsMatch(map,@"GatherAccess\?\.Invoke\(point\.BaseId\)(?!:null)") || map.Contains("IsGatheringPointBase(point) ? GatherAccess?.Invoke(point.BaseId):null"),"one guarded access");
        }),
    ];
}
