using System.Numerics;
using TenshoAssistant.Maps;

// X-516 (0.1711, the user's request of 16/11): on Tensho's map, a present gathering point has one of four icons (botanist, miner, timed
// botanist, timed miner: the game's GatheringType icons), and a timed point's area its own colour; the icon table's row is the legend.
internal static class GatheringNodes1711Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Gathering nodes 1711: one icon per job and timing, four distinct; spearfishing none", () =>
        {
            Check(GatheringNodeIcons.For(0, false) == 60438 && GatheringNodeIcons.For(1, false) == 60438, "Mining and Quarrying: the miner's pickaxe");
            Check(GatheringNodeIcons.For(2, false) == 60432 && GatheringNodeIcons.For(3, false) == 60432, "Logging and Harvesting: the botanist's leaf");
            Check(GatheringNodeIcons.For(0, true) == 60464 && GatheringNodeIcons.For(1, true) == 60464, "timed miner: one icon whatever the kind");
            Check(GatheringNodeIcons.For(2, true) == 60461 && GatheringNodeIcons.For(3, true) == 60461, "timed botanist: one icon");
            Check(new[] { GatheringNodeIcons.Miner, GatheringNodeIcons.Botanist, GatheringNodeIcons.TimedMiner, GatheringNodeIcons.TimedBotanist }.Distinct().Count() == 4, "four distinct");
            Check(GatheringNodeIcons.For(4, false) == 0 && GatheringNodeIcons.For(5, true) == 0 && GatheringNodeIcons.For(99, false) == 0, "spearfishing or unknown: none (the leaf stays)");
            Check(GatheringNodeIcons.Timed(60464) && GatheringNodeIcons.Timed(60461) && !GatheringNodeIcons.Timed(60438) && !GatheringNodeIcons.Timed(60432) && !GatheringNodeIcons.Timed(0), "timed told from the icon");
        }),
        ("Gathering nodes 1711: a point is timed by its transient row; a timed area has its own colour", () =>
        {
            Check(GatheringNodeIcons.TimedPoint(12, 65535, 65535), "a rare pop table");
            Check(GatheringNodeIcons.TimedPoint(0, 400, 800) && GatheringNodeIcons.TimedPoint(0, 0, 0), "an ephemeral window");
            Check(!GatheringNodeIcons.TimedPoint(0, 65535, 65535) && !GatheringNodeIcons.TimedPoint(0, 65535, 400), "none: not timed");
            var normal = new Vector4(0.55f, 0.90f, 0.55f, 0.20f); var timed = new Vector4(0.95f, 0.40f, 0.75f, 0.28f);
            Check(GatheringNodeIcons.AreaColour(true, normal, timed) == timed && GatheringNodeIcons.AreaColour(false, normal, timed) == normal, "the colour by timing");
            var settings = Read("Configuration.cs");
            Check(settings.Contains("public System.Numerics.Vector4 TimedGatheringAreaColor { get; set; } = new(0.95f,0.40f,0.75f,0.28f);", StringComparison.Ordinal)
                && settings.Contains("public System.Numerics.Vector4 GatheringAreaColor { get; set; } = new(0.55f,0.90f,0.55f,0.20f);", StringComparison.Ordinal), "a default distinct from the normal green");
        }),
        ("Gathering nodes 1711: wiring: the reader gives each present point its icon, the map draws it with the clock, the areas and the legend", () =>
        {
            var reader = Read("Maps/MapLiveReader.Dalamud.cs");
            Check(reader.Contains("GatheringNodeIcons.For(point.GatheringPointBase.Value.GatheringType.RowId,timed)", StringComparison.Ordinal)
                && reader.Contains("kind==MapObjectKind.Gathering ? gatheringIcons.GetValueOrDefault(item.BaseId):0", StringComparison.Ordinal), "the icon of each present point");
            var map = Read("MainWindow.MapView.cs");
            Check(map.Contains("else if(item.Kind is MapObjectKind.IslandAnimal or MapObjectKind.Gathering && item.Icon!=0", StringComparison.Ordinal), "drawn with its game icon");
            Check(map.Contains("if(item.Kind==MapObjectKind.Gathering && GatheringNodeIcons.Timed(item.Icon))", StringComparison.Ordinal), "the clock badge on a timed one");
            Check(map.Contains("GatheringNodeIcons.AreaColour(point.Timed,settings.GatheringAreaColor,settings.TimedGatheringAreaColor)", StringComparison.Ordinal), "the area by timing");
            Check(map.Contains("AreaSetting(\"MapView.AreaGatheringTimed\"", StringComparison.Ordinal), "its setting");
            Check(map.Contains("if(ImGui.IsItemHovered())DrawNearbyGatheringLegend();", StringComparison.Ordinal), "the legend on the icon table's row");
            var en = Read("Localization/Languages/en.json"); var fr = Read("Localization/Languages/fr.json");
            foreach (var key in new[] { "AreaGatheringTimed", "AreaGatheringTimedHelp", "GatheringTimedLabel", "NearbyGatheringLegend", "TimedNode" })
                Check(en.Contains($"    \"{key}\": ", StringComparison.Ordinal) && fr.Contains($"    \"{key}\": ", StringComparison.Ordinal), "text: " + key);
        }),
    ];
}
