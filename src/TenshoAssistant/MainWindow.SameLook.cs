using Dalamud.Bindings.ImGui;
using TenshoAssistant.Inventory;

namespace TenshoAssistant;

// X-216 (0.1783, the user's request of 12/11, K-26), first part: on any item sheet of an equipable piece, whether the character logged in
// already holds a piece of the same look (same models and slot), and where: glamour dresser, armoire, bags, armoury, saddlebags, retainers,
// as last read. Built once per change of those storages, the looks of the sheets kept (no sheet read in Draw after the first time).
internal sealed partial class MainWindow
{
    private (InventorySnapshot? Live,BankState Bank,ulong Character)? lookIndexKey;
    private LookIndex? lookIndex;
    private readonly Dictionary<uint,ItemLook?> lookOfItem=[];

    private ItemLook? LookOf(uint id)
    {
        if(lookOfItem.TryGetValue(id,out var known))return known;
        var look=ItemLookOf?.Invoke(id);
        if(lookOfItem.Count<20_000)lookOfItem[id]=look;
        return look;
    }

    // The storages of the character logged in: the live bags, then every storage saved (not the currencies nor the crystals).
    private LookIndex? LookIndexNow()
    {
        var character=bankBridge.State.CharacterId;
        if(character==0 || ItemLookOf is null)return null;
        var key=(inventory.Current,bank.State,character);
        if(lookIndexKey==key)return lookIndex;
        var places=new List<(string,IEnumerable<uint>)>();
        if(inventory.Current is { } live)
            places.Add((localization.Get("BankUi.Bags"),live.Containers.Values.Where(container=>container.Location.Owner.Value==character
                && container.Location.Container.Value is >= 0 and <= 3 && container.Coverage!=InventoryCoverage.Unknown).SelectMany(container=>container.Stacks).Select(stack=>stack.Item.Id.Value)));
        foreach(var entry in bank.State.Entries.Where(entry=>entry.Key.CharacterId==character && entry.Key.Kind is not (BankStorageKind.Currency or BankStorageKind.Crystals or BankStorageKind.FreeCompanyChest)
            && !(entry.Key.Kind==BankStorageKind.Bags && inventory.Current is not null)))
            places.Add((BankEntryName(entry),entry.Snapshot.Containers.Values.SelectMany(container=>container.Stacks).Where(stack=>stack.Quantity>0).Select(stack=>stack.Item.Id.Value)));
        lookIndex=LookIndex.Build(places,LookOf);
        lookIndexKey=key;
        return lookIndex;
    }

    /// <summary>X-216: « Same look » with a check (held, and where) or a cross; nothing for an item without a look, or nobody logged in.</summary>
    private void DrawItemSameLook(uint id)
    {
        if(LookOf(id) is not { Main:not 0,Slot:not 0 } look || LookIndexNow() is not { } index)return;
        var held=index.Find(look);
        DrawStateMark(held.Count>0);
        ImGui.SameLine();
        var items=itemCatalog.State.Catalog;
        var shown=held.Take(3).Select(piece=>piece.Item==id ? piece.Place:$"{piece.Place} ({items?.Find(piece.Item)?.Name ?? $"#{piece.Item}"})");
        ImGui.TextDisabled(held.Count==0 ? localization.Get("HoverUi.SameLookNone")
            :localization.Format("HoverUi.SameLookHeld",string.Join(", ",shown)+(held.Count>3 ? " "+localization.Format("HoverUi.SameLookMore",held.Count-3):"")));
        Tip("HoverUi.SameLookTip");
    }
}
