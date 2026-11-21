using TenshoAssistant.FreeCompany;

// X-713 (0.1712, finding of relay 158, as X-711 M3 for submarines): an airship unlock chain cut by an unknown link or a loop is said unknown,
// never shown as whole; every chain of the embedded table goes back to the starting sector.
internal static class AirshipChain1712Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string name) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + name)).ReplaceLineEndings("\n");
    private static AirshipSector Sector(uint id, AirshipUnlock? unlock) => new(id, $"S{id}", $"S{id}", 1, 1, 13, 180, 0, unlock);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Airship chain 1712: whole back to a starting sector; cut by an unknown link or a loop: not whole", () =>
        {
            var catalog = new AirshipCatalog
            {
                Sectors = [Sector(0, new(0, 0, 1)), Sector(1, new(0, 0, 1)), Sector(3, new(1, 0, 7)), Sector(5, new(9, 0, 10)), Sector(6, new(7, 0, 1)), Sector(7, new(6, 0, 1)), Sector(8, null), Sector(10, new(8, 0, 1))],
                Drops = new Dictionary<uint, IReadOnlyList<uint>>(),
            };
            Check(AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, 3)), "3 ← 1 ← 0: whole");
            Check(AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, 0)), "the starting sector itself: whole");
            Check(!AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, 5)), "a link missing from the sectors (9): cut");
            Check(!AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, 6)), "a loop (6 ↔ 7): cut");
            Check(!AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, 10)), "a sector without an unlock row (8): cut");
            Check(!AirshipSectors.Whole(catalog, []), "nothing: not whole");
        }),
        ("Airship chain 1712: every chain of the embedded table is whole", () =>
        {
            using var unlockFile = new StreamReader(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/airship-unlock.csv"));
            var unlocks = AirshipSectors.ReadUnlocks(unlockFile);
            var catalog = new AirshipCatalog { Sectors = [.. unlocks.Select(pair => Sector(pair.Key, pair.Value))], Drops = new Dictionary<uint, IReadOnlyList<uint>>() };
            var cut = unlocks.Keys.Where(id => !AirshipSectors.Whole(catalog, AirshipSectors.UnlockPath(catalog, id))).ToArray();
            Check(cut.Length == 0, "every sector goes back to sector 0: " + string.Join(",", cut));
        }),
        ("Airship chain 1712: wiring: the planner's goal and the sectors table's tooltip say a cut chain unknown", () =>
        {
            Check(Read("MainWindow.AirshipPlanner.cs").Contains("AirshipSectors.Whole(catalog,path) ? string.Join(\" › \",path.Select(SectorName)):localization.Get(\"AirshipSectors.PathUnknown\")", StringComparison.Ordinal), "planner");
            Check(Read("MainWindow.AirshipSectors.cs").Contains("AirshipSectors.Whole(catalog,unlockPath)", StringComparison.Ordinal), "sectors table");
            Check(Read("Localization/Languages/en.json").Contains("    \"PathUnknown\": ", StringComparison.Ordinal) && Read("Localization/Languages/fr.json").Contains("    \"PathUnknown\": ", StringComparison.Ordinal), "text");
        }),
    ];
}
