// 0.871 (X-73.15): data centre regions from the game sheet before Universalis' table.
using TenshoAssistant.Market;
internal static class Regions871Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Regions: the game sheet names the region with no request; cloud and unknown fall back to Universalis",() =>
        {
            using var service=new MarketBoardService{Enabled=()=>false,GameRegions=new Dictionary<string,uint>(StringComparer.OrdinalIgnoreCase){["Light"]=3,["Materia"]=4,["NA Cloud DC (Beta)"]=7}};
            Check(service.RegionOf("light")=="Europe" && service.RegionOf("Materia")=="Oceania","from the game");
            Check(service.RegionOf("NA Cloud DC (Beta)") is null && service.RegionOf("Nowhere") is null,"source off: unknown stays unknown");
            Check(MarketBoardService.UniversalisRegion(1)=="Japan" && MarketBoardService.UniversalisRegion(2)=="North-America" && MarketBoardService.UniversalisRegion(7) is null,"names");
        }),
    ];
}
