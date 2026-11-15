using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace TenshoAssistant.Inventory;

/// <summary>
/// X-307 (0.1394): the game's item finder copy of the retainers' bags (ItemFinderModule.RetainerInventories), copied on the framework thread
/// into managed data. Null when the module is not there (not logged in, or a game update moved it: R08, read as unavailable, never guessed).
/// </summary>
internal static unsafe class RetainerFinderReader
{
    public static IReadOnlyList<FinderRetainer>? Read()
    {
        var module=ItemFinderModule.Instance();
        if(module==null)return null;
        var found=new List<FinderRetainer>();
        foreach(var pair in module->RetainerInventories)
        {
            var (id,pointer)=pair;
            if(id==0 || pointer.Value==null)continue;
            var inventory=pointer.Value;
            var ids=inventory->ItemIds;var counts=inventory->ItemCount;
            if(ids.Length!=RetainerFinderCompare.Slots || counts.Length!=RetainerFinderCompare.Slots || inventory->EquippedItemIds.Length!=RetainerFinderCompare.Equipped
                || inventory->CrystalQuantities.Length!=RetainerFinderCompare.Crystals)return null;
            var items=new (uint,ushort)[ids.Length];
            for(var slot=0;slot<ids.Length;slot++)items[slot]=(ids[slot],counts[slot]);
            found.Add(new(id,module->IsRetainerCurrent(id),items,inventory->EquippedItemIds.ToArray(),inventory->CrystalQuantities.ToArray()));
            if(found.Count>64)return null; // a game has at most 10 retainers a character: anything far beyond is a moved structure.
        }
        return found.AsReadOnly();
    }
}
