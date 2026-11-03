using TenshoAssistant.Items;

// 0.353 (g, I-12): item search with filters (alone or with a name); key items searched by name; the wiki command's search limit fixed.
internal static class ItemFilters353Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item filters: category, levels, slot, tradable, marketable; with or without a name; an empty filter is the old search",() =>
        {
            var catalog = ItemCatalog.Build(
            [
                new(1,"Casque de fer",true,UiCategoryId:34,UiCategoryName:"Tête",IsMarketable:true,Equipment:EquipmentSlots.Head,ItemLevel:100,EquipLevel:50),
                new(2,"Casque de mythril",true,UiCategoryId:34,UiCategoryName:"Tête",IsMarketable:true,Equipment:EquipmentSlots.Head,ItemLevel:200,EquipLevel:70),
                new(3,"Bague liée",false,UiCategoryId:43,UiCategoryName:"Bague",Equipment:EquipmentSlots.Ring,ItemLevel:200,EquipLevel:70,Traits:ItemTraits.Untradable),
                new(4,"Minerai de fer",true,UiCategoryId:47,UiCategoryName:"Minerai",IsMarketable:true),
            ],new("test","French","test",1));
            Check(catalog.Search("",100,new ItemSearchFilter(Category:34)).Select(item => item.Id).Order().SequenceEqual([1u,2u]),"category alone, no name");
            Check(catalog.Search("fer",100,new ItemSearchFilter(Category:34)).Single().Id == 1,"name and category");
            Check(catalog.Search("",100,new ItemSearchFilter(MinEquipLevel:60,MaxItemLevel:250)).Select(item => item.Id).Order().SequenceEqual([2u,3u]),"level ranges");
            Check(catalog.Search("",100,new ItemSearchFilter(Slot:EquipmentSlots.Ring)).Single().Id == 3,"slot");
            Check(catalog.Search("",100,new ItemSearchFilter(MinItemLevel:150,TradableOnly:true)).Single().Id == 2,"tradable only");
            Check(catalog.Search("",100,new ItemSearchFilter(MarketableOnly:true)).Count == 3,"sold on the market only");
            Check(catalog.Search("",100,ItemSearchFilter.None).Count == 0 && catalog.Search("fer",100,ItemSearchFilter.None).Count == 2,"no filter: the name search as before");
            Check(Read("MainWindow.ExternalLinks.cs").Contains("catalog.Search(text,100)",StringComparison.Ordinal),"wiki command within the search limit (200 threw)");
            Check(Read("Plugin.cs").Contains("Lumina.Excel.Sheets.EventItem",StringComparison.Ordinal),"key items from the game data");
        }),
    ];
}
