using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// X-420 (0.1398, review of 0.1390 to 0.1397, I1; R28.14): « Erase this character » when another client or version left a row of another character
// that cannot be read at all (not JSON, another schema, a key disagreeing with its entry): the erase is done, the erased character is no longer
// shown, the answer is « Character erased. », and a second erase works the same way. X-418 (BankErase1391Tests) covered only rows beyond the checks.
internal static class BankEraseRows1398Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static T Await<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();

    private static BankEntry Entry(ulong character, BankStorageKind kind, ulong storage = 0)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner, new(id)), InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057), false), 3, 0)] : [])).ToArray();
        return new(new(character, kind, storage), kind == BankStorageKind.Retainer ? "Servant" : "Name (World)", new(DateTimeOffset.UtcNow, "test", containers));
    }

    private static void InTemp(Action<Func<PerCharacterBankStore>, string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-bank-1398-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, TenshoDataDatabase.FileName);
        try { run(() => new PerCharacterBankStore(Path.Combine(root, "InventoryBank"), Path.Combine(root, "InventoryBank.json"), 10_000, new TenshoDataFile(database, (error, message) => { })), database); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SqliteConnection Open(string database)
    {
        var open = new SqliteConnection("Data Source=" + database + ";Pooling=False");
        open.Open();
        return open;
    }

    private static int RowsOf(string database, long character)
    {
        using var open = Open(database);
        using var command = open.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM bank_entries WHERE character=$c;";
        command.Parameters.AddWithValue("$c", character);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    // What another client or version could leave: one row of character 2 written straight into the data file.
    private static void Plant(string database, long kind, long storage, string entry)
    {
        using var open = Open(database);
        using var command = open.CreateCommand();
        command.CommandText = "INSERT INTO bank_entries(character,kind,storage,seq,entry) VALUES(2,$k,$s,0,$e);";
        command.Parameters.AddWithValue("$k", kind);
        command.Parameters.AddWithValue("$s", storage);
        command.Parameters.AddWithValue("$e", entry);
        command.ExecuteNonQuery();
    }

    private static void ErasesWith(Func<string, string> row, string what, long storage = 0) => InTemp((store, database) =>
    {
        using var bank = new InventoryBankService(store());
        bank.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        Check(bank.State.Error is null, what + ": bank loaded: " + bank.State.Error);
        Check(Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag))).Success && Await(bank.ImportAsync(Entry(3, BankStorageKind.Saddlebag))).Success
              && Await(bank.ImportAsync(Entry(2, BankStorageKind.Saddlebag))).Success, what + ": three characters saved");
        string good;
        using (var open = Open(database))
        using (var command = open.CreateCommand())
        {
            command.CommandText = "SELECT entry FROM bank_entries WHERE character=2;";
            good = (string)command.ExecuteScalar()!;
            command.CommandText = "DELETE FROM bank_entries WHERE character=2;";
            command.ExecuteNonQuery();
        }
        Plant(database, (long)BankStorageKind.Saddlebag, storage, row(good));
        var erased = Await(bank.EraseCharacterAsync(1));
        Check(erased.Success && erased.Message == "Character erased.", what + ": the answer says the erase is done: " + erased.Message);
        Check(bank.State.Entries.All(entry => entry.Key.CharacterId != 1), what + ": the erased character is no longer shown");
        Check(bank.State.Entries.Any(entry => entry.Key.CharacterId == 3) && !bank.State.Loading, what + ": the other character still shown, nothing left loading");
        Check(RowsOf(database, 1) == 0 && RowsOf(database, 3) == 1 && RowsOf(database, 2) == 1, what + ": erased in the data file, the others kept as they are");
        var again = Await(bank.EraseCharacterAsync(1));
        Check(again.Success && again.Message == "Character erased.", what + ": a second erase answers the same: " + again.Message);
        var third = Await(bank.EraseCharacterAsync(3));
        Check(third.Success && third.Message == "Character erased." && RowsOf(database, 3) == 0 && bank.State.Entries.All(entry => entry.Key.CharacterId != 3),
            what + ": another character's erase works the same: " + third.Message);
    });

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bank erase rows 1398: behaviour: a row of another character that is not JSON", () => ErasesWith(_ => "{not json", "not JSON")),
        ("Bank erase rows 1398: behaviour: a row of another schema (another version)", () => ErasesWith(_ => "{\"schema\":99,\"other\":[1,2,3]}", "another schema")),
        ("Bank erase rows 1398: behaviour: a row whose key disagrees with its entry", () => ErasesWith(good => good, "key disagreeing", 7)),
    ];
}
