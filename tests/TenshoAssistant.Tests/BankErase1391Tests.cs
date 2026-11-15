using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// X-418 (0.1391, review of 0.1383 to 0.1389, I2; R28.14): what the bank service shows and answers after « Erase this character » when another
// client left rows beyond the checks of the whole list (4096 retainers of another character): the erase is done, the erased character is no
// longer shown, the answer is « Character erased. », and a second erase or another character's erase works the same way.
internal static class BankErase1391Tests
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
        var root = Path.Combine(Path.GetTempPath(), "tensho-bank-1391-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, TenshoDataDatabase.FileName);
        try { run(() => new PerCharacterBankStore(Path.Combine(root, "InventoryBank"), Path.Combine(root, "InventoryBank.json"), 10_000, new TenshoDataFile(database, (error, message) => { })), database); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static int RowsOf(string database, long character)
    {
        using var open = new SqliteConnection("Data Source=" + database + ";Pooling=False");
        open.Open();
        using var command = open.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM bank_entries WHERE character=$c;";
        command.Parameters.AddWithValue("$c", character);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Bank erase 1391: behaviour: another client's rows beyond the checks: the service erases, hides the character and says so", () => InTemp((store, database) =>
        {
            using var bank = new InventoryBankService(store());
            bank.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            Check(bank.State.Error is null, "bank loaded: " + bank.State.Error);
            Check(Await(bank.ImportAsync(Entry(1, BankStorageKind.Saddlebag))).Success && Await(bank.ImportAsync(Entry(3, BankStorageKind.Saddlebag))).Success, "two characters saved");
            var other = store();
            other.Load();
            other.Save([.. Enumerable.Range(1, 4096).Select(storage => Entry(2, BankStorageKind.Retainer, (ulong)storage))]);
            var erased = Await(bank.EraseCharacterAsync(1));
            Check(erased.Success && erased.Message == "Character erased.", "the answer says the erase is done: " + erased.Message);
            Check(bank.State.Entries.All(entry => entry.Key.CharacterId != 1), "the erased character is no longer shown");
            Check(bank.State.Entries.Any(entry => entry.Key.CharacterId == 3) && !bank.State.Loading, "the other character still shown, nothing left loading");
            Check(RowsOf(database, 1) == 0 && RowsOf(database, 3) == 1 && RowsOf(database, 2) == 4096, "erased in the data file, the others kept");
            var again = Await(bank.EraseCharacterAsync(1));
            Check(again.Success && again.Message == "Character erased.", "a second erase answers the same: " + again.Message);
            var third = Await(bank.EraseCharacterAsync(3));
            Check(third.Success && bank.State.Entries.All(entry => entry.Key.CharacterId == 2) && bank.State.Entries.Count == 4096 && RowsOf(database, 3) == 0,
                "another character's erase: the bank read back now passes the checks (4096 entries) and is shown: " + third.Message + ", " + bank.State.Entries.Count);
        })),
    ];
}
