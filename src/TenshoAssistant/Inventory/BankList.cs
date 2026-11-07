namespace TenshoAssistant.Inventory;

/// <summary>0.594 (I11): one line of the Bank's list view: an item, its quantities and the storages it is in.</summary>
public sealed record BankListRow(uint ItemId,long Nq,long Hq,IReadOnlyList<string> Storages)
{
    public long Total=>checked(Nq+Hq);
}
public enum BankListSort {Name,Quantity,Category}

public static class BankList
{
    /// <summary>The stacks of every storage shown, one row per item, with the storages in the order they came.</summary>
    public static BankListRow[] Merge(IEnumerable<(string Storage,uint Item,bool Hq,long Amount)> quantities)
    {
        var rows=new Dictionary<uint,(long Nq,long Hq,List<string> Storages)>();
        foreach(var (storage,item,hq,amount) in quantities)
        {
            if(amount<=0)continue;
            if(!rows.TryGetValue(item,out var row))row=(0,0,[]);
            if(hq)row.Hq=checked(row.Hq+amount);else row.Nq=checked(row.Nq+amount);
            if(!row.Storages.Contains(storage))row.Storages.Add(storage);
            rows[item]=row;
        }
        return rows.Select(pair=>new BankListRow(pair.Key,pair.Value.Nq,pair.Value.Hq,pair.Value.Storages.ToArray())).ToArray();
    }

    /// <summary>By name, by quantity (most first) or by category, then by name; Descending turns the order round.</summary>
    public static BankListRow[] Sorted(IEnumerable<BankListRow> rows,BankListSort sort,bool descending,Func<uint,string> name,Func<uint,string> category)
    {
        var comparer=StringComparer.CurrentCultureIgnoreCase;
        var ordered=sort switch
        {
            BankListSort.Quantity=>descending ? rows.OrderByDescending(row=>row.Total):rows.OrderBy(row=>row.Total),
            BankListSort.Category=>descending ? rows.OrderByDescending(row=>category(row.ItemId),comparer):rows.OrderBy(row=>category(row.ItemId),comparer),
            _=>descending ? rows.OrderByDescending(row=>name(row.ItemId),comparer):rows.OrderBy(row=>name(row.ItemId),comparer),
        };
        return ordered.ThenBy(row=>name(row.ItemId),comparer).ThenBy(row=>row.ItemId).ToArray();
    }
}
