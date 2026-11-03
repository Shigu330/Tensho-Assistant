namespace TenshoAssistant.Inventory;

/// <summary>One item spread over several stacks of a storage that could hold it in fewer: stacks now, stacks needed, slots freed.</summary>
public sealed record MergeableStack(string Storage,uint ItemId,bool Hq,int Stacks,long Quantity,int Needed)
{
    public int Freed=>Stacks-Needed;
}

/// <summary>An item the armoire takes, found in another storage: already in the armoire (true: this copy is spare), not (false), unknown (null).</summary>
public sealed record ArmoireCandidate(string Storage,uint ItemId,bool Hq,bool? InArmoire);

/// <summary>How an item looks (Item.ModelMain, ModelSub, EquipSlotCategory) and how many dye channels it has (not part of the look).</summary>
public readonly record struct ItemLook(ulong Main,ulong Sub,uint Slot,byte Dyes);

/// <summary>
/// g, I-10 (0.348): storage report, nothing moved (user's choice). Stacks of one item and quality spread over more slots than their
/// total needs (stack size from the game data), per storage; and armoury pieces used by no gear set. Original code (ideas from
/// XIVDupeFinder and InventoryCleaner, read only).
/// </summary>
public static class StorageReport
{
    /// <summary>Per storage, the items whose stacks could be merged; a stack size of 1 or unknown (0) is never merged.</summary>
    public static IReadOnlyList<MergeableStack> Mergeable(IEnumerable<(string Storage,IEnumerable<InventoryStack> Stacks)> storages,Func<uint,uint> stackSize)
    {
        var found=new List<MergeableStack>();
        foreach(var (storage,stacks) in storages)
            foreach(var group in stacks.Where(stack=>stack.Quantity>0).GroupBy(stack=>stack.Item))
            {
                var count=group.Count();
                if(count<2)continue;
                var size=stackSize(group.Key.Id.Value);
                if(size<=1)continue;
                var total=group.Sum(stack=>stack.Quantity);
                var needed=(int)((total+size-1)/size);
                if(needed<count)found.Add(new(storage,group.Key.Id.Value,group.Key.IsHighQuality,count,total,needed));
            }
        return found.OrderByDescending(entry=>entry.Freed).ThenBy(entry=>entry.Storage,StringComparer.CurrentCulture).ThenBy(entry=>entry.ItemId).ToArray();
    }

    /// <summary>Glamour dresser slots (FFXIVClientStructs MirageManager._prismBoxItemIds: FixedSizeArray800); the bank numbers outfit pieces after them.</summary>
    public const int DresserSlots=800;

    /// <summary>
    /// g, C-03 (0.357): items the armoire takes (Cabinet sheet) kept in another storage, once per storage, item and quality. Pieces of a
    /// gear set are left out (moving them breaks the set); the armoire not read yet (null) leaves every « in armoire » unknown. Spare
    /// copies (already in the armoire) first.
    /// </summary>
    public static IReadOnlyList<ArmoireCandidate> ForArmoire(IEnumerable<(string Storage,IEnumerable<InventoryStack> Stacks)> storages,
        IReadOnlySet<uint> cabinetItems,IReadOnlySet<uint>? armoire,IReadOnlySet<uint>? gearsetItems)
    {
        var found=new List<ArmoireCandidate>();
        foreach(var (storage,stacks) in storages)
            foreach(var item in stacks.Where(stack=>stack.Quantity>0 && cabinetItems.Contains(stack.Item.Id.Value)
                && gearsetItems?.Contains(stack.Item.Id.Value)!=true).Select(stack=>stack.Item).Distinct())
                found.Add(new(storage,item.Id.Value,item.IsHighQuality,armoire is null ? null:armoire.Contains(item.Id.Value)));
        return found.OrderByDescending(entry=>entry.InArmoire==true).ThenBy(entry=>entry.Storage,StringComparer.CurrentCulture).ThenBy(entry=>entry.ItemId).ToArray();
    }

    /// <summary>
    /// g, C-07 (0.358): items of the glamour dresser that look the same (same models, same equipment slot), by group of two or more;
    /// items without a model or a slot are left out. Largest groups first. Recolours (other variant of a model) are not grouped.
    /// </summary>
    public static IReadOnlyList<uint[]> SameLook(IEnumerable<uint> items,Func<uint,ItemLook?> look)
        =>items.Distinct().Select(id=>(Id:id,Look:look(id))).Where(entry=>entry.Look is { Main:not 0,Slot:not 0 })
            .GroupBy(entry=>(entry.Look!.Value.Main,entry.Look.Value.Sub,entry.Look.Value.Slot)).Where(group=>group.Count()>1)
            .Select(group=>group.Select(entry=>entry.Id).Order().ToArray()).OrderByDescending(group=>group.Length).ThenBy(group=>group[0]).ToArray();

    /// <summary>Armoury pieces (base item id) that no gear set uses; soul crystals are left out (they follow the job, not a set).</summary>
    public static IReadOnlyList<InventoryStack> OutsideGearsets(IEnumerable<InventoryContainerSnapshot> armoury,IReadOnlySet<uint> gearsetItems)
        =>armoury.Where(container=>container.Location.Container.Value!=3400 && container.Coverage!=InventoryCoverage.Unknown)
            .SelectMany(container=>container.Stacks).Where(stack=>stack.Quantity>0 && !gearsetItems.Contains(stack.Item.Id.Value))
            .OrderBy(stack=>stack.Item.Id.Value).ToArray();
}
