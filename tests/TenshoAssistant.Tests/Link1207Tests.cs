using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Inventory;
using TenshoAssistant.Link;

// 0.1207 (X-184 N6 and N21, night review of 12/11): the local link. A category the application does not know (a newer plugin's) refused
// every snapshot and left it unlinked for the session; it is now left out. The data file's journal (-wal), made and removed at every
// opening, a read too, made the application copy the whole data file each time; it is no notice any more, a real write still is (the .db).
internal static class Link1207Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static LinkSnapshot Sample(IReadOnlyList<LinkChange>? changes = null)
        => new(LocalLinkProtocol.Version, "0.1207.0", true, false, 132, "New Gridania", 7, null, changes);
    private static bool Wait(Func<bool> done)
    {
        var until = Environment.TickCount64 + 10_000;
        while (Environment.TickCount64 < until) { if (done()) return true; Thread.Sleep(50); }
        return done();
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Link 1207: behaviour: a newer plugin's category is left out, the snapshot read; through a real pipe the application stays linked", () =>
        {
            Check(LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample([new LinkChange("bank", 3), new LinkChange("future-x", 1)])), out var read, out var error)
                && read!.Changes!.Select(change => change.Category).SequenceEqual(["bank"]) && read.Changes![0].Generation == 3, "only the known one kept: " + error);
            Check(!LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample([new LinkChange("bank", 0)])), out _, out var malformed) && malformed == "change list refused", "a malformed notice still refuses the line");
            var many = Enumerable.Range(1, LocalLinkProtocol.MaxChanges + 4).Select(index => new LinkChange(index % 2 == 0 ? "bank" : "future-" + index, index)).ToArray();
            Check(LocalLinkProtocol.TryParse(LocalLinkProtocol.Serialize(Sample(many)), out var bounded, out _) && bounded!.Changes!.All(change => change.Category == "bank") && bounded.Changes!.Count <= LocalLinkProtocol.MaxChanges, "bounded, unknown ones out");
            if (!OperatingSystem.IsWindows()) return;
            var name = "TenshoAssistant.Link.Test." + Guid.NewGuid().ToString("N")[..12];
            using var server = new LocalLinkServer(name);
            using var client = new LocalLinkClient(name);
            server.Publish(Sample([new LinkChange("bank", 1), new LinkChange("future-x", 1)]));
            Check(Wait(() => client.Current is not null), "linked although the snapshot carries a category it does not know: " + client.Problem);
            Check(client.Current!.Changes!.Select(change => change.Category).SequenceEqual(["bank"]) && client.Problem is null, "the known notice passed on, no problem said");
        }),
        ("Link 1207: behaviour: the data file's journal is no notice; a real write still changes the .db, a read leaves it as it was", () =>
        {
            var root = Path.Combine(Path.GetTempPath(), "tensho-link-1207-" + Guid.NewGuid().ToString("N"));
            var folder = Path.Combine(root, "TenshoAssistant"); Directory.CreateDirectory(folder);
            var settings = Path.Combine(root, "TenshoAssistant.json");
            var database = Path.Combine(folder, TenshoDataDatabase.FileName);
            try
            {
                foreach (var journal in new[] { "-wal", "-journal", "-shm" })
                    Check(LocalLinkProtocol.ChangeCategory(settings, folder, database + journal) is null, "no notice: " + journal);
                Check(LocalLinkProtocol.ChangeCategory(settings, folder, database) == "data", "the data file itself: « data »");
                var store = new GilJournalStore(Path.Combine(folder, "GilJournal"), new TenshoDataFile(database, (_, _) => { }));
                store.Save(7, [new GilEntry(DateTimeOffset.UtcNow, 10, 110, GilSource.Character, GilCategory.Other)]);
                SqliteConnection.ClearAllPools();
                var written = File.ReadAllBytes(database);
                Check(!File.Exists(database + "-wal") || new FileInfo(database + "-wal").Length == 0, "the journal folded in when the last connection closed");
                store.Save(7, [new GilEntry(DateTimeOffset.UtcNow, 10, 110, GilSource.Character, GilCategory.Other), new GilEntry(DateTimeOffset.UtcNow.AddSeconds(1), 5, 115, GilSource.Character, GilCategory.Other)]);
                SqliteConnection.ClearAllPools();
                Check(!File.ReadAllBytes(database).SequenceEqual(written), "a real write changes the .db itself: its notice is not lost");
                var before = File.ReadAllBytes(database);
                Check(store.Load(7, DateTimeOffset.UtcNow).Count == 2, "read back");
                SqliteConnection.ClearAllPools();
                Check(File.ReadAllBytes(database).SequenceEqual(before), "a read leaves the .db as it was: no notice, no copy");
            }
            finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
        }),
    ];
}
