using TenshoAssistant.Inventory;

// 0.594 (I11): the Bank as a list.
internal static class BankList594Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Bank list: one row per item, NQ and HQ apart, each storage once",() =>
        {
            var rows=BankList.Merge([("Bags",1,false,5),("Bags",1,true,2),("Retainer",1,false,3),("Retainer",2,false,0),("Bags",1,false,1)]);
            Check(rows.Length==1 && rows[0] is {ItemId:1,Nq:9,Hq:2,Total:11},"the sums, an empty stack ignored");
            Check(rows[0].Storages.SequenceEqual(["Bags","Retainer"]),"each storage once, in order");
        }),
        ("Bank list: sorted by name, quantity or category, both ways",() =>
        {
            BankListRow[] rows=[new(1,5,0,["A"]),new(2,50,1,["A"]),new(3,5,0,["A"])];
            string Name(uint id)=>id==1 ? "Pear":id==2 ? "Apple":"Melon";
            string Category(uint id)=>id==2 ? "Ore":"Food";
            uint[] Ids(BankListSort sort,bool descending)=>BankList.Sorted(rows,sort,descending,Name,Category).Select(row=>row.ItemId).ToArray();
            Check(Ids(BankListSort.Name,false).SequenceEqual<uint>([2,3,1]),"by name");
            Check(Ids(BankListSort.Name,true).SequenceEqual<uint>([1,3,2]),"by name, reversed");
            Check(Ids(BankListSort.Quantity,true).SequenceEqual<uint>([2,3,1]),"most first, equal ones by name");
            Check(Ids(BankListSort.Category,false).SequenceEqual<uint>([3,1,2]),"by category, then name");
        }),
        ("Bank list: the same filters as the grids, off the frame's work, and its setting",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.BankList.cs"));
            Check(view.Contains("MatchesBankFilters(new(row.ItemId,row.Nq,row.Hq))"),"the filters of the grids");
            Check(view.Contains("if(bankListKey!=key)"),"built only when something changes");
            Check(!view.Contains("Second"),"not again each second");
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Bank.cs"));
            Check(page.Contains("if(configuration.BankListView){DrawBankList(sourceKey,selectedEntries);return;}"),"instead of the grids");
            Check(!new TenshoAssistant.Configuration().BankListView,"grids by default");
        }),
    ];
}
