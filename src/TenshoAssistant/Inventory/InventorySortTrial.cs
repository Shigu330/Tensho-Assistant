namespace TenshoAssistant.Inventory;

/// <summary>X-394 (review of 0.1361, R37): the sample inventory of the desktop application's « SortPreview-trial » picture (never in the
/// game): one character's four bags, read complete, with shards, crystals and clusters scattered over them, each in a slot of its own and
/// its displayed place given, so the preview shows its frames in fill order (bags 1, 3, 2, 4) and the stacks the sort would move.</summary>
public static class InventorySortTrial
{
    public const ulong Character=1001;
    public const int Stacks=30;
    /// <summary>The sort's line after « Apply » on the bags (InventoryVisualSortBridge), pictured once even in developer mode (X-385).</summary>
    public const string Message="Visual sort applied to the four bags; refreshed within a second.";

    public static InventorySnapshot Snapshot(DateTimeOffset at)
    {
        var rows=new List<(int Bag,InventoryStack Stack)>();
        for(var index=0;index<Stacks;index++)
        {
            // Spread over the four bags (more in the first two), slots apart; ids 2 to 19: the game's shards, crystals and clusters.
            var bag=(index%7) switch{0 or 1 or 2=>0,3 or 4=>1,5=>2,_=>3};
            var slot=index*3%35;
            while(rows.Any(row=>row.Bag==bag && row.Stack.Slot==slot))slot=(slot+1)%35;
            var item=(uint)(2+index*5%18);
            rows.Add((bag,new InventoryStack(new(new(item),false),index*37%999+1,slot,bag*35+slot)));
        }
        var owner=new InventoryOwnerId(Character);
        return new InventorySnapshot(at,"desktop trial",Enumerable.Range(0,4).Select(bag=>new InventoryContainerSnapshot(new(owner,new(bag)),InventoryCoverage.Complete,
            rows.Where(row=>row.Bag==bag).Select(row=>row.Stack))));
    }
}
