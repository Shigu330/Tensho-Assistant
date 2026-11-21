using System.Numerics;
using TenshoAssistant.FreeCompany;

// X-712 (review of 0.1704-0.1708), minor M2: with the goal « Unlock a sector », each submarine of « Your submersibles » gets the next sector
// to explore from its own company's unlocked sectors, not the chosen submarine's company's.
internal static class ReviewMinors1709Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static SubmarineSector Sector(uint id, bool start) => new(id, 1, start, 1, new Vector3(id * 100, 0, 0), 10, 60, 100, "S" + id);
    private static string Text(IEnumerable<uint> ids) => string.Join(",", ids);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1709: the sector a submarine visits to unlock a target follows its own company's unlocked sectors", () =>
        {
            // 1 opens 2, 2 opens 3, 3 opens 4; 1 is the start.
            var sectors = new Dictionary<uint, SubmarineSector> { [1] = Sector(1, true), [2] = Sector(2, false), [3] = Sector(3, false), [4] = Sector(4, false) };
            var unlocks = new Dictionary<uint, SubmarineUnlock> { [1] = new(0, 1), [2] = new(1, 1), [3] = new(2, 5), [4] = new(3, 10) };
            var (state, visit) = PlannerTargets.UnlockVisitFor(sectors, unlocks, 4, [1u, 2u]);
            Check(state == SubmarineUnlockState.Path && Text(visit) == "2", "a company that unlocked 2 explores 2 next: " + Text(visit));
            (state, visit) = PlannerTargets.UnlockVisitFor(sectors, unlocks, 4, [1u, 2u, 3u]);
            Check(state == SubmarineUnlockState.Path && Text(visit) == "3", "another company, further on, explores 3: " + Text(visit));
            (state, visit) = PlannerTargets.UnlockVisitFor(sectors, unlocks, 4, [1u, 2u, 3u, 4u]);
            Check(state == SubmarineUnlockState.Unlocked && visit.Count == 0, "a company that already has the target: nothing to explore, said unlocked");
            (state, visit) = PlannerTargets.UnlockVisitFor(sectors, unlocks, 4, null);
            Check(state == SubmarineUnlockState.Path && Text(visit) == "1", "a company not read: the whole chain, from its first sector");
            Check(PlannerTargets.UnlockVisitFor(sectors, unlocks, 9, null).State == SubmarineUnlockState.Unknown, "an unknown target: unknown");
        }),
        ("Review minors 1709: wiring: the fleet asks each submarine's own targets for the unlock goal and says why one has none", () =>
        {
            var source = Read("MainWindow.RoutePlanner.cs");
            Check(source.Contains("PlannerTargets.UnlockVisitFor(catalog.Sectors,catalog.Unlocks,unlockTarget,Voyages.UnlockedFor(configuration.SubmarineUnlocked,sub.HouseId))", StringComparison.Ordinal),
                "each submarine's company's unlocked sectors");
            Check(source.Contains("1,token,ship.Excluded,ship.Target.Targets).FirstOrDefault()", StringComparison.Ordinal), "the search uses the submarine's own targets");
            Check(source.Contains("string.Join(\"+\",ship.Target.Targets)", StringComparison.Ordinal), "the submarine's own targets are in the search key");
            Check(source.Contains("\"RoutePlanner.Unlock\"+own", StringComparison.Ordinal), "the reason when it has nothing to explore");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"Localization/Languages/{language}.json");
                Check(text.Contains(language == "en" ? "from its own company's unlocked sectors" : "d'après les secteurs débloqués de sa propre compagnie", StringComparison.Ordinal), "FleetTip says it: " + language);
            }
        }),
    ];
}
