using TenshoAssistant.Gathering;

// 0.1605 (X-661 M1, review of the block 0.1599 to 0.1602): a custom delivery's collectable in several SatisfactionSupply rows (Coal 38821 in
// 42.1 and 45.6, same client and thresholds: docs/evidence/0.1601-delivery-collectables.md) was shown once per row in the collectables table.
internal static class CollectableDeliveryOnce1605Tests
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    private static CollectableRule Delivery(uint item,string name,int low=240,string client="Anden")=>new(item,name,0,0,low,450,600,0,0,0,false,client,-1,Delivery:true);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Collectable delivery once 1605: the same rule in two rows kept once; rows that differ stay apart; the table shows each once",()=>
        {
            var rows=new[]{Delivery(38821,"Charbon"),Delivery(38822,"Ingrédient"),Delivery(38821,"Charbon"),Delivery(38822,"Ingrédient"),Delivery(38821,"Charbon",200),Delivery(38821,"Charbon",client:"Margrat")};
            var kept=CollectableDeliveries.Distinct(rows);
            Check(kept.Count==4,"two duplicates dropped: "+kept.Count);
            Check(kept.Count(rule=>rule.ItemId==38821 && rule.Low==240 && rule.Group=="Anden")==1,"Coal of Anden at 240 once");
            Check(kept.Count(rule=>rule.ItemId==38821)==3,"other thresholds or another client: their own rule");
            var catalog=new CollectableCatalog(kept);
            Check(catalog.All.Count(rule=>rule.ItemId==38822)==1 && catalog.For(38822).Count==1,"the table and the panel: once");
        }),
    ];
}
