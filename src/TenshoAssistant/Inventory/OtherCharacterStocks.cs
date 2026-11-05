namespace TenshoAssistant.Inventory;

/// <summary>0.609: an item on another character, from what the Bank saved: its quantities and the kinds of storage holding it.</summary>
public sealed record OtherCharacterStock(ulong CharacterId,long Nq,long Hq,IReadOnlyList<BankStorageKind> Storages,DateTimeOffset ObservedAt);

public static class OtherCharacterStocks
{
    /// <summary>Most held first. Only what was saved: a character or storage never read is not there, which does not mean it holds none.</summary>
    public static OtherCharacterStock[] Of(uint itemId,ulong currentCharacter,IEnumerable<BankEntry> entries)
    {
        var rows=new List<OtherCharacterStock>();
        foreach(var group in entries.Where(entry=>entry.Key.CharacterId!=currentCharacter).GroupBy(entry=>entry.Key.CharacterId))
        {
            long nq=0,hq=0;var kinds=new List<BankStorageKind>();var at=DateTimeOffset.MinValue;
            foreach(var entry in group)
            {
                long here=0;
                foreach(var container in entry.Snapshot.Containers.Values)
                {
                    var plain=container.Quantities.GetValueOrDefault(new InventoryItem(new InventoryItemId(itemId),false));
                    var high=container.Quantities.GetValueOrDefault(new InventoryItem(new InventoryItemId(itemId),true));
                    nq=checked(nq+plain);hq=checked(hq+high);here=checked(here+plain+high);
                }
                if(here<=0)continue;
                if(!kinds.Contains(entry.Key.Kind))kinds.Add(entry.Key.Kind);
                if(entry.Snapshot.ObservedAt>at)at=entry.Snapshot.ObservedAt;
            }
            if(nq+hq>0)rows.Add(new(group.Key,nq,hq,kinds,at));
        }
        return rows.OrderByDescending(row=>row.Nq+row.Hq).ToArray();
    }
}
