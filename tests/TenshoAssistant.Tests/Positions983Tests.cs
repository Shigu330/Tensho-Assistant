using System.Text.RegularExpressions;

// X-111 (0.983, user request): wherever a page shows a position, the common place menu (flag, nearest aetheryte, map, weather) goes with it.
internal static class Positions983Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Positions 983: every page that writes coordinates offers the place menu or the nearest aetheryte",() =>
        {
            var pages=Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs");
            var missing=pages.Where(file=>Regex.IsMatch(File.ReadAllText(file),@"MapProjection\.(CoordinateText|CoordinateNumber|CoordinateFromWorld)\(")
                && !Regex.IsMatch(File.ReadAllText(file),@"PlaceMenu\(|NpcMenu\(|AskNearestAetheryteAt\(|PlaceEntries\(")).Select(Path.GetFileName).ToArray();
            Check(missing.Length==0,"positions without the place menu: "+string.Join(", ",missing));
        }),
        ("Positions 983: the community's « x, y » of an S rank read as map coordinates, anything else left out",() =>
        {
            Check(TenshoAssistant.Hunting.SRankConditions.PlaceOf("14, 23.5") is { X:14, Y:23.5f },"place");
            Check(TenshoAssistant.Hunting.SRankConditions.PlaceOf("north of the lake")==null && TenshoAssistant.Hunting.SRankConditions.PlaceOf("0, 50")==null,"not a place");
        }),
    ];
}
