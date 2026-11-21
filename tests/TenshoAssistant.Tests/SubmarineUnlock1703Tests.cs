using System.Numerics;
using TenshoAssistant.FreeCompany;

// 0.1703 (X-200, the user's request of 12/11): submarines: the way to unlock a sector aimed at, as for airships (LuminaSupplemental's
// SubmarineUnlock table embedded; docs/evidence/0.1703-submarine-unlocks.md).
internal static class SubmarineUnlock1703Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static IReadOnlyDictionary<uint, SubmarineUnlock> Embedded() => SubmarineUnlocks.Read(new StringReader(Read("FreeCompany/Data/submarine-unlock.csv")));
    private static Dictionary<uint, SubmarineSector> Sectors(IEnumerable<uint> ids)
        => ids.ToDictionary(id => id, id => new SubmarineSector(id, 1, false, 1, Vector3.Zero, 10, 60, 100, "S" + id));
    private static string Text(IReadOnlyList<uint> ids) => string.Join(",", ids);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Submarine unlock 1703: two known chains of the embedded table (sector 8: explore 2, 4, 7; sector 17: explore 2, 5, 9, 11, 16)", () =>
        {
            var unlocks = Embedded();
            Check(unlocks.Count >= 140 && unlocks[3] == new SubmarineUnlock(1, 4) && unlocks[1].UnlockedBy == 0 && unlocks[2].UnlockedBy == 0, "read: " + unlocks.Count);
            var sectors = Sectors(unlocks.Keys);
            Check(Text(SubmarineUnlocks.Path(sectors, unlocks, 8)) == "2,4,7,8", "chain of 8: " + Text(SubmarineUnlocks.Path(sectors, unlocks, 8)));
            var (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 8, null);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "2,4,7", "8, nothing read: the whole chain");
            (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 17, null);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "2,5,9,11,16", "17: " + Text(explore));
            (state, explore) = SubmarineUnlocks.Route(sectors, unlocks, 17, [1, 2, 5, 9]);
            Check(state == SubmarineUnlockState.Path && Text(explore) == "9,11,16", "17, 9 already unlocked: from 9: " + Text(explore));
        }),
        ("Submarine unlock 1703: unknown, open from the start or already unlocked said as such; a loop or a missing link ends the chain", () =>
        {
            var unlocks = new Dictionary<uint, SubmarineUnlock> { [1] = new(0, 1), [3] = new(1, 4), [4] = new(3, 7), [6] = new(7, 9), [7] = new(6, 9), [8] = new(99, 9) };
            var sectors = Sectors([1, 3, 4, 6, 7, 8, 50]);
            Check(SubmarineUnlocks.Route(sectors, unlocks, 50, null).State == SubmarineUnlockState.Unknown, "not in the table");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 123, null).State == SubmarineUnlockState.Unknown, "not a sector");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 1, null).State == SubmarineUnlockState.Open, "open from the start");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 4, [1, 3, 4]).State == SubmarineUnlockState.Unlocked, "already unlocked");
            Check(Text(SubmarineUnlocks.Route(sectors, unlocks, 4, [1, 3]).Explore) == "3", "its unlocker unlocked: explore it only");
            Check(Text(SubmarineUnlocks.Route(sectors, unlocks, 4, []).Explore) == "1,3", "nothing unlocked yet: from the first");
            Check(Text(SubmarineUnlocks.Path(sectors, unlocks, 6)) == "7,6", "a loop ends");
            Check(SubmarineUnlocks.Route(sectors, unlocks, 8, null).State == SubmarineUnlockState.Unknown, "its unlocker is not a sector: unknown");
        }),
        ("Submarine unlock 1703: wiring: embedded with its notice and credited, in the catalog, a target in the route planner, en and fr", () =>
        {
            var csv = Read("FreeCompany/Data/submarine-unlock.csv");
            Check(csv.Contains("GNU General Public License v3.0", StringComparison.Ordinal) && csv.Contains("bd265c2b39868387937ae5b0c038593af997e0f9", StringComparison.Ordinal), "notice and source");
            Check(Read("TenshoAssistant.csproj").Contains("<EmbeddedResource Include=\"FreeCompany\\Data\\submarine-unlock.csv\" />", StringComparison.Ordinal), "embedded");
            Check(Read("FreeCompany/VoyageWatcher.Dalamud.cs").Contains("Unlocks=unlocks};", StringComparison.Ordinal), "in the catalog");
            Check(Read("Credits/CreditCatalog.cs").Contains("\"FreeCompany/Data/submarine-unlock.csv\"", StringComparison.Ordinal), "credited");
            var planner = Read("MainWindow.RoutePlanner.cs");
            Check(planner.Contains("DrawSubmarineUnlockTarget(catalog,unlockedList);", StringComparison.Ordinal) && planner.Contains("SubmarineUnlocks.Route(catalog.Sectors,catalog.Unlocks,plannerUnlockTarget,unlockedList)", StringComparison.Ordinal), "the planner");
            foreach (var state in new[] { "Unknown", "Open", "Unlocked" })
                Check(Read("Localization/Languages/en.json").Contains("\"Unlock" + state + "\":", StringComparison.Ordinal) && Read("Localization/Languages/fr.json").Contains("\"Unlock" + state + "\":", StringComparison.Ordinal), state);
            Check(Read("FreeCompany/SubmarineUnlocks.cs").Contains("docs/evidence/0.1703-submarine-unlocks.md", StringComparison.Ordinal), "the evidence cited");
        }),
    ];
}
