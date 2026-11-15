using Microsoft.Data.Sqlite;
using TenshoAssistant.Discoveries;

// X-353 (0.1381, review of 0.1318 to 0.1329, minors): M2 (b) a two-client behaviour test of the shared file's join (R28.14); M4 the tool
// proposal form has its own prefix, which the labelling job leaves alone; M5 the allied societies catalogue's failure is logged.
internal static class ReviewMinors1381Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 14, 4, 0, 0, TimeSpan.Zero);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    private static void InTemp(Action<DiscoveryDatabase> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-discoveries-1381-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName))); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static DiscoveryStore Client(DiscoveryDatabase database)
    {
        var store = new DiscoveryStore();
        store.Load(database.ReadMine());
        return store;
    }
    private static void Save(DiscoveryStore store, DiscoveryDatabase database)
    {
        var changed = store.ChangesToSave(out var stamp);
        try { database.Write(changed.Write, changed.Remove, changed.All, changed.Bases); }
        catch (Exception) { store.SaveFailed(changed); throw; }
        store.MarkSaved(stamp);
    }
    private static void See(DiscoveryStore store, uint subject, int times)
    {
        for (var i = 0; i < times; i++) store.SeeSource(DiscoveryKind.Desynthesis, subject, 1, 1, At.AddMinutes(i));
    }
    private static DiscoveryFact Row(DiscoveryDatabase database, uint subject)
        => database.ReadMine().Facts.Concat(database.ReadShared(DiscoveryKind.Desynthesis).Facts).Single(fact => fact.Subject == subject);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1381 M2: behaviour: A joins a shared file's copy of a fact B saw since A's load: B's sightings stay", () => InTemp(database =>
        {
            var first = Client(database);
            See(first, 3, 2);
            Save(first, database);
            var a = Client(database);
            var b = Client(database);
            See(b, 3, 3);
            Save(b, database);
            Check(Row(database, 3).Seen == 5, "B saved: 2 + 3");
            var shared = new DiscoveryFact { Kind = DiscoveryKind.Desynthesis, Subject = 3, Detail = 1, Seen = 0, Imported = 4, LevelMin = 5, LevelMax = 5,
                First = At.AddDays(-3), Last = At.AddDays(-1), ImportedValues = new() { [5] = 4 } };
            a.LoadShared(new DiscoveryFile { Written = At, Facts = [shared] });
            Save(a, database);
            var row = Row(database, 3);
            Check(row.Seen == 5, "the join adds no sighting and B's three stay: " + row.Seen);
            Check(row.Imported == 4 && row.First == At.AddDays(-3), "the shared copy's part joined: " + row.Imported);
            Save(a, database); Save(b, database);
            Check(Row(database, 3).Seen == 5, "saves without a change add nothing");
        })),
        ("Review minors 1381 M4: the tool proposal form has its own prefix, which the labelling job leaves alone", () =>
        {
            var form = Read(".github/ISSUE_TEMPLATE/tool_proposal.yml").Replace("\r\n", "\n");
            Check(form.Contains("\ntitle: \"[Tool] \"\n", StringComparison.Ordinal) && form.Contains("labels: [\"tool-proposal\", \"player-report\"]", StringComparison.Ordinal), "form");
            var flow = Read(".github/workflows/player-report-labels.yml");
            Check(!flow.Contains("[Tool]", StringComparison.Ordinal), "not relabelled: no « enhancement » on a tool proposal");
        }),
        ("Review minors 1381 M5: the allied societies catalogue's failure is logged", () =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.AlliedSocieties.cs").Contains("if(!loaded.IsCompletedSuccessfully)log.Warning(", StringComparison.Ordinal), "logged");
        }),
    ];
}
