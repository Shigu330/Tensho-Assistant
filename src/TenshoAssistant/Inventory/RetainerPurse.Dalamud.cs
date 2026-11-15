using FFXIVClientStructs.FFXIV.Client.Game;

namespace TenshoAssistant.Inventory;

public static partial class RetainerPurse
{
    /// <summary>X-431 M2 (0.1416, from X-19 0.769): the open retainer's id and the gil in its own purse, which the game updates at once; null when no
    /// retainer is open, the purse is not loaded, or the game's functions did not resolve (R08). Framework thread only.</summary>
    public static unsafe (ulong Id,long Gil)? Open(RetainerManager* manager)
    {
        if(manager==null || (nint)InventoryManager.StaticAddressPointers.pInstance==0 || (nint)InventoryManager.MemberFunctionPointers.GetInventoryContainer==0)return null;
        var inventory=InventoryManager.Instance();
        var active=manager->GetActiveRetainer();
        if(inventory==null || active==null || active->RetainerId==0)return null;
        var purse=inventory->GetInventoryContainer(InventoryType.RetainerGil);
        if(purse==null || !purse->IsLoaded)return null;
        long held=0;
        for(var slot=0;slot<purse->Size;slot++)if(purse->GetInventorySlot(slot) is var item && item!=null && item->ItemId==1)held+=item->Quantity;
        return (active->RetainerId,held);
    }
}
