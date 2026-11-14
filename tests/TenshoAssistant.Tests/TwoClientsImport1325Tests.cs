using Microsoft.Data.Sqlite;
using TenshoAssistant.Discoveries;

// 0.1325 (X-330, review of 0.1318 I1): X-171's « add both » covered sightings only. A fact changed by an import, or by the join of the common
// file, had no base: its row was replaced whole and could erase what the other client of the profile had saved since. Now import and join note
// the fact as it was before (DiscoveryStore.Mine), and the save adds the change to the row as saved now (AddMine). Behaviour tests (R28.14):
// two stores on one real SQLite file, in turn.
internal static class TwoClientsImport1325Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026,11,14,4,0,0,TimeSpan.Zero);

    private static void InTemp(Action<DiscoveryDatabase> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-discoveries-1325-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName))); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static DiscoveryStore Client(DiscoveryDatabase database)
    {
        var store = new DiscoveryStore();
        store.Load(database.ReadMine());
        return store;
    }
    private static void Save(DiscoveryStore store,DiscoveryDatabase database)
    {
        var changed = store.ChangesToSave(out var stamp);
        try { database.Write(changed.Write,changed.Remove,changed.All,changed.Bases); }
        catch (Exception) { store.SaveFailed(changed); throw; }
        store.MarkSaved(stamp);
    }
    private static void See(DiscoveryStore store,uint subject,int times,int value = 1)
    {
        for (var i = 0; i < times; i++) store.SeeSource(DiscoveryKind.Desynthesis,subject,1,value,At.AddMinutes(i));
    }
    // A fact as another player's file holds it: seen by them, imported here.
    private static DiscoveryFact Theirs(uint subject,int seen,int imported = 0,int value = 5) => new()
    {
        Kind = DiscoveryKind.Desynthesis,Subject = subject,Detail = 1,Seen = seen,Imported = imported,LevelMin = value,LevelMax = value,
        First = At.AddDays(-3),Last = At.AddDays(-1),Values = new() { [value] = seen },
    };
    private static DiscoveryFact Row(DiscoveryDatabase database,uint subject)
    {
        var all = database.ReadMine().Facts.Concat(database.ReadShared(DiscoveryKind.Desynthesis).Facts);
        return all.Single(fact => fact.Subject == subject);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Two clients 1325: behaviour: B saves a sighting, then A imports a file holding the same fact and saves: B's sighting stays",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,1,2);
            Save(first,database);
            var a = Client(database);
            var b = Client(database);
            See(b,1,3);
            Save(b,database);
            var (added,joined,refused) = a.Merge([Theirs(1,4)]);
            Check(added == 0 && joined == 1 && refused == 0,"joined in A");
            Save(a,database);
            var row = Row(database,1);
            Check(row.Seen == 5,"2 + 3 of B kept, the import adds no sighting: " + row.Seen);
            Check(row.Imported == 1 && row.ImportedValues is { } imported && imported[5] == 4,"the import's part joined: " + row.Imported);
            Check(row.LevelMax == 5 && row.First == At.AddDays(-3),"levels and dates widened by the import");
            Save(a,database);
            Save(b,database);
            Check(Row(database,1).Seen == 5 && Row(database,1).Imported == 1,"saves without a change add nothing");
        })),
        ("Two clients 1325: behaviour: the common file's join of a fact B saw since A's load keeps B's row (sightings, not made shared)",() => InTemp(database =>
        {
            var a = Client(database);
            var b = Client(database);
            See(b,2,3);
            Save(b,database);
            var (added,_,_) = a.Merge([Theirs(2,6,imported: 9)],common: true);
            Check(added == 1,"new in A");
            Save(a,database);
            var row = Row(database,2);
            Check(row.Seen == 3,"B's three sightings kept: " + row.Seen);
            Check(row.Imported == 9,"the common file's count taken: " + row.Imported);
            Check(database.ReadMine().Facts.Any(fact => fact.Subject == 2),"still B's own fact, not a shared row");
        })),
        ("Two clients 1325: behaviour: both clients join the same common file: its count is taken once, never added",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,3,1);
            Save(first,database);
            var a = Client(database);
            var b = Client(database);
            a.Merge([Theirs(3,4,imported: 7)],common: true);
            b.Merge([Theirs(3,4,imported: 7)],common: true);
            See(b,3,2);
            Save(a,database);
            Save(b,database);
            var row = Row(database,3);
            Check(row.Imported == 7 && row.ImportedValues is { } imported && imported[5] == 4,"the largest import, not the sum: " + row.Imported);
            Check(row.Seen == 3,"1 + 2 of B: " + row.Seen);
        })),
        ("Two clients 1325: behaviour: an import saved first, a sighting of the other client saved after: both in the row",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,4,1);
            Save(first,database);
            var a = Client(database);
            var b = Client(database);
            a.Merge([Theirs(4,2)]);
            Save(a,database);
            See(b,4,4);
            Save(b,database);
            var row = Row(database,4);
            Check(row.Seen == 5 && row.Imported == 1,"1 + 4 sightings and A's import: " + row.Seen + " / " + row.Imported);
        })),
    ];
}
