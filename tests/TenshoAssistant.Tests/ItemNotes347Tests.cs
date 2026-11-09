using TenshoAssistant.Inventory;
using TenshoAssistant.Items;

// 0.347 (g, I-08): item notes and labels, bounded and copied on write; the shop reminder counts only the player's four known bags.
internal static class ItemNotes347Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item notes: set, emptied note removed, text bounded, item limit kept, copy on write",() =>
        {
            var first = ItemNotes.With(null,5057,new ItemNote("à garder",true,false))!;
            Check(first.Count == 1 && first[5057].Important,"note set");
            var second = ItemNotes.With(first,5057,new ItemNote("  ",false,false))!;
            Check(second.Count == 0 && first.Count == 1,"empty note removed, the previous dictionary untouched");
            Check(ItemNotes.With(null,1,new ItemNote(new string('x',900),false,false))![1].Text.Length == ItemNotes.MaximumText,"text bounded");
            var full = Enumerable.Range(1,ItemNotes.MaximumItems).ToDictionary(id => (uint)id,_ => new ItemNote("n",false,false));
            Check(ItemNotes.With(full,99999,new ItemNote("n",false,false)) is null && ItemNotes.With(full,1,new ItemNote("m",false,false)) is not null,"limit: no new item, edits allowed");
        }),
        ("Item notes: shop reminder lists labelled items of the four known bags only, with quantities",() =>
        {
            InventoryContainerSnapshot Bag(ulong owner,int container,InventoryCoverage coverage,params (uint Item,long Quantity)[] stacks)
                => new(new(new(owner),new(container)),coverage,stacks.Select((stack,slot) => new InventoryStack(new(new(stack.Item),false),stack.Quantity,slot)).ToArray());
            var live = new InventorySnapshot(DateTimeOffset.UtcNow,"test",
            [
                Bag(9,0,InventoryCoverage.Complete,(10,3),(20,1)),
                Bag(9,1,InventoryCoverage.Unknown),
                Bag(9,2001,InventoryCoverage.Complete,(10,7)),
                Bag(8,0,InventoryCoverage.Complete,(10,2)),
            ]);
            var notes = new Dictionary<uint,ItemNote> { [10] = new("",false,true),[20] = new("",true,false) };
            var (toSell,important) = ItemNotes.InBags(live,9,notes);
            Check(toSell.SequenceEqual([(10u,3L)]) && important.SequenceEqual([(20u,1L)]),"bags 0-3 of this character, known coverage only");
            Check(ItemNotes.InBags(live,0,notes).ToSell.Count == 0 && ItemNotes.InBags(null,9,notes).ToSell.Count == 0,"no character or no live inventory: nothing");
            Check(Read("Items/ItemNoteShopReminder.Dalamud.cs").Contains("open && !shopOpen",StringComparison.Ordinal),"once per shop opening");
            Check(!new TenshoAssistant.Configuration().ItemNoteImportantSound && new TenshoAssistant.Configuration().ItemNoteShopReminder,"sound off, reminder on by default");
            Check(TenshoAssistant.ConfigurationReset.Kept.Contains("ItemNotes"),"notes are saved data, kept by « restore every setting »");
        }),
    ];
}
