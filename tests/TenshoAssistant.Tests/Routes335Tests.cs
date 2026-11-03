using TenshoAssistant.FreeCompany;

// 0.335 (DEC-023, point 2): route value from the market (best of NPC and market, missing prices reported) and from one surveillance band
// (the community source splits each sector's records by the surveillance of the submarine that made them).
internal static class Routes335Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static bool Near(double? value,double expected) => value is { } found && Math.Abs(found - expected) < 1e-9;

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Routes: a band weighs each pool by its share of that band's records; a band without record is unknown, not zero",() =>
        {
            // Pool 1: 100 records (60 low, 40 mid), item 1 seen 300 times. Pool 2: 50 records (10 mid, 40 high), item 2 seen 100 times.
            var loot = new SectorLoot(7,150,[(1,300),(2,100)],
                [new SectorTier(100,60,40,0,[(1,300)]),new SectorTier(50,0,10,40,[(2,100)]),new SectorTier(0,0,0,0,[])]);
            double Price(uint item) => item == 1 ? 10 : 100;
            Check(Near(VoyagePlanner.ExpectedValue(loot,Price,SurveillanceBand.All),(300 * 10 + 100 * 100) / 150.0),"all records = the former average");
            Check(Near(VoyagePlanner.ExpectedValue(loot,Price,SurveillanceBand.Low),3 * 10),"low: only pool 1");
            Check(Near(VoyagePlanner.ExpectedValue(loot,Price,SurveillanceBand.Mid),0.8 * 3 * 10 + 0.2 * 2 * 100),"mid: 40 of 50 records in pool 1, 10 in pool 2");
            Check(Near(VoyagePlanner.ExpectedValue(loot,Price,SurveillanceBand.High),2 * 100),"high: only pool 2");
            var noHigh = loot with { Tiers = [new SectorTier(150,100,50,0,[(1,300),(2,100)])] };
            Check(VoyagePlanner.ExpectedValue(noHigh,Price,SurveillanceBand.High) is null,"no record in the band: unknown");
            Check(VoyagePlanner.ExpectedValue(loot with { Tiers = null },Price,SurveillanceBand.Mid) is null,"no pools: unknown");
        }),
        ("Routes: market value is the best of NPC and market; a marketable item without price stays at the NPC price and is missing",() =>
        {
            Check(VoyagePlanner.UnitValue(50,false,null) == (50,false),"not marketable: NPC price, nothing missing");
            Check(VoyagePlanner.UnitValue(50,true,null) == (50,true),"no price: NPC price, missing");
            Check(VoyagePlanner.UnitValue(50,true,0) == (50,true),"zero is not a price");
            Check(VoyagePlanner.UnitValue(50,true,200) == (200,false),"market higher: market");
            Check(VoyagePlanner.UnitValue(500,true,200) == (500,false),"NPC higher: NPC");
        }),
        ("Routes: the embedded loot keeps its pools by band, which add up to the sector's records",() =>
        {
            using var file = File.OpenRead(Path.GetFullPath("src/TenshoAssistant/FreeCompany/Data/submarine-loot.json"));
            var loot = VoyagePlanner.ReadLoot(file).Loot;
            Check(loot.Count == 143 && loot.Values.All(sector => sector.Tiers is { Count: 3 }),"143 sectors, 3 pools each");
            Check(loot.Values.All(sector => sector.Tiers!.Sum(tier => tier.Records) == sector.Records),"pools add up to the records");
            Check(loot.Values.All(sector => sector.Tiers!.All(tier => tier.Low + tier.Mid + tier.High == tier.Records)),"bands add up to each pool");
            // Generator and evidence describe the same layout.
            Check(File.ReadAllText(Path.GetFullPath("tools/submarine-loot.py")).Contains("Stats Min / Mid / High",StringComparison.Ordinal),"generator documents the bands");
            Check(File.ReadAllText(Path.GetFullPath("docs/evidence/0.335-submarine-tiers.md")).Contains("MinT2",StringComparison.Ordinal),"evidence of the probe");
        }),
        ("Routes: the planner copies prices on the UI thread and fetches market prices only on a click, bounded to 32",() =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.RoutePlanner.cs"));
            Check(page.Contains("var values=BuildRouteValues(catalog,map,world.WorldId);",StringComparison.Ordinal),"values built before the worker");
            Check(page.Contains("routePrices.StartListAsync(market,character,world.WorldId,wanted.Take(32))",StringComparison.Ordinal),"bounded list on a click");
            Check(page.Contains("configuration.CommunityUniversalis",StringComparison.Ordinal),"honours the Universalis switch");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Lifetime.cs")).Contains("routePrices.Dispose();",StringComparison.Ordinal),"session disposed");
        }),
    ];
}
