using TenshoAssistant.Inventory;

// 0.609: where an item is on the other characters.
internal static class OtherStocks609Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At=new(2026,11,5,12,0,0,TimeSpan.Zero);
    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage,uint item,long quantity,bool hq=false,int days=0)
        =>new(new(character,kind,storage),"",new InventorySnapshot(At.AddDays(days),"test",[new InventoryContainerSnapshot(new(new(storage==0 ? character:storage),new(10000)),InventoryCoverage.Complete,[new(new(new(item),hq),quantity,0,0)])]));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Other stocks: per character, most held first, without the current one",() =>
        {
            BankEntry[] entries=[Entry(1,BankStorageKind.Retainer,10,5,9),Entry(2,BankStorageKind.Retainer,20,5,1),Entry(3,BankStorageKind.Retainer,30,5,4),Entry(3,BankStorageKind.Retainer,31,5,6,true,2)];
            var rows=OtherCharacterStocks.Of(5,1,entries);
            Check(rows.Select(row=>row.CharacterId).SequenceEqual<ulong>([3,2]),"others, by quantity");
            Check(rows[0] is {Nq:4,Hq:6} && rows[0].Storages.SequenceEqual([BankStorageKind.Retainer]) && rows[0].ObservedAt==At.AddDays(2),"sum, storage once, latest date");
        }),
        ("Other stocks: another item or none is left out; on the item sheet, once per change",() =>
        {
            Check(OtherCharacterStocks.Of(6,1,[Entry(2,BankStorageKind.Retainer,20,5,1)]).Length==0,"not held");
            Check(OtherCharacterStocks.Of(5,1,[]).Length==0,"nothing saved");
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Items.cs"));
            Check(view.Contains("DrawOtherCharacterStocks((uint)itemId,character,bankState);"),"after the stocks");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Characters.cs")).Contains("if(otherStocksItem!=item || otherStocksCharacter!=character || !ReferenceEquals(otherStocksBank,bank))"),"built when something changes");
        }),
    ];
}
