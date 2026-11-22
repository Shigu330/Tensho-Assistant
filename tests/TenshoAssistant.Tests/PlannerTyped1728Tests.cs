using TenshoAssistant.FreeCompany;

// X-730 M1 (0.1728, review of 0.1721 to 0.1726): a vessel's speed and range unknown: typing one alone keeps them unknown (said, nothing
// searched with the other one's old value); both typed: known. Read or from the parts: one typed is enough. Behaviour: the airship of 21/11
// (rank 14, everything read at 0) replayed through PlannerStats then the typing.
internal static class PlannerTyped1728Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Planner typed 1728: the airship of 21/11, everything read at 0: one value typed keeps it unknown, both make it known", () =>
        {
            var airship = new VoyageVessel { Kind = VesselKind.Airship, Name = "Airship", Rank = 14 };
            var (_, _, source) = VesselBuilds.PlannerStats(airship, null);
            Check(source == VesselStatsSource.Unknown, "read at 0, no parts: unknown");
            var afterSpeed = VesselBuilds.AfterTyping(source, speedTyped: true, rangeTyped: false);
            Check(afterSpeed == VesselStatsSource.Unknown, "the speed alone: still unknown (the range is still 1 or another vessel's)");
            Check(VesselBuilds.AfterTyping(source, speedTyped: false, rangeTyped: true) == VesselStatsSource.Unknown, "the range alone: still unknown");
            Check(VesselBuilds.AfterTyping(afterSpeed, speedTyped: true, rangeTyped: true) == VesselStatsSource.Read, "both typed: known, searched");
        }),
        ("Planner typed 1728: read or from the parts: one value typed is enough, as before", () =>
        {
            Check(VesselBuilds.AfterTyping(VesselStatsSource.Read, true, false) == VesselStatsSource.Read, "read");
            Check(VesselBuilds.AfterTyping(VesselStatsSource.Parts, false, true) == VesselStatsSource.Read, "from the parts, then typed");
        }),
        ("Planner typed 1728: wiring: both fields, the flags reset when a vessel is chosen, the text says both", () =>
        {
            var planner = Read("MainWindow.Planner.cs");
            Check(planner.Contains("ref inputs.Speed)){inputs.SpeedTyped=true;inputs.StatsFrom=VesselBuilds.AfterTyping(inputs.StatsFrom,inputs.SpeedTyped,inputs.RangeTyped);}", StringComparison.Ordinal)
                && planner.Contains("ref inputs.Range)){inputs.RangeTyped=true;inputs.StatsFrom=VesselBuilds.AfterTyping(inputs.StatsFrom,inputs.SpeedTyped,inputs.RangeTyped);}", StringComparison.Ordinal), "both fields");
            Check(!planner.Contains("))inputs.StatsFrom=VesselStatsSource.Read;Tip", StringComparison.Ordinal), "no field makes it known alone");
            Check(planner.Contains("inputs.Rank=vessel.Rank;inputs.StatsFrom=source;inputs.SpeedTyped=false;inputs.RangeTyped=false;", StringComparison.Ordinal), "reset when a vessel is chosen");
            Check(Read("Localization/Languages/fr.json").Contains("ou saisissez les deux ici.\"", StringComparison.Ordinal) && Read("Localization/Languages/en.json").Contains("or type both here.\"", StringComparison.Ordinal), "en and fr");
        }),
    ];
}
