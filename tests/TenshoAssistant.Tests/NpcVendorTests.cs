using TenshoAssistant.Caching;
using TenshoAssistant.Npcs;

internal static class NpcVendorTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static T Wait<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private static readonly NpcLocation Limsa=new(128,11,"Limsa Lominsa",10.5f,11.2f,1,2,3);
    private static NpcVendor Vendor(uint id,string name,uint shop,params NpcLocation[] places)=>new(id,name,[shop],places);
    private static NpcShopSale Gil(uint shop,uint item,uint price)=>new(shop,NpcShopKind.Gil,"Shop "+shop,item,1,false,[new(1,price)]);
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("NPC shop links follow direct shops topics pre-handlers and talk scripts without cycles",()=>
        {
            var shops=new HashSet<uint>{262144,1769472,1441792};
            var links=new Dictionary<uint,IReadOnlyList<uint>>{[3276800]=[262144,3604480],[3604480]=[1769472],[721000]=[5,1441792],[9]=[10],[10]=[9]};
            Check(NpcShopLinks.Resolve([262144],shops,links).SequenceEqual(new uint[]{262144}),"direct shop");
            Check(NpcShopLinks.Resolve([3276800],shops,links).SequenceEqual(new uint[]{262144,1769472}),"topic menu then pre-handler");
            Check(NpcShopLinks.Resolve([721000,0,262144],shops,links).SequenceEqual(new uint[]{262144,1441792}),"talk script arguments, zero ignored, distinct");
            Check(NpcShopLinks.Resolve([9],shops,links).Count==0,"cycle terminates");
            var wolfShops=new HashSet<uint>{1769743,1770537,1769744};
            Check(NpcShopLinks.Resolve([721280],wolfShops,NpcShopLinks.ObservedTalkShops).SequenceEqual(new uint[]{1769743,1769744,1770537}),"observed script menu reaches the three wolf-mark shops");
            var fate=new Dictionary<uint,IReadOnlyList<uint>>{[1027998]=[1770736,1770746]};
            var special=new HashSet<uint>{1770736,1770746};
            Check(NpcShopLinks.Resolve(new uint[]{0,5}.Prepend(1027998u),special,fate).SequenceEqual(new uint[]{1770736,1770746}),"FateShop keyed by the NPC itself");
            var deep=new Dictionary<uint,IReadOnlyList<uint>>{[1]=[2],[2]=[3],[3]=[4],[4]=[5],[5]=[262144]};
            Check(NpcShopLinks.Resolve([1],shops,deep).Count==0 && NpcShopLinks.Resolve([2],shops,deep).Count==1,"depth bounded");
        }),
        ("Currencies are exact for items and never guessed for encoded scrip or tomestone ids",()=>
        {
            Check(NpcShopLinks.Currency(0,25,300)==new NpcCost(25,300),"direct small item id (wolf marks) kept");
            Check(NpcShopLinks.Currency(4,26807,5)==new NpcCost(26807,5),"large id is an item even when encoded");
            // Probe 2026-10-30: purple scrip exchanges encode type 16 id 4 for both crafter and gatherer scrips.
            const byte tome=NpcShopLinks.TomestoneCost,scrip=NpcShopLinks.ScripCost;
            Check(NpcShopLinks.Currency(16,4,20,scrip)==new NpcCost(33914,20) && NpcShopLinks.Currency(16,2,20,scrip)==new NpcCost(33913,20),"observed purple scrips");
            Check(NpcShopLinks.Currency(16,6,500,scrip)==new NpcCost(41784,500) && NpcShopLinks.Currency(16,7,500,scrip)==new NpcCost(41785,500),"orange scrips (validated in game)");
            // Same code 2 under type 4: CostType separates the mathematics shop (Zircon, 495) from the old crafter scrip shops.
            Check(NpcShopLinks.Currency(4,2,495,tome)==new NpcCost(48,495) && NpcShopLinks.Currency(4,2,20,scrip)==new NpcCost(33913,20),"CostType disambiguates code 2");
            Check(NpcShopLinks.Currency(2,3,500,tome)==new NpcCost(49,500) && NpcShopLinks.Currency(16,1,600,tome)==new NpcCost(28,600),"mnemonics and poetics");
            Check(NpcShopLinks.Currency(16,25,350)==new NpcCost(25,350) && NpcShopLinks.Currency(4,27,600)==new NpcCost(27,600) && NpcShopLinks.Currency(4,29,10000)==new NpcCost(29,10000),"wolf marks, allied seals, MGP");
            Check(NpcShopLinks.Currency(4,25,2000)==new NpcCost(25,2000),"wolf marks of the currency exchange");
            Check(NpcShopLinks.Currency(16,2,5)==new NpcCost(0,5,2) && NpcShopLinks.Currency(4,9,5,tome)==new NpcCost(0,5,9),"unnamed or unknown codes stay raw");
            Check(NpcShopLinks.Currency(2,4,50)==new NpcCost(0,50,4),"other encodings stay raw");
        }),
        ("Map coordinates follow the game formula for scale and offset",()=>
        {
            // Headers seen in the probe: a valid planner file, and the 312-byte layout with LGP1 at offset 32 that broke Lumina.
            var valid=Convert.FromHexString("4C474231"+"10000000"+"01000000"+"4C475031");
            var shifted=Convert.FromHexString("4C474231"+"10000000"+"01000000"+"01000000");
            Check(NpcShopLinks.ValidLayout(valid) && !NpcShopLinks.ValidLayout(shifted) && !NpcShopLinks.ValidLayout(valid.AsSpan(0,12))
                && !NpcShopLinks.ValidLayout(Convert.FromHexString("4C474231"+"20000000"+"01000000"+"4C475031")),"layout header check");
            Check(NpcShopLinks.Resolve([721735],new HashSet<uint>{1770684},NpcShopLinks.ObservedTalkShops).SequenceEqual(new uint[]{1770684}),"commendation crystal exchange");
            Check(Math.Abs(NpcShopLinks.MapCoordinate(0,0,100)-21.5f)<0.001f,"map centre");
            Check(Math.Abs(NpcShopLinks.MapCoordinate(1024,0,100)-42f)<0.001f && Math.Abs(NpcShopLinks.MapCoordinate(-1024,0,100)-1f)<0.001f,"edges");
            Check(Math.Abs(NpcShopLinks.MapCoordinate(0,0,200)-11.25f)<0.001f && Math.Abs(NpcShopLinks.MapCoordinate(-100,100,200)-11.25f)<0.001f,"scale and offset");
        }),
        ("Offers put located and cheaper vendors first and the catalogue survives the disk cache",()=>
        {
            var vendors=new[]{Vendor(1000001,"Zed",262144,Limsa),Vendor(1000002,"Ann",262145,Limsa),Vendor(1000003,"Nowhere",262146),Vendor(1000004,"Orphan",999)};
            var sales=new[]{Gil(262144,42,100),Gil(262145,42,80),Gil(262146,42,10),new NpcShopSale(262145,NpcShopKind.Special,"S",43,3,true,[new(28,50),new(0,2,7)])};
            var catalog=NpcVendorCatalog.Build(vendors,sales,"v");
            Check(catalog.ForItem(42).Select(offer=>offer.Vendor.Name).SequenceEqual(new[]{"Ann","Zed","Nowhere"}),"located, then cheaper, then unlocated");
            Check(catalog.ForItem(43).Single().Sale.Costs.Count==2 && catalog.ForItem(99).Count==0 && catalog.VendorCount==4,"multi-cost sale and empty lookup");
            var root=Path.Combine(Path.GetTempPath(),"Tensho-NpcTests-"+Guid.NewGuid().ToString("N"));
            try
            {
                var data=new NpcVendorCacheData("v",vendors,sales);
                using(var cache=new DataCache(root))Check(Wait(cache.WriteAsync(NpcVendorCache.Family,"French",NpcVendorCache.Version("v"),data,DateTimeOffset.UtcNow)),"written");
                using var reloaded=new DataCache(root);
                var value=Wait(reloaded.ReadAsync<NpcVendorCacheData>(NpcVendorCache.Family,"French",NpcVendorCache.Version("v")))?.Value;
                Check(NpcVendorCache.Valid(value,"v") && !NpcVendorCache.Valid(value,"other"),"identity rechecked");
                var rebuilt=NpcVendorCatalog.Build(value!.Vendors,value.Sales,"v");
                Check(rebuilt.ForItem(42)[0].Vendor.Locations[0]==Limsa && rebuilt.ForItem(43)[0].Sale.Costs[1]==new NpcCost(0,2,7),"positions and raw currencies kept");
                Check(!NpcVendorCache.Valid(data with{Sales=[]},"v") && !NpcVendorCache.Valid(data with{Vendors=[vendors[0] with{Name=null!}]},"v")
                    && !NpcVendorCache.Valid(data with{Sales=[sales[0] with{Kind=(NpcShopKind)9}]},"v"),"corrupt rows refused");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }),
    };
}
