using TenshoAssistant.Collections;

// 0.362 (g, C-05): glamour dresser outfits — owned, in the dresser, stored (a stored outfit counts all its pieces).
internal static class Outfits362Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Outfits: missing, every piece owned, ready in the dresser, stored",() =>
        {
            var outfit = new OutfitDefinition(900,[1,2,3]);
            Check(Outfits.Progress(outfit,new HashSet<uint>{1},new HashSet<uint>()) is { Owned:1,State:OutfitState.Missing },"missing");
            Check(Outfits.Progress(outfit,new HashSet<uint>{1,2,3},new HashSet<uint>{1}) is { Owned:3,InDresser:1,State:OutfitState.Complete },"every piece owned");
            Check(Outfits.Progress(outfit,new HashSet<uint>{1,2,3},new HashSet<uint>{1,2,3}).State == OutfitState.InDresser,"ready to store");
            Check(Outfits.Progress(outfit,new HashSet<uint>(),new HashSet<uint>{900}) is { Owned:3,Stored:true,State:OutfitState.Stored },"stored outfit counts its pieces");
            var page = Read("MainWindow.Outfits.cs");
            Check(page.Contains("entry.Key.Kind==BankStorageKind.GlamourDresser && stack.Slot<StorageReport.DresserSlots",StringComparison.Ordinal)
                && page.Contains("finally{ImGui.EndChild();}",StringComparison.Ordinal),"dresser slots only; child always ended");
            Check(Read("Plugin.cs").Contains("outfit.SetItemId!=0 && outfit.Pieces.Count>1",StringComparison.Ordinal),"outfits from MirageStoreSetItem");
        }),
    ];
}
