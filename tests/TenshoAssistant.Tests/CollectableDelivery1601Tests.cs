using TenshoAssistant.Gathering;

// 0.1601 (X-657, the user's report of 20/11: the collectable « Charbon » had no threshold in the Collectable panel). The custom deliveries'
// collectables are in SatisfactionSupply only, never in CollectablesShopItem: their thresholds were never read
// (docs/evidence/0.1601-delivery-collectables.md). They now join the catalogue.
internal static class CollectableDelivery1601Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}

    private static CollectableRule Delivery(uint item,string name)=>new(item,name,0,0,240,450,600,0,0,0,false,"Anden",-1,Delivery:true);
    private static CollectableRule Scrip(uint item,string name)=>new(item,name,80,80,480,580,720,10,15,20,true,"Gatherers",2);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable delivery 1601: a custom delivery's collectable is found by its item and its name, with its thresholds",()=>
        {
            var catalog=new CollectableCatalog([Delivery(38821,"Charbon"),Delivery(38822,"Ingrédient de plat de Voeburt"),Scrip(1,"Autre")]);
            Check(catalog.For(38821) is [{Low:240,Mid:450,High:600,Delivery:true,Scrips:false,Group:"Anden"}],"Charbon by its item (the window's value 2)");
            Check(catalog.Named("Charbon").Count==1,"Charbon by its name");
            Check(catalog.Named("Ingrédient de plat de Voeburt …").Count==1 || catalog.Named("Ingrédient de plat de Voeburt…").Count==1,"the cut name of the game's window");
            Check(catalog.All.Count==3,"in the collectables table too");
        }),
        ("Collectable delivery 1601: no scrips for a delivery; a node mixing a delivery and a scrip collectable names no « best »",()=>
        {
            var delivery=Delivery(38821,"Charbon");
            Check(!delivery.Scrips && delivery.RewardHigh==0 && delivery.ScripKind==-1,"no scrips");
            Check(CollectableChoice.Best([delivery,Scrip(2,"Autre")]) is null,"mixed kinds: none");
            Check(!new CollectableRule(1,"x",1,1,1,2,3,0,0,0,false).Delivery,"a shop's rule is not a delivery (default)");
        }),
        ("Collectable delivery 1601: the reader adds SatisfactionSupply's rows; the table says « custom delivery » and « — » for no level",()=>
        {
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Gathering/CollectableReader.Dalamud.cs"));
            Check(reader.Contains("rules.AddRange(CollectableDeliveries.Distinct(DeliveryRules()));") && reader.Contains("data.GetSubrowExcelSheet<SatisfactionSupply>()")
                && reader.Contains("line.CollectabilityLow,line.CollectabilityMid,line.CollectabilityHigh"),"the delivery rows read");
            var table=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.CollectableTable.cs"));
            Check(table.Contains("rule.Delivery ? \"CollectableTable.DeliveryReward\"") && table.Contains("rule.LevelMin==0 && rule.LevelMax==0 ? \"—\""),"the table");
            foreach(var language in new[]{"en","fr"})
                Check(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).Contains("\"DeliveryReward\": \""),language);
        }),
    ];
}
