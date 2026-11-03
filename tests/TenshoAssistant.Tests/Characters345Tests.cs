using TenshoAssistant.Inventory;

// 0.345 (g, I-03): one bank file per character, the former shared file split once (and kept), an erased character's file deleted,
// own bags / crystals / currencies recorded and shown for the other characters only, sort preview limited to sortable storages.
internal static class Characters345Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage = 0,uint item = 5057)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(item),false),3,0)] : [])).ToArray();
        return new(new(character,kind,storage),kind == BankStorageKind.Retainer ? "Servant" : "Nom (Monde)",new(DateTimeOffset.UtcNow,"test",containers));
    }

    private static string Folder()
    {
        var path = Path.Combine(Path.GetTempPath(),"tensho-bank-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Characters: one file per character, the former shared file split once and kept, an erased character's files deleted",() =>
        {
            var root = Folder();
            try
            {
                var legacy = Path.Combine(root,"InventoryBank.json");
                new FileInventoryBankStore(legacy).Save([Entry(11,BankStorageKind.Saddlebag),Entry(22,BankStorageKind.Retainer,99),Entry(22,BankStorageKind.Bags)]);
                var folder = Path.Combine(root,"InventoryBank");
                var store = new PerCharacterBankStore(folder,legacy);
                var loaded = store.Load();
                Check(loaded.Length == 3 && File.Exists(Path.Combine(folder,"11.json")) && File.Exists(Path.Combine(folder,"22.json")),"split into one file per character");
                Check(!File.Exists(legacy) && File.Exists(legacy + ".migrated"),"former file kept beside, not deleted");
                Check(!Directory.Exists(folder + ".migrating"),"temporary folder moved into place");
                store.Save(loaded.Where(entry => entry.Key.CharacterId != 11).ToArray());
                Check(!File.Exists(Path.Combine(folder,"11.json")) && File.Exists(Path.Combine(folder,"22.json")),"erased character: its file deleted, the other kept");
                var again = new PerCharacterBankStore(folder,legacy).Load();
                Check(again.Length == 2 && again.All(entry => entry.Key.CharacterId == 22),"reloaded from the remaining file; no second migration");
            }
            finally { Directory.Delete(root,true); }
        }),
        ("Characters: a file holding another character's entries fails closed; a failed load blocks every write",() =>
        {
            var root = Folder();
            try
            {
                var folder = Path.Combine(root,"InventoryBank");
                Directory.CreateDirectory(folder);
                new FileInventoryBankStore(Path.Combine(folder,"33.json")).Save([Entry(44,BankStorageKind.Saddlebag)]);
                var store = new PerCharacterBankStore(folder,Path.Combine(root,"InventoryBank.json"));
                var failed = false;
                try { store.Load(); } catch (InvalidDataException) { failed = true; }
                Check(failed,"mismatched owner refused");
                var blocked = false;
                try { store.Save([]); } catch (InvalidOperationException) { blocked = true; }
                Check(blocked && File.Exists(Path.Combine(folder,"33.json")),"nothing deleted after a failed load");
            }
            finally { Directory.Delete(root,true); }
        }),
        ("Characters: own bags, crystals and currencies saved (added last), never sorted, live for the logged-in character; erase queued",() =>
        {
            Check((int)BankStorageKind.Bags == 6 && (int)BankStorageKind.Currency == 8,"added last: saved kinds keep their numbers");
            Check(BankStorageScope.ContainerIds(BankStorageKind.Bags).SequenceEqual([0,1,2,3]) && BankStorageScope.ContainerIds(BankStorageKind.Crystals).SequenceEqual([2001]),"containers");
            Check(!BankStorageScope.Sortable(BankStorageKind.Bags) && BankStorageScope.Sortable(BankStorageKind.Retainer),"sort preview: sortable storages only");
            Check(Read("MainWindow.SortPreview.cs").Contains("BankStorageScope.Sortable(entry.Key.Kind)",StringComparison.Ordinal),"sort preview uses the list");
            Check(Read("MainWindow.Bank.cs").Contains("(!live || !BankStorageScope.IsCharacterOwn(entry.Key.Kind))",StringComparison.Ordinal),"no double count for the logged-in character");
            Check(Read("Inventory/BankService.cs").Contains("request.Erase != 0 ? state.Entries.Where(e => e.Key.CharacterId != request.Erase)",StringComparison.Ordinal),"erase on the bank's own queue");
            Check(Read("Plugin.cs").Contains("new PerCharacterBankStore(",StringComparison.Ordinal),"plugin uses one file per character");
            Check(Read("MainWindow.Characters.cs").Contains("characterEraseArmed==summary.Id",StringComparison.Ordinal),"erase needs a second click");
        }),
    ];
}
