namespace TenshoAssistant.Inventory;

/// <summary>
/// X-307 (0.1394): one retainer as the game's item finder keeps it (ItemFinderModule.RetainerInventories, copied on the framework thread): the
/// 175 bag slots in order (item id as the game stores it, count; 0 = empty), the 14 equipped pieces and the 18 crystal counts. <see cref="Current"/>
/// is the game's IsRetainerCurrent (the copy matches the retainer's last known state).
/// </summary>
public sealed record FinderRetainer(ulong RetainerId,bool Current,IReadOnlyList<(uint Id,ushort Count)> Items,IReadOnlyList<uint> Equipped,IReadOnlyList<ushort> Crystals);

/// <summary>X-307: one retainer of the item finder against the bank's saved pages of the same retainer (null: not in the bank).</summary>
public sealed record FinderComparison(ulong RetainerId,bool Current,bool InBank,int FinderStacks,int BankStacks,bool SameItems,bool SameOrder,
    int FinderHighId,int BankHq,bool? SameCrystals,bool Packed=false,bool SameSequence=false,bool? SameDisplay=null);

/// <summary>
/// X-307 (0.1394, R06): pure comparison of the game's item finder copy of a retainer with the bank. The bank's pages are the retainer's seven
/// containers of 25 slots (10000 to 10006), so finder slot i is page i / 25, slot i % 25. An item id from <see cref="HighQualityOffset"/> on is
/// read as a high-quality item (counted apart, <see cref="FinderComparison.FinderHighId"/>; confirmed by the probe of 0.1394:
/// docs/evidence/0.1394-retainer-finder.md).
/// </summary>
public static class RetainerFinderCompare
{
    public const uint HighQualityOffset=1_000_000;
    public const int Slots=175,PageSize=25,Equipped=14,Crystals=18;

    /// <summary>The finder's stacks as (item, quantity, position), the HQ encoding decoded, empty slots left out.</summary>
    public static IReadOnlyList<(InventoryItem Item,long Quantity,int Position)> Stacks(FinderRetainer retainer)
        =>[.. retainer.Items.Select((slot,position)=>(slot,position)).Where(pair=>pair.slot.Id!=0 && pair.slot.Count>0)
            .Select(pair=>(Decode(pair.slot.Id),(long)pair.slot.Count,pair.position))];

    public static InventoryItem Decode(uint id)=>id>=HighQualityOffset ? new(new(id-HighQualityOffset),true):new(new(id),false);

    /// <summary>The bank's stacks of the retainer's pages as (item, quantity, position: page * 25 + slot; -1 when the slot is unknown).</summary>
    public static IReadOnlyList<(InventoryItem Item,long Quantity,int Position)> BankStacks(BankEntry entry)
        =>[.. entry.Snapshot.Containers.Values.Where(container=>container.Location.Container.Value is >=10000 and <=10006)
            .SelectMany(container=>container.Stacks.Select(stack=>(stack.Item,stack.Quantity,
                stack.Slot<0 ? -1:(container.Location.Container.Value-10000)*PageSize+stack.Slot)))];

    public static FinderComparison Compare(FinderRetainer finder,BankEntry? bank)
    {
        var mine=Stacks(finder);
        var highIds=mine.Count(stack=>stack.Item.IsHighQuality);
        if(bank is null)return new(finder.RetainerId,finder.Current,false,mine.Count,0,false,false,highIds,0,null);
        var theirs=BankStacks(bank);
        static string Key((InventoryItem Item,long Quantity,int Position) stack)=>$"{stack.Item.Id.Value}:{stack.Item.IsHighQuality}:{stack.Quantity}";
        var sameItems=mine.Select(Key).Order(StringComparer.Ordinal).SequenceEqual(theirs.Select(Key).Order(StringComparer.Ordinal),StringComparer.Ordinal);
        var sameOrder=sameItems && theirs.All(stack=>stack.Position>=0)
            && mine.OrderBy(stack=>stack.Position).Select(stack=>(Key(stack),stack.Position)).SequenceEqual(theirs.OrderBy(stack=>stack.Position).Select(stack=>(Key(stack),stack.Position)));
        // 0.1395: which order the copy keeps: packed from slot 0, the bank's slot order without the gaps, or the window's display order.
        var packed=mine.Select(stack=>stack.Position).SequenceEqual(Enumerable.Range(0,mine.Count));
        var mineSequence=mine.OrderBy(stack=>stack.Position).Select(Key).ToArray();
        var sameSequence=sameItems && theirs.All(stack=>stack.Position>=0) && mineSequence.SequenceEqual(theirs.OrderBy(stack=>stack.Position).Select(Key));
        var display=BankDisplay(bank);
        bool? sameDisplay=display is null ? null:sameItems && mineSequence.SequenceEqual(display);
        bool? sameCrystals=null;
        if(bank.Snapshot.Containers.Values.FirstOrDefault(container=>container.Location.Container.Value==BankStorageScope.RetainerCrystals) is { } crystals)
        {
            var saved=new long[Crystals];
            foreach(var stack in crystals.Stacks)if(stack.Slot is >=0 and <Crystals)saved[stack.Slot]+=stack.Quantity;
            sameCrystals=finder.Crystals.Count==Crystals && finder.Crystals.Select(count=>(long)count).SequenceEqual(saved);
        }
        return new(finder.RetainerId,finder.Current,true,mine.Count,theirs.Count,sameItems,sameOrder,highIds,theirs.Count(stack=>stack.Item.IsHighQuality),sameCrystals,
            packed,sameSequence,sameDisplay);
    }

    /// <summary>0.1395: the bank's pages in the window's display order (DisplayIndex), as keys; null when a stack has no display index.</summary>
    private static string[]? BankDisplay(BankEntry entry)
    {
        var stacks=entry.Snapshot.Containers.Values.Where(container=>container.Location.Container.Value is >=10000 and <=10006).SelectMany(container=>container.Stacks).ToArray();
        if(stacks.Length==0 || stacks.Any(stack=>stack.DisplayIndex<0))return null;
        return [.. stacks.OrderBy(stack=>stack.DisplayIndex).Select(stack=>$"{stack.Item.Id.Value}:{stack.Item.IsHighQuality}:{stack.Quantity}")];
    }

    /// <summary>The self-check's text: what the copy holds and how it compares with the bank (numbers only, no item or retainer name).</summary>
    public static string Summary(IReadOnlyList<FinderRetainer> finder,IReadOnlyList<FinderComparison> compared)
    {
        var inBank=compared.Where(row=>row.InBank).ToArray();
        return $"{finder.Count} retainers in the game's copy ({finder.Count(row=>row.Current)} current), {finder.Sum(row=>Stacks(row).Count)} stacks, "
            +$"{finder.Sum(row=>row.Equipped.Count(id=>id!=0))} equipped pieces; {inBank.Length} in the bank: same items {inBank.Count(row=>row.SameItems)}/{inBank.Length}, "
            +$"same slots {inBank.Count(row=>row.SameOrder)}/{inBank.Length}, high ids {compared.Sum(row=>row.FinderHighId)} (bank HQ {inBank.Sum(row=>row.BankHq)}), "
            +$"same crystals {inBank.Count(row=>row.SameCrystals==true)}/{inBank.Count(row=>row.SameCrystals is not null)}; "
            +$"packed {compared.Count(row=>row.Packed)}/{compared.Count}, same sequence as the bank's slots {inBank.Count(row=>row.SameSequence)}/{inBank.Length}, "
            +$"same display order {inBank.Count(row=>row.SameDisplay==true)}/{inBank.Count(row=>row.SameDisplay is not null)}";
    }
}
