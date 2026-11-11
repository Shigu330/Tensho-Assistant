// 0.1103 (X-01): the map's first frame after a load grouped every NPC sale by shop and scanned every vendor (« canvas vendors » 71 to 156 ms,
// once 2 729 ms, dalamud.log of 11/11). The catalogue now builds both off the frame; same results as the former per-frame code.
using System.Diagnostics;
using TenshoAssistant.Npcs;

internal static class MapVendorIndex1103Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static NpcLocation Spot(uint map,int condition=0) => new(1,map,"Zone",10,10,0,0,0,condition);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map vendors: shop summaries and vendors per map come from the catalogue, same as the former frame code",() =>
        {
            var vendors = new[]{new NpcVendor(1,"A",[10,20],[Spot(5),Spot(6)]),new NpcVendor(2,"B",[20],[Spot(6)]),new NpcVendor(3,"C",[30],[Spot(7)]),
                                new NpcVendor(4,"D",[10],[Spot(7),Spot(5,1)])};
            var sales = new[]{new NpcShopSale(10,NpcShopKind.Gil,"Ten",100,1,false,[new(1,5)]),new NpcShopSale(20,NpcShopKind.Special,"Twenty",200,1,false,[new(2,5)]),
                              new NpcShopSale(10,NpcShopKind.Gil,"Ten bis",101,1,false,[new(1,5)]),new NpcShopSale(10,NpcShopKind.Gil,"Ten",100,2,false,[new(1,9)])};
            var catalog = NpcVendorCatalog.Build(vendors,sales,"v");
            var former = catalog.Sales.GroupBy(sale=>sale.ShopId).ToDictionary(group=>group.Key,group=>(group.First().ShopName,group.First().Kind,group.Select(sale=>sale.ItemId).Distinct().Count()));
            Check(former.Count == catalog.ShopSummaries.Count && former.All(pair=>catalog.ShopSummaries[pair.Key] == pair.Value),"same shop summaries");
            Check(catalog.ShopSummaries[10] == ("Ten",NpcShopKind.Gil,2),"first sale's name and kind, distinct items");
            foreach (var same in new[]{new HashSet<uint>{5},new HashSet<uint>{6},new HashSet<uint>{5,7},new HashSet<uint>{9},new HashSet<uint>{5,6,7}})
            {
                var scan = catalog.Vendors.Where(vendor=>vendor.Locations.Any(spot=>same.Contains(spot.MapId))).ToArray();
                Check(catalog.VendorsOnAny(same).SequenceEqual(scan),"same vendors, same order, maps " + string.Join(",",same));
            }
        }),
        ("Map vendors: the frame no longer groups the sales; the index answers at once on a large catalogue",() =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(!page.Contains("catalog.Sales.GroupBy") && page.Contains("var shops=catalog.ShopSummaries;") && page.Contains("var nearby=catalog.VendorsOnAny(same);"),"wired");
            var vendors = Enumerable.Range(1,6000).Select(n=>new NpcVendor((uint)n,"V"+n,[(uint)(n % 3000 + 1)],[Spot((uint)(n % 400 + 1))])).ToArray();
            var sales = Enumerable.Range(0,200_000).Select(n=>new NpcShopSale((uint)(n % 3000 + 1),NpcShopKind.Gil,"S",(uint)(n % 50_000 + 1),1,false,[new(1,1)])).ToArray();
            var catalog = NpcVendorCatalog.Build(vendors,sales,"v");
            var watch = Stopwatch.StartNew();
            for (var map = 1u; map <= 50; map++) _ = catalog.VendorsOnAny([map]);
            var indexed = watch.Elapsed.TotalMilliseconds;
            watch.Restart();
            _ = catalog.Sales.GroupBy(sale=>sale.ShopId).ToDictionary(group=>group.Key,group=>group.Select(sale=>sale.ItemId).Distinct().Count());
            var grouped = watch.Elapsed.TotalMilliseconds;
            Check(indexed < grouped,$"50 map changes by the index ({indexed:0.0} ms) cost less than one former grouping of the sales ({grouped:0.0} ms)");
        }),
    ];
}
