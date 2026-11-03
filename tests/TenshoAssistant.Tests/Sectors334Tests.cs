using TenshoAssistant.FreeCompany;

// 0.334 (DEC-023, point 1): the community breakpoint table is kept; its 19 sectors below every record are flagged « not confirmed ».
internal static class Sectors334Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Breakpoints: the 19 sectors below every record are flagged, all present in the embedded table, and only those",() =>
        {
            Check(VesselBuilds.UnconfirmedSectors.Count == 19,"19 sectors (docs/evidence/0.315-vessel-builds.md)");
            using var reader = new StreamReader(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/submarine-breakpoints.csv"));
            var table = VesselBuilds.ReadBreakpoints(reader);
            Check(VesselBuilds.UnconfirmedSectors.All(table.ContainsKey),"each flagged sector is in the table");
            Check(VesselBuilds.Unconfirmed([1,2,81]) && !VesselBuilds.Unconfirmed([1,2,3]) && !VesselBuilds.Unconfirmed([]),"a route is flagged when one of its sectors is");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.315-vessel-builds.md")).Contains("81, 86, 92, 98, 105, 109",StringComparison.Ordinal),"list taken from the evidence");
        }),
    ];
}
