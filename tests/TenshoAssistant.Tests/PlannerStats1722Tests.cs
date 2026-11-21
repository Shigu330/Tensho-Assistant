using TenshoAssistant.FreeCompany;

// X-718 (0.1722, the user's report of 21/11): the route planner never takes 1 for an unknown speed or range: the read stats, else the read
// parts, else unknown (said); a vessel read with no stat and no part keeps the ones saved for it (behaviour tests, R28.14: the reading of
// 21/11, the user's airship at rank 14 with everything else at 0).
internal static class PlannerStats1722Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");

    // Airship parts (AirshipExplorationPart rows, slots 0 hull, 1 rigging, 2 forecastle, 3 aftcastle) with made-up stats; a submarine set.
    private static VesselPart Part(VesselKind kind, uint row, int slot, int speed, int range) => new(row, kind, 1, slot, 1, 5, 1, new(10, 10, speed, range, 10), 21000 + row, $"P{row}");
    private static readonly VesselPartCatalog Catalog = new()
    {
        Parts = new[]
        {
            Part(VesselKind.Airship, 1, 0, 30, 20), Part(VesselKind.Airship, 2, 1, 20, 15), Part(VesselKind.Airship, 3, 2, 25, 10), Part(VesselKind.Airship, 4, 3, 15, 25),
            Part(VesselKind.Submarine, 1, 0, 20, 40), Part(VesselKind.Submarine, 2, 1, 60, 30), Part(VesselKind.Submarine, 3, 2, 10, -20), Part(VesselKind.Submarine, 4, 3, 20, 20),
        }.ToDictionary(part => (part.Kind, part.Row)),
        SubmarineRanks = [new(1, 20, 6180, new(1, 2, 3, 4, 5))],
        AirshipRanks = [new(1, 12, 6180, default)],
        Breakpoints = new Dictionary<uint, SectorBreakpoint>(),
        SubmarineStatNames = ["a", "b", "c", "d", "e"], AirshipStatNames = ["a", "b", "c", "d", "e"],
    };

    private static VoyageVessel Airship(int speed, int range, params uint[] parts) => new() { HouseId = 9, Kind = VesselKind.Airship, Slot = 1, Name = "Aéronef-1", Rank = 14, Speed = speed, Range = range, Parts = [.. parts] };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Planner stats 1722: read stats, else the parts read, else unknown: never 1", () =>
        {
            Check(VesselBuilds.PlannerStats(Airship(110, 70, 1, 2, 3, 4), Catalog) == (110, 70, VesselStatsSource.Read), "read: the read values");
            Check(VesselBuilds.PlannerStats(Airship(0, 0, 1, 2, 3, 4), Catalog) == (90, 70, VesselStatsSource.Parts), "speed and range at 0, the parts known: their sum (airships: no rank bonus)");
            Check(VesselBuilds.PlannerStats(Airship(80, 0, 1, 2, 3, 4), Catalog).Source == VesselStatsSource.Parts, "one of them at 0: from the parts");
            var unknown = VesselBuilds.PlannerStats(Airship(0, 0, 0, 0, 0, 0), Catalog);
            Check(unknown == (0, 0, VesselStatsSource.Unknown), "the user's airship of 21/11 (rank 14, everything at 0): unknown, not 1");
            Check(VesselBuilds.PlannerStats(Airship(0, 0, 1, 2, 0, 4), Catalog).Source == VesselStatsSource.Unknown, "a part missing: no partial sum");
            Check(VesselBuilds.PlannerStats(Airship(0, 0, 1, 2, 3, 4), null).Source == VesselStatsSource.Unknown, "no part catalog: unknown");
            Check(VesselBuilds.PlannerStats(Airship(0, 0), Catalog).Source == VesselStatsSource.Unknown, "no part list (saved before 0.315): unknown");
            var submarine = new VoyageVessel { Kind = VesselKind.Submarine, Slot = 1, Rank = 1, Parts = [1, 2, 3, 4] };
            Check(VesselBuilds.PlannerStats(submarine, Catalog) == (113, 74, VesselStatsSource.Parts), "submarines too, the rank's bonus included (110 + 3, 70 + 4)");
        }),
        ("Planner stats 1722: a vessel read with no stat and no part keeps the saved ones; a real reading replaces them", () =>
        {
            var known = Airship(110, 70, 1, 2, 3, 4) with { Surveillance = 30, Retrieval = 110, Favor = 70 };
            var empty = Airship(0, 0, 0, 0, 0, 0);
            Check(Voyages.NoStats(empty) && !Voyages.NoStats(known), "no stat, no part");
            var kept = Voyages.KeepKnownStats([known], [empty]).Single();
            Check(kept.Speed == 110 && kept.Range == 70 && kept.Surveillance == 30 && kept.Favor == 70 && kept.Parts.SequenceEqual(new uint[] { 1, 2, 3, 4 }) && kept.Rank == 14, "kept");
            Check(!ReferenceEquals(kept.Parts, known.Parts), "a copy of the parts, not the saved list");
            var merged = Voyages.Merge([known], 9, Voyages.KeepKnownStats([known], [empty]));
            Check(merged.Single().Speed == 110 && VesselBuilds.PlannerStats(merged.Single(), Catalog).Source == VesselStatsSource.Read, "after the workshop's merge: still known for the planner");
            var changed = Airship(95, 60, 1, 2, 3, 4);
            Check(Voyages.KeepKnownStats([known], [changed]).Single() == changed, "a real reading replaces them");
            Check(Voyages.NoStats(Voyages.KeepKnownStats([known], [empty with { Name = "Other" }]).Single()), "another vessel in the slot (another name): nothing kept");
            Check(Voyages.NoStats(Voyages.KeepKnownStats([known with { HouseId = 3 }], [empty]).Single()), "another house: nothing kept");
            Check(Voyages.NoStats(Voyages.KeepKnownStats(null, [empty]).Single()), "nothing saved: still nothing");
        }),
        ("Planner stats 1722: wiring: the planner, the watcher's probe and keeping, both kinds, the texts", () =>
        {
            var planner = Read("MainWindow.Planner.cs");
            Check(!planner.Contains("Math.Max(1,vessel.Speed)", StringComparison.Ordinal) && planner.Contains("var (speed,range,source)=VesselBuilds.PlannerStats(vessel,VesselParts?.Invoke());", StringComparison.Ordinal), "no 1 in silence");
            Check(planner.Contains("if(inputs.StatsFrom==VesselStatsSource.Unknown)StateLine(UI.UiState.Waiting,localization.Get(\"RoutePlanner.StatsUnknown\"));", StringComparison.Ordinal)
                && planner.Contains("OriginLine(UI.UiOrigin.Estimated,localization.Get(\"RoutePlanner.StatsFromParts\"));", StringComparison.Ordinal), "unknown and from the parts: said");
            Check(planner.Contains("if(ImGui.InputInt(localization.Get(\"RoutePlanner.Speed\")+\"##PlannerSpeed\",ref inputs.Speed)){inputs.SpeedTyped=true;inputs.StatsFrom=VesselBuilds.AfterTyping(", StringComparison.Ordinal), "typed: known (both, when unknown: X-730 M1)");
            foreach (var file in new[] { "MainWindow.AirshipPlanner.cs", "MainWindow.RoutePlanner.cs" })
                Check(Read(file).Contains("if(inputs.StatsFrom==VesselStatsSource.Unknown)return;", StringComparison.Ordinal), file + ": nothing searched while unknown");
            var watcher = Read("FreeCompany/VoyageWatcher.Dalamud.cs");
            Check(watcher.Contains("SayStats(airRead[^1],air.HullId,air.SternId,air.BowId,air.BridgeId);", StringComparison.Ordinal) && watcher.Contains("SayStats(read[^1],sub.HullId,sub.SternId,sub.BowId,sub.BridgeId);", StringComparison.Ordinal)
                && watcher.Contains("if(Developer?.Invoke()!=true)return;", StringComparison.Ordinal), "the probe, developer mode only, both kinds");
            Check(watcher.IndexOf("read=Voyages.KeepKnownStats(configuration.Voyages,read);", StringComparison.Ordinal) is > 0 and var keep && keep < watcher.IndexOf("var merged=Voyages.Merge(configuration.Voyages,houseId,read);", StringComparison.Ordinal), "kept before the merge");
            Check(Read("Plugin.Voyages.cs").Contains("Developer=()=>configuration.DeveloperMode,", StringComparison.Ordinal), "wired");
            Check(Read("Localization/Languages/fr.json").Contains("\"StatsUnknown\": \"Vitesse et distance de ce vaisseau non lues", StringComparison.Ordinal)
                && Read("Localization/Languages/en.json").Contains("\"StatsFromParts\": \"Speed and range from its parts read in the workshop.\"", StringComparison.Ordinal), "en and fr");
        }),
    ];
}
