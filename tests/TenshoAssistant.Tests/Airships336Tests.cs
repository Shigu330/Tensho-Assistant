using TenshoAssistant.FreeCompany;

// 0.336 (DEC-023, point 3): the airship unlock tree and possible loot of LuminaSupplemental (GPL-3.0) are embedded with their notice,
// read as published, and shown without quantity or value.
internal static class Airships336Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Data(string file) => Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/" + file);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Airships: embedded unlock tree and drops carry source, commit, hash and GPL-3.0 notice, and read as published",() =>
        {
            foreach (var file in new[] { "airship-unlock.csv","airship-drop.csv" })
            {
                var text = File.ReadAllText(Data(file));
                Check(text.Contains("bd265c2b39868387937ae5b0c038593af997e0f9",StringComparison.Ordinal) && text.Contains("GPL-3.0",StringComparison.Ordinal)
                    && text.Contains("SHA-256",StringComparison.Ordinal),file + ": notice");
            }
            using var unlockFile = new StreamReader(Data("airship-unlock.csv"));
            var unlocks = AirshipSectors.ReadUnlocks(unlockFile);
            Check(unlocks.Count == 24 && !unlocks.ContainsKey(22),"24 points, 22 (old Diadem) absent");
            Check(unlocks[3] == new AirshipUnlock(1,0,7) && unlocks[16] == new AirshipUnlock(15,90,50),"rows kept as published");
            using var dropFile = new StreamReader(Data("airship-drop.csv"));
            var drops = AirshipSectors.ReadDrops(dropFile);
            Check(drops.Values.Sum(items => items.Count) > 200 && drops[0].Contains(12224u),"drops read, point 0 has item 12224");
        }),
        ("Airships: the unlock path walks back to a starting sector and stops on loops or unknown links",() =>
        {
            AirshipSector Sector(uint id,AirshipUnlock? unlock) => new(id,$"S{id}",$"S{id}",1,1,13,180,0,unlock);
            var catalog = new AirshipCatalog
            {
                Sectors = [Sector(0,new(0,0,1)),Sector(1,new(0,0,1)),Sector(3,new(1,0,7)),Sector(5,new(9,0,10)),Sector(6,new(7,0,1)),Sector(7,new(6,0,1))],
                Drops = new Dictionary<uint,IReadOnlyList<uint>>(),
            };
            Check(AirshipSectors.UnlockPath(catalog,3).SequenceEqual([0u,1u,3u]),"3 ← 1 ← 0");
            Check(AirshipSectors.UnlockPath(catalog,0).SequenceEqual([0u]),"a sector unlocking itself starts the tree");
            Check(AirshipSectors.UnlockPath(catalog,5).SequenceEqual([5u]),"unknown link: stops");
            Check(AirshipSectors.UnlockPath(catalog,6).SequenceEqual([7u,6u]),"loop: stops");
        }),
        ("Airships: the page shows the loot as a list without value, and the embedded files are credited",() =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.AirshipSectors.cs"));
            Check(page.Contains("AirshipSectors.LootTitle",StringComparison.Ordinal) && !page.Contains("PriceLow",StringComparison.Ordinal),"no value computed");
            Check(page.Contains("airshipViewKey!=key",StringComparison.Ordinal),"filtered only when an input changes");
            var credits = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Credits/CreditCatalog.cs"));
            Check(credits.Contains("\"FreeCompany/Data/airship-unlock.csv\", \"FreeCompany/Data/airship-drop.csv\"",StringComparison.Ordinal),"credited");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.336-airship-sectors.md")).Contains("GetAirshipVoyageTimeAndDistance",StringComparison.Ordinal),"planner left for a later version, evidence says why");
        }),
    ];
}
