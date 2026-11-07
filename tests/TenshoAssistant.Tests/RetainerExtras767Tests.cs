using TenshoAssistant.Inventory;

// 0.767 (I10): a retainer's gil and crystals.
internal static class RetainerExtras767Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private const ulong Character=1,Retainer=99;
    private static InventoryContainerSnapshot Container(int id,uint item=0,long quantity=0)=>new(new(new(Retainer),new(id)),InventoryCoverage.Complete,quantity>0 ? [new InventoryStack(new(new(item),false),quantity,0)]:[]);
    private static BankEntry Entry(params InventoryContainerSnapshot[] extra)=>new(new(Character,BankStorageKind.Retainer,Retainer),"Retainer",
        new(DateTimeOffset.UtcNow,"test",Enumerable.Range(10000,7).Select(id=>Container(id)).Concat(extra)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Retainer extras: a copy with gil and crystals is valid, one without them too, a stranger container is not",() =>
        {
            BankEntryValidator.Validate(Entry());
            BankEntryValidator.Validate(Entry(Container(12000,1,250),Container(12001,4,30)));
            var refused=false;
            try{BankEntryValidator.Validate(Entry(Container(12002)));}catch(ArgumentException){refused=true;}
            Check(refused,"another container");
            var missing=false;
            try{BankEntryValidator.Validate(new(new(Character,BankStorageKind.Retainer,Retainer),"Retainer",new(DateTimeOffset.UtcNow,"test",[Container(10000),Container(12001)])));}catch(ArgumentException){missing=true;}
            Check(missing,"the pages are still required");
        }),
        ("Retainer extras: its crystals count and are exact; a copy without them stays a lower bound",() =>
        {
            var with=ItemHoldings.Build(4,Character,null,[Entry(Container(12001,4,30))],StorageFilter.Retainers,crystal:true);
            Check(with.Total==30 && with.Rows.Single(row=>row.Label==StorageLabel.Retainer).Coverage==InventoryCoverage.Complete,"counted, exact");
            var without=ItemHoldings.Build(4,Character,null,[Entry()],StorageFilter.Retainers,crystal:true);
            Check(without.Rows.Single(row=>row.Label==StorageLabel.Retainer).Coverage==InventoryCoverage.Partial,"lower bound as before");
            Check(ItemHoldings.Build(1,Character,null,[Entry(Container(12000,1,250))],StorageFilter.Retainers).Total==250,"gil too");
        }),
    ];
}
