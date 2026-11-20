using System.Numerics;
using TenshoAssistant.FreeCompany;

// 0.1568 (X-610, the user's report of 19/11): « How to get it » lists the airship and submarine sectors where an item may be found, from the
// loot Tensho carries; Alchemic Fibre (12926) no longer says « no known source ». The sectors' names come from the game's sheets in the plugin;
// here they are stand-ins, the loot is the embedded files themselves.
internal static class VoyageItemSources1568Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private const uint Fibre = 12926;

    private static AirshipCatalog Airships()
    {
        using var reader = new StreamReader(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/airship-drop.csv"));
        var drops = AirshipSectors.ReadDrops(reader);
        var sectors = drops.Keys.Order().Select(id => new AirshipSector(id, "Point " + id, "P" + id, 1, 1, 1, 1, 1, null)).ToArray();
        return new AirshipCatalog { Sectors = sectors, Drops = drops };
    }

    private static SubmarineCatalog Submarines()
    {
        using var stream = File.OpenRead(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/submarine-loot.json"));
        var (loot, _, _) = VoyagePlanner.ReadLoot(stream);
        var sectors = loot.Keys.ToDictionary(id => id, id => new SubmarineSector(id, 1 + id / 100, false, 1, Vector3.Zero, 1, 1, 1, "Sector " + id));
        var maps = sectors.Values.Select(sector => sector.Map).Distinct().Select(map => (map, "Sea " + map)).ToArray();
        return new SubmarineCatalog { Sectors = sectors, Maps = maps, Loot = loot, SellPrices = new Dictionary<uint, uint>(), LootSource = "test", LootBuilt = "test" };
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Voyage sources 1568: Alchemic Fibre (12926) is found by airship at points 10 and 11, by name", () =>
        {
            var names = VoyageItemSources.Airships(Airships(), Fibre);
            Check(names.Contains("Point 10") && names.Contains("Point 11"), "airship points 10 and 11 missing: " + string.Join(", ", names));
        }),
        ("Voyage sources 1568: Alchemic Fibre (12926) is found by submarine, each sector with its sea", () =>
        {
            var names = VoyageItemSources.Submarines(Submarines(), Fibre);
            Check(names.Count > 0, "no submarine sector for 12926");
            Check(names.All(name => name.StartsWith("Sector ", StringComparison.Ordinal) && name.Contains(" (Sea ")), "a sector without its name or sea: " + string.Join(", ", names));
            Check(names.Count == names.Distinct().Count(), "a sector listed twice");
        }),
        ("Voyage sources 1568: an item found on no voyage gives nothing, as do missing catalogs", () =>
        {
            Check(VoyageItemSources.Airships(Airships(), 9999999).Count == 0 && VoyageItemSources.Submarines(Submarines(), 9999999).Count == 0, "an unknown item found on a voyage");
            Check(VoyageItemSources.Airships(null, Fibre).Count == 0 && VoyageItemSources.Submarines(null, Fibre).Count == 0, "no catalog: nothing");
        }),
        ("Voyage sources 1568: the voyages count as a known source in « How to get it »", () =>
        {
            var code = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MarketHover.cs"));
            var voyages = code.IndexOf("known+=DrawVoyageSources(id);", StringComparison.Ordinal);
            var fallback = code.IndexOf("if(known==0)DrawFallbackSources(id);", Math.Max(0, voyages), StringComparison.Ordinal);
            Check(voyages > 0 && fallback > voyages, "the voyages are not counted before the « no known source » fallback");
        }),
    ];
}
