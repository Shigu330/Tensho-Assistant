using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// X-414 (0.1386, review of 0.1375 to 0.1382, I3; R28.14): behaviour tests of X-336 M12 on a real data file. A bank that fails the checks of
// the whole list only (more than 4096 entries over every character, each character valid alone; a row unreadable on its own is already
// refused when read) makes this character's change fail whole, nothing written, as Load fails closed on it; the player's erase is never
// blocked by it (done first), and the bank read back after the erase is checked again (0.1380 returned it unchecked).
internal static class BankValidation1386Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static BankEntry Entry(ulong character, BankStorageKind kind, ulong storage = 0)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner, new(id)), InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057), false), 3, 0)] : [])).ToArray();
        return new(new(character, kind, storage), kind == BankStorageKind.Retainer ? "Servant" : "Name (World)", new(DateTimeOffset.UtcNow, "test", containers));
    }

    private static void InTemp(Action<Func<PerCharacterBankStore>, string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-bank-1386-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, TenshoDataDatabase.FileName);
        try { run(() => new PerCharacterBankStore(Path.Combine(root, "InventoryBank"), Path.Combine(root, "InventoryBank.json"), 10_000, new TenshoDataFile(database, (error, message) => { })), database); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static List<(long Character, long Kind, string Entry)> Saved(string database)
    {
        using var open = new SqliteConnection("Data Source=" + database + ";Pooling=False");
        open.Open();
        using var command = open.CreateCommand();
        command.CommandText = "SELECT character,kind,entry FROM bank_entries ORDER BY character,kind,storage;";
        var rows = new List<(long, long, string)>();
        using var reader = command.ExecuteReader();
        while (reader.Read()) rows.Add((reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2)));
        return rows;
    }

    /// <summary>Another client saves character 2 with 4096 retainers, valid alone (a whole-list Save checks its own list only): with this
    /// client's characters, the bank holds more than the 4096 entries the whole list may hold.</summary>
    private static void Crowd(PerCharacterBankStore other)
    {
        other.Load();
        other.Save([.. Enumerable.Range(1, 4096).Select(storage => Entry(2, BankStorageKind.Retainer, (ulong)storage))]);
    }

    private static Exception? Fails(Action run) { try { run(); return null; } catch (Exception error) { return error; } }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bank validation 1386: behaviour: another character's invalid row: this character's change fails whole, nothing written", () => InTemp((store, database) =>
        {
            var a = store();
            var shownA = a.Load();
            shownA = a.Apply(new BankChange(Put: Entry(1, BankStorageKind.Saddlebag)), shownA);
            Crowd(store());
            var before = Saved(database);
            Check(before.Count == 4097, "4097 rows saved: " + before.Count);
            var failed = Fails(() => a.Apply(new BankChange(Put: Entry(1, BankStorageKind.Retainer, 77)), shownA));
            Check(failed is ArgumentException && failed.Message.Contains("Too many bank entries", StringComparison.Ordinal), "the change fails on the whole list: " + failed?.Message);
            var after = Saved(database);
            Check(after.SequenceEqual(before) && after.Count(row => row.Character == 1) == 1, "nothing written: " + after.Count(row => row.Character == 1) + " row(s) of character 1");
            Check(Fails(() => store().Load()) is not null, "Load fails closed on the same rows (the bank unavailable)");
        })),
        ("Bank validation 1386: behaviour: the erase is done whatever the other characters hold, the bank read back is checked", () => InTemp((store, database) =>
        {
            var a = store();
            var shownA = a.Load();
            shownA = a.Apply(new BankChange(Put: Entry(1, BankStorageKind.Saddlebag)), shownA);
            shownA = a.Apply(new BankChange(Put: Entry(3, BankStorageKind.Saddlebag)), shownA);
            Crowd(store());
            // X-418 (0.1391): the bank read back fails the checks: not shown, and no failure: the other characters as this client holds them.
            var shown = a.Apply(new BankChange(Erase: 1), shownA);
            Check(shown.Length == 1 && shown[0].Key.CharacterId == 3,
                "the bank read back after the erase is checked (0.1380 returned it unchecked): not shown, this client's other characters instead: " + shown.Length);
            var after = Saved(database);
            Check(after.All(row => row.Character != 1) && after.Count == 4097, "the erase of character 1 is done, the others kept: " + after.Count);
        })),
        ("Bank validation 1386: behaviour: a valid bank: an erase returns the other characters, checked", () => InTemp((store, database) =>
        {
            var a = store();
            var shown = a.Load();
            shown = a.Apply(new BankChange(Put: Entry(1, BankStorageKind.Saddlebag)), shown);
            shown = a.Apply(new BankChange(Put: Entry(2, BankStorageKind.Saddlebag)), shown);
            shown = a.Apply(new BankChange(Erase: 1), shown);
            Check(shown.Length == 1 && shown[0].Key.CharacterId == 2 && Saved(database).Count == 1, "character 2 left, shown and saved");
        })),
    ];
}
