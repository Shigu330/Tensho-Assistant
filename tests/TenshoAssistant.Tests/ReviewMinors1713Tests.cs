using TenshoAssistant.Gathering;
using TenshoAssistant.Maps;

// X-714 (0.1713, review of block 0.1709 to 0.1712): M1, a present gathering point is timed by the gathering catalogue's own rule (one function,
// GatheringSchedule.FromTransient, read by both); M2, the timed areas' switch sits under « Gathering areas » (off: no area at all).
internal static class ReviewMinors1713Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1713: M1, timed is the catalogue's schedule; the review's edge cases agree", () =>
        {
            var zeroWindow = GatheringSchedule.FromTransient(4, 0, 0, 0, null);
            Check(zeroWindow.Kind == GatheringScheduleKind.Unrestricted && !zeroWindow.Timed, "a window 0-0: no restriction, not timed (the review's case)");
            var oneBound = GatheringSchedule.FromTransient(4, 0, 65535, 400, null);
            Check(oneBound.Kind == GatheringScheduleKind.Invalid && !oneBound.Timed, "a single bound at 65535: invalid, not timed");
            Check(GatheringSchedule.FromTransient(4, 0, 0, 0, null).Key == GatheringSchedule.FromEphemeral(0, 0).Key
                && GatheringSchedule.FromTransient(5, 0, 1200, 1400, null).Key == GatheringSchedule.FromEphemeral(1200, 1400).Key, "no rare table: the ephemeral window, as before");
            var rare = GatheringSchedule.FromTransient(2, 7, 65535, 65535, [(800, 200), (2000, 200), (65535, 0)]);
            Check(rare.Timed && rare.Windows.Count == 2 && rare.Key == GatheringSchedule.FromRare([(800, 200), (2000, 200), (65535, 0)]).Key, "a rare pop table: its slots, as before");
            Check(GatheringSchedule.FromTransient(2, 7, 65535, 65535, null) == GatheringSchedule.Unknown, "a rare table whose row is missing: unknown, not timed");
            Check(GatheringSchedule.FromTransient(8, 0, 400, 800, null) == GatheringSchedule.Unknown, "point type 8: unknown, as the catalogue left it");
            Check(!GatheringSchedule.Unknown.Timed && !GatheringSchedule.FromTransient(1, 0, 65535, 65535, null).Timed, "unknown or unrestricted: not timed");
            var point = new MapLocalGatheringPoint(1, "Mining", 50, "", [], false, null, 0, rare);
            Check(point.Timed == rare.Timed && !(point with { Schedule = zeroWindow }).Timed && !(point with { Schedule = null }).Timed, "the map's point reads the same property");
        }),
        ("Review minors 1713: M1 wiring: the catalogue and the map's present points call the same rule", () =>
        {
            var catalogue = Read("Gathering/GatheringCatalogLoader.Dalamud.cs");
            Check(catalogue.Contains("return GatheringSchedule.FromTransient(point.Type, timing.GatheringRarePopTimeTable.RowId, timing.EphemeralStartTime, timing.EphemeralEndTime, slots);", StringComparison.Ordinal), "catalogue");
            Check(!catalogue.Contains("point.Type == 8 ||", StringComparison.Ordinal), "type 8 in the shared rule only");
            var reader = Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("TenshoAssistant.Gathering.GatheringSchedule.FromTransient(point.Type,timing.GatheringRarePopTimeTable.RowId,timing.EphemeralStartTime,timing.EphemeralEndTime,", StringComparison.Ordinal)
                && reader.Contains("? RareSlots(rare):null).Timed;", StringComparison.Ordinal), "the map's present points");
            Check(!Read("Maps/GatheringNodeIcons.cs").Contains("public static bool TimedPoint(", StringComparison.Ordinal), "the old separate rule is gone");
            Check(Read("Maps/MapLocalIndex.cs").Contains("public bool Timed=>Schedule?.Timed==true;", StringComparison.Ordinal), "the map's catalogue points: same property");
        }),
        ("Review minors 1713: M2, the timed areas follow « Gathering areas »", () =>
        {
            Check(GatheringNodeIcons.AreaShown(false, true, true) && GatheringNodeIcons.AreaShown(true, true, true), "both on: both shown");
            Check(!GatheringNodeIcons.AreaShown(true, false, true) && !GatheringNodeIcons.AreaShown(false, false, true), "gathering areas off: no timed area either (the review's case)");
            Check(GatheringNodeIcons.AreaShown(false, true, false) && !GatheringNodeIcons.AreaShown(true, true, false), "timed off: normal areas only");
            Check(!GatheringNodeIcons.AreaShown(true, false, false) && !GatheringNodeIcons.AreaShown(false, false, false), "both off: none");
            var map = Read("MainWindow.MapView.cs");
            Check(map.Contains("            if(settings.ShowGatheringAreas)\n                foreach(var point in gathering)\n                    if(point.Position is { Radius:>0 } area && GatheringNodeIcons.AreaShown(point.Timed,settings.ShowGatheringAreas,settings.ShowTimedGatheringAreas))", StringComparison.Ordinal), "the areas drawn");
            Check(map.Contains("worldRadius:GatheringNodeIcons.AreaShown(point.Timed,settings.ShowGatheringAreas,settings.ShowTimedGatheringAreas) ? spot.Radius:0);", StringComparison.Ordinal), "the hover inside an area");
            Check(!map.Contains("point.Timed ? settings.ShowTimedGatheringAreas:settings.ShowGatheringAreas", StringComparison.Ordinal), "no independent switch left");
            Check(map.Contains("        ImGui.Indent();\n        using(ImRaii.Disabled(!settings.ShowGatheringAreas))\n            changed|=AreaSetting(\"MapView.AreaGatheringTimed\"", StringComparison.Ordinal), "the setting under « Gathering areas », greyed while off");
            Check(Read("Localization/Languages/fr.json").Contains("affichées ou non (seulement quand les zones de récolte le sont)", StringComparison.Ordinal)
                && Read("Localization/Languages/en.json").Contains("shown or not (only while the gathering areas are)", StringComparison.Ordinal), "its help says so");
        }),
    ];
}
