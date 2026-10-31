using TenshoAssistant.Caching;
using TenshoAssistant.Items;

internal static class ItemCatalogCacheTests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static T Wait<T>(Task<T> task)=>task.WaitAsync(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult();
    private static readonly ItemCatalogMetadata Game=new("Lumina.Excel/Item","French","ffxiv=2026.10.01;ex1=2026.10.01;schema=5.0",1);
    private static ItemDefinition[] Items()=>
    [
        new(42,"Épée de fer",true,1001,1,"Arme",true,0,EquipmentSlots.MainHand),
        new(43,"Marteau",false,1002,2,"Outil",false,0,EquipmentSlots.MainHand,new CraftToolDefinition(10,175)),
        new(44,"Carte",false,1003,3,"Carte",true,7,OrchestrionId:9),
    ];
    private static string Temporary()=>Path.Combine(Path.GetTempPath(),"Tensho-ItemCacheTests-"+Guid.NewGuid().ToString("N"));
    public static IEnumerable<(string Name,Action Run)> Cases=>new (string,Action)[]
    {
        ("Item cache roundtrips through the disk cache and rebuilds an equivalent catalogue",()=>
        {
            var root=Temporary();
            try
            {
                using(var cache=new DataCache(root))
                    Check(Wait(cache.WriteAsync(ItemCatalogCache.Family,ItemCatalogCache.Scope(Game),ItemCatalogCache.Version(Game),ItemCatalogCache.Snapshot(Game,Items()),DateTimeOffset.UtcNow)),"written");
                using var reloaded=new DataCache(root);
                var value=Wait(reloaded.ReadAsync<ItemCatalogCacheData>(ItemCatalogCache.Family,ItemCatalogCache.Scope(Game),ItemCatalogCache.Version(Game)))?.Value;
                Check(ItemCatalogCache.Valid(value,Game),"restored snapshot valid");
                var catalog=ItemCatalog.Build(value!.Items,ItemCatalogCache.FromCache(Game));
                Check(catalog.Count==3 && catalog.Find(42)==Items()[0] && catalog.Find(43)!.CraftTool==new CraftToolDefinition(10,175),"rows equal, nested tool kept");
                Check(catalog.ForTriadCard(7).Single().Id==44 && catalog.ForOrchestrion(9).Single().Id==44 && catalog.Categories.Count==3,"derived indexes rebuilt");
                Check(catalog.Metadata.Source.EndsWith("DataCache",StringComparison.Ordinal) && catalog.Metadata.DataVersion==Game.DataVersion,"provenance marks the disk copy");
            }
            finally{if(Directory.Exists(root))Directory.Delete(root,true);}
        }),
        ("A client update another language or another copy shape never reuses the item cache",()=>
        {
            var updated=Game with{DataVersion="ffxiv=2026.11.15;ex1=2026.11.15;schema=5.0"};
            Check(ItemCatalogCache.Version(updated)!=ItemCatalogCache.Version(Game),"game version is part of the key");
            Check(ItemCatalogCache.Scope(Game with{Language="English"})!=ItemCatalogCache.Scope(Game),"language is the scope");
            Check(ItemCatalogCache.Version(Game).Contains(ItemCatalogCache.Shape,StringComparison.Ordinal) && ItemCatalogCache.Shape.Contains("CraftTool:CraftToolDefinition",StringComparison.Ordinal)
                && ItemCatalogCache.Shape.Contains("GoodQualityPercent:Int32?",StringComparison.Ordinal),"record shape is part of the key");
            var snapshot=ItemCatalogCache.Snapshot(Game,Items());
            Check(!ItemCatalogCache.Valid(snapshot,updated) && !ItemCatalogCache.Valid(snapshot,Game with{Language="English"}) && !ItemCatalogCache.Valid(snapshot,Game with{Source="Other"}),"stored identity rechecked");
        }),
        ("Corrupt item cache content is rejected instead of published",()=>
        {
            var good=ItemCatalogCache.Snapshot(Game,Items());
            foreach(var bad in new[]{good with{Items=[]},good with{Items=null!},good with{Items=[Items()[0],Items()[0]]},good with{Items=[Items()[0] with{Id=0}]},
                good with{Items=[Items()[0] with{Name=" "}]},good with{Items=[Items()[0] with{Equipment=(EquipmentSlots)(1<<20)}]},
                good with{Items=[Items()[1] with{CraftTool=new(3,null)}]},good with{Items=[null!]}})
                Check(!ItemCatalogCache.Valid(bad,Game),"invalid snapshot refused");
            Check(!ItemCatalogCache.Valid(null,Game) && ItemCatalogCache.Valid(good,Game),"null refused, good accepted");
            var detached=Items();var copy=ItemCatalogCache.Snapshot(Game,detached);detached[0]=detached[1];
            Check(copy.Items[0].Id==42,"snapshot owns its array");
        }),
    };
}
