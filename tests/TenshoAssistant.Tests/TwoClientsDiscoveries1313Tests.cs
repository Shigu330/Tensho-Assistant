using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Discoveries;

// 0.1313 (X-171, user's rule of 11/13 « add both »): two game clients on one profile see the same discovery. Before, each wrote its whole fact
// and the last writer erased what the other had seen since its load. Now each save adds what this client saw since its last save to the row as
// saved now, read in the same transaction (DiscoveryStore.AddMine, DiscoveryDatabase.Write). Behaviour tests (R28.14): two stores on one real
// SQLite file, in turn, at the same time on two threads, after a failed save; nothing lost, nothing counted twice; imports never added up.
internal static class TwoClientsDiscoveries1313Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026,11,14,4,0,0,TimeSpan.Zero);

    private static void InTemp(Action<DiscoveryDatabase> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-discoveries-1313-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(new DiscoveryDatabase(Path.Combine(root,DiscoveryDatabase.FileName))); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    // A client: its store loaded from the file, as at a start.
    private static DiscoveryStore Client(DiscoveryDatabase database)
    {
        var store = new DiscoveryStore();
        store.Load(database.ReadMine());
        return store;
    }
    // A save as the plugin makes it (MainWindow.SaveDiscoveries).
    private static void Save(DiscoveryStore store,DiscoveryDatabase database)
    {
        var changed = store.ChangesToSave(out var stamp);
        try { database.Write(changed.Write,changed.Remove,changed.All,changed.Bases); }
        catch (Exception) { store.SaveFailed(changed); throw; }
        store.MarkSaved(stamp);
    }
    private static void See(DiscoveryStore store,uint subject,int times,int value = 1,bool hq = false)
    {
        for (var i = 0; i < times; i++) store.SeeSource(DiscoveryKind.Desynthesis,subject,1,value,At.AddMinutes(i),hq: hq);
    }
    private static DiscoveryFact Saved(DiscoveryDatabase database,uint subject) => database.ReadMine().Facts.Single(fact => fact.Subject == subject);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Two clients 1313: the sum: what this client saw since its last save is added to the row as saved now; imports take the largest",() =>
        {
            var stored = new DiscoveryFact { Kind = DiscoveryKind.Desynthesis,Subject = 1,Detail = 1,Seen = 7,Hq = 1,Values = new() { [10] = 6,[11] = 1 },Imported = 4,
                LevelMin = 10,LevelMax = 11,First = At,Last = At.AddHours(1),ImportedValues = new() { [10] = 4 } };
            var before = new DiscoveryFact { Kind = DiscoveryKind.Desynthesis,Subject = 1,Detail = 1,Seen = 5,Hq = 1,Values = new() { [10] = 5 },Imported = 4,
                LevelMin = 10,LevelMax = 10,First = At,Last = At.AddMinutes(30),ImportedValues = new() { [10] = 4 } };
            var mine = new DiscoveryFact { Kind = DiscoveryKind.Desynthesis,Subject = 1,Detail = 1,Seen = 8,Hq = 2,Values = new() { [10] = 7,[12] = 1 },Imported = 6,
                LevelMin = 10,LevelMax = 12,First = At.AddMinutes(-5),Last = At.AddHours(2),ImportedValues = new() { [10] = 6 } };
            var merged = DiscoveryStore.AddMine(stored,mine,before);
            Check(merged.Seen == 10,"7 saved + 3 of mine: " + merged.Seen);
            Check(merged.Values![10] == 8 && merged.Values[11] == 1 && merged.Values[12] == 1 && merged.Values.Count == 3,"values added per value");
            Check(merged.Hq == 2,"HQ 1 + 1: " + merged.Hq);
            Check(merged.Imported == 6 && merged.ImportedValues![10] == 6,"imports: the largest, never added");
            Check(merged.LevelMin == 10 && merged.LevelMax == 12 && merged.First == At.AddMinutes(-5) && merged.Last == At.AddHours(2),"levels and dates widened");
            var again = DiscoveryStore.AddMine(merged,mine,mine);
            Check(again.Seen == 10 && again.Hq == 2 && again.Values![10] == 8,"nothing new since the last save: nothing added");
            var fresh = DiscoveryStore.AddMine(stored,mine,null);
            Check(fresh.Seen == 15,"new in this client (the other made it meanwhile): all of mine added: " + fresh.Seen);
        }),
        ("Two clients 1313: behaviour: both clients load the same fact, each sees more and saves, in either order, several times: the file holds the sum",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,1,3);
            Save(first,database);
            var a = Client(database);
            var b = Client(database);
            See(a,1,2,value: 2);
            See(b,1,4,value: 3,hq: true);
            Save(a,database);
            Save(b,database);
            var saved = Saved(database,1);
            Check(saved.Seen == 9,"3 + 2 + 4: " + saved.Seen);
            Check(saved.Values![1] == 3 && saved.Values[2] == 2 && saved.Values[3] == 4 && saved.Hq == 4,"values and HQ of both");
            Save(a,database);
            Save(b,database);
            Check(Saved(database,1).Seen == 9,"saves without a new sighting add nothing");
            See(b,1,1);
            Save(b,database);
            See(a,1,1);
            Save(a,database);
            Check(Saved(database,1).Seen == 11,"later sightings of each added once: " + Saved(database,1).Seen);
            See(a,2,2);
            See(b,2,3);
            Save(b,database);
            Save(a,database);
            Check(Saved(database,2).Seen == 5,"a fact new in both clients: the sum: " + Saved(database,2).Seen);
            Check(Client(database).Summary().Sum(kind => kind.Total) == 2,"two facts, one row each");
        })),
        ("Two clients 1313: behaviour: two clients saving at the same time on two threads lose nothing and count nothing twice",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,1,5);
            Save(first,database);
            var clients = new[] { Client(database),Client(database) };
            var errors = new List<Exception>();
            var threads = clients.Select((client,side) => new Thread(() =>
            {
                try
                {
                    for (var round = 0; round < 25; round++)
                    {
                        See(client,1,1);
                        See(client,(uint)(100 + side),1);
                        Save(client,database);
                    }
                }
                catch (Exception error) { lock (errors) errors.Add(error); }
            })).ToArray();
            foreach (var thread in threads) thread.Start();
            foreach (var thread in threads) Check(thread.Join(TimeSpan.FromSeconds(60)),"both clients finished");
            Check(errors.Count == 0,"no save failed: " + string.Join("; ",errors.Select(error => error.Message)));
            Check(Saved(database,1).Seen == 55,"5 + 25 + 25 on the shared fact: " + Saved(database,1).Seen);
            Check(Saved(database,100).Seen == 25 && Saved(database,101).Seen == 25,"each client's own fact whole");
        })),
        ("Two clients 1313: behaviour: a save that failed is taken again from the fact as it was before it: each sighting counted once",() => InTemp(database =>
        {
            var first = Client(database);
            See(first,1,2);
            Save(first,database);
            var a = Client(database);
            var b = Client(database);
            See(a,1,3);
            var failed = a.ChangesToSave(out _);
            a.SaveFailed(failed);
            See(a,1,1);
            See(b,1,2);
            Save(b,database);
            Save(a,database);
            Check(Saved(database,1).Seen == 8,"2 + 3 + 1 of a + 2 of b: " + Saved(database,1).Seen);
            first.Clear();
            See(first,7,1);
            Save(first,database);
            Check(database.ReadMine().Facts.Count == 1 && Saved(database,7).Seen == 1,"a base cleared on purpose replaces everything, as before");
        })),
    ];
}
