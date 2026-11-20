namespace TenshoAssistant.Inventory;

/// <summary>X-394 (review of 0.1361, R37): the sample inventory of the desktop application's « SortPreview-trial » picture (never in the
/// game): one character's four bags, read complete, with shards, crystals and clusters scattered over them, each in a slot of its own and
/// its displayed place given, so the preview shows its frames in fill order (bags 1, 3, 2, 4) and the stacks the sort would move.
/// X-647 (review of 0.1592): 75 stacks, more than two bags hold, so the sorted stacks fill bag 1, then bag 3, then five in bag 2.</summary>
public static class InventorySortTrial
{
    public const ulong Character=1001;
    public const int Stacks=75;
    /// <summary>The sort's line after « Apply » on the bags (InventoryVisualSortBridge), pictured once even in developer mode (X-385).</summary>
    public const string Message="Visual sort applied to the four bags; refreshed within a second.";

    /// <summary>Slots of one bag.</summary>
    public const int SlotsPerBag=35;

    public static InventorySnapshot Snapshot(DateTimeOffset at)=>Snapshot(at,Stacks);

    /// <summary>X-649 (review of 0.1593, R07): the free slot is looked for over the bag's 35 slots at most; a sample with more stacks than a
    /// bag holds fails with a clear message instead of looping.</summary>
    public static InventorySnapshot Snapshot(DateTimeOffset at,int stacks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stacks);
        var rows=new List<(int Bag,InventoryStack Stack)>();
        for(var index=0;index<stacks;index++)
        {
            // Spread over the four bags (more in the first two), slots apart; ids 2 to 19: the game's shards, crystals and clusters.
            var bag=(index%7) switch{0 or 1 or 2=>0,3 or 4=>1,5=>2,_=>3};
            var slot=-1;
            for(var step=0;step<SlotsPerBag && slot<0;step++)
            {
                var candidate=(index*3+step)%SlotsPerBag;
                if(!rows.Any(row=>row.Bag==bag && row.Stack.Slot==candidate))slot=candidate;
            }
            if(slot<0)throw new InvalidOperationException($"The sample inventory holds at most {SlotsPerBag} stacks per bag; bag {bag+1} is full at stack {index+1} of {stacks}.");
            var item=(uint)(2+index*5%18);
            rows.Add((bag,new InventoryStack(new(new(item),false),index*37%999+1,slot,bag*35+slot)));
        }
        var owner=new InventoryOwnerId(Character);
        return new InventorySnapshot(at,"desktop trial",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(owner,new(bag)),InventoryCoverage.Complete,
            rows.Where(row=>row.Bag==bag).Select(row=>row.Stack))));
    }
}
