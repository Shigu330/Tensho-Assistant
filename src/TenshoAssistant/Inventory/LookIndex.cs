namespace TenshoAssistant.Inventory;

/// <summary>X-216: a piece held with a given look: the item and the place it is in.</summary>
public readonly record struct LookHeld(uint Item,string Place);

/// <summary>
/// X-216 (the user's request of 12/11, the comparison with other plugins K-26), first part: the pieces a character holds, by look (the same
/// models, Item.ModelMain and ModelSub, and the same equipment slot, EquipSlotCategory: what StorageReport.SameLook groups in the glamour
/// dresser), for « same look » on any item sheet. Built once per change of the storages read; items without a model or a slot are left out;
/// dyes aside (not part of the look). Pure (tests).
/// </summary>
public sealed class LookIndex
{
    private readonly Dictionary<(ulong Main,ulong Sub,uint Slot),List<LookHeld>> byLook=[];

    public int Pieces{get;private set;}

    public static LookIndex Build(IEnumerable<(string Place,IEnumerable<uint> Items)> places,Func<uint,ItemLook?> look)
    {
        var index=new LookIndex();
        foreach(var (place,items) in places)
            foreach(var item in items.Distinct())
            {
                if(item==0 || look(item) is not { Main:not 0,Slot:not 0 } seen)continue;
                var key=(seen.Main,seen.Sub,seen.Slot);
                if(!index.byLook.TryGetValue(key,out var list))index.byLook[key]=list=[];
                list.Add(new(item,place));
                index.Pieces++;
            }
        return index;
    }

    /// <summary>The pieces held with this look (the item itself included when held), in the order of the places; empty when none; a look
    /// without a model or a slot finds nothing.</summary>
    public IReadOnlyList<LookHeld> Find(ItemLook look)=>
        look is { Main:not 0,Slot:not 0 } && byLook.TryGetValue((look.Main,look.Sub,look.Slot),out var held) ? held:[];
}
