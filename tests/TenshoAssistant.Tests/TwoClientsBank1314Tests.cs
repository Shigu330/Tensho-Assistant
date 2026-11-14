using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;

// 0.1314 (X-171, bank cache between two clients): with the data file, each change reads every character back as saved now, under the same lock:
// the bank a client shows holds what the other client saved (or erased) since its load. Before, the other characters stayed as this client had
// loaded them until a reload. Behaviour tests (R28.14): two stores and two services on one real data file.
internal static class TwoClientsBank1314Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static BankEntry Entry(ulong character,BankStorageKind kind,ulong storage = 0)
    {
        var owner = new InventoryOwnerId(kind == BankStorageKind.Retainer ? storage : character);
        var containers = BankStorageScope.ContainerIds(kind).Select(id => new InventoryContainerSnapshot(new(owner,new(id)),InventoryCoverage.Complete,
            id == BankStorageScope.ContainerIds(kind)[0] ? [new InventoryStack(new(new(5057),false),3,0)] : [])).ToArray();
        return new(new(character,kind,storage),kind == BankStorageKind.Retainer ? "Servant" : "Name (World)",new(DateTimeOffset.UtcNow,"test",containers));
    }

    private static void InTemp(Action<Func<PerCharacterBankStore>> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-bank-1314-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = Path.Combine(root,TenshoDataDatabase.FileName);
        try { run(() => new PerCharacterBankStore(Path.Combine(root,"InventoryBank"),Path.Combine(root,"InventoryBank.json"),10_000,new TenshoDataFile(database,(error,message) => { }))); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static ulong[] Characters(IEnumerable<BankEntry> entries) => [.. entries.Select(entry => entry.Key.CharacterId).Distinct().Order()];

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Two clients bank 1314: behaviour: a change shows what the other client saved and erased since the load (every character read back)",() => InTemp(store =>
        {
            var a = store();
            var b = store();
            var shownA = a.Load();
            var shownB = b.Load();
            Check(shownA.Length == 0 && shownB.Length == 0,"empty bank on both");
            shownB = b.Apply(new BankChange(Put: Entry(2,BankStorageKind.Saddlebag)),shownB);
            shownA = a.Apply(new BankChange(Put: Entry(1,BankStorageKind.Saddlebag)),shownA);
            Check(Characters(shownA).SequenceEqual(new ulong[] { 1,2 }),"client A shows B's character too: " + string.Join(",",Characters(shownA)));
            shownB = b.Apply(new BankChange(Erase: 2),shownB);
            shownA = a.Apply(new BankChange(Put: Entry(1,BankStorageKind.Retainer,77)),shownA);
            Check(Characters(shownA).SequenceEqual(new ulong[] { 1 }) && shownA.Length == 2,"B's erase seen by A at its next change, nothing brought back: " + shownA.Length);
            Check(Characters(store().Load()).SequenceEqual(new ulong[] { 1 }),"saved: character 1 only");
        })),
        ("Two clients bank 1314: behaviour: two services: an import on one client shows the other client's characters saved since",() => InTemp(store =>
        {
            using var a = new InventoryBankService(store(),TimeSpan.FromMilliseconds(100));
            using var b = new InventoryBankService(store(),TimeSpan.FromMilliseconds(100));
            a.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            b.Ready.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
            Check(b.ImportAsync(Entry(5,BankStorageKind.Bags)).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult().Success,"B saved character 5");
            Check(a.ImportAsync(Entry(4,BankStorageKind.Bags)).WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult().Success,"A saved character 4");
            Check(Characters(a.State.Entries).SequenceEqual(new ulong[] { 4,5 }),"A's bank shows both: " + string.Join(",",Characters(a.State.Entries)));
        })),
    ];
}
