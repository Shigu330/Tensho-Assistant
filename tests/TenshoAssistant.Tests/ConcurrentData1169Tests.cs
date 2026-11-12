using System.Numerics;
using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.Discoveries;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Inventory;

// 0.1169 (X-182, night review N3, N4, N9, N11, N27, N28): two game clients, or two versions of the plugin the same day, on the same data.
// Behaviour tests (R28: data loss, concurrency), on real databases in a temporary folder: two clients on two companies keep each other's voyages;
// an older reading of an achievement never takes back a newer one; a file of before written again after the conversion (an older version still
// running) is merged, while a copy already set aside is not read again; a second client loading during a conversion counts every voyage once;
// two conversions of the discoveries at once leave one whole base and no temporary file.
internal static class ConcurrentData1169Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow.AddDays(-2);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1169-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string root) => new(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
    private static VoyageVessel Vessel(ulong house, int slot, DateTimeOffset registered, uint item)
        => new() { HouseId = house, Kind = VesselKind.Submarine, Slot = slot, Name = "Sub " + slot, Rank = 50, RegisteredAt = registered, ReturnAt = registered.AddHours(18),
                   Route = ["Deep-sea Site 1"], Points = [1], Loot = [new VoyageLoot(item, 1)] };
    private static VoyageRecord Record(ulong house, int slot, DateTimeOffset registered, uint item)
        => new(house, VesselKind.Submarine, slot, "Sub " + slot, 50, registered, registered.AddHours(18), [1u], ["Deep-sea Site 1"], [new VoyageLoot(item, 1)]);
    private static string Key(VoyageRecord record) => $"{record.HouseId}/{record.Slot}/{record.RegisteredAt.UtcTicks}/{record.Loot[0].ItemId}";
    private static string[] Aside(string root) => Directory.GetDirectories(root, "before-sqlite-*").SelectMany(Directory.GetFiles).ToArray();
    private static DiscoveryFact Fact(uint subject) => new() { Kind = DiscoveryKind.FishCatch, Subject = subject, Detail = 1, Territory = 135, Map = 16,
        CellX = DiscoveryStore.Cell(120, -40).X, CellZ = DiscoveryStore.Cell(120, -40).Z, X = 120, Z = -40, LevelMin = 1, LevelMax = 1, Seen = 1, First = At.AddDays(-3), Last = At.AddDays(-2) };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Concurrent data 1169: two clients on two companies, each saving its own voyages: neither erases the other's (N4)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "VoyageHistory.json");
            var first = new VoyageHistoryStore(file, (_, _) => { }, Data(root)); var second = new VoyageHistoryStore(file, (_, _) => { }, Data(root));
            Check(first.Records.Count == 0 && second.Records.Count == 0, "both loaded, empty");
            first.Observe([Vessel(100, 0, At, 21)]); Check(first.Flush(TimeSpan.FromSeconds(10)), "first saved");
            second.Observe([Vessel(200, 0, At.AddHours(1), 22)]); Check(second.Flush(TimeSpan.FromSeconds(10)), "second saved");
            first.Observe([Vessel(100, 1, At.AddHours(2), 23)]); Check(first.Flush(TimeSpan.FromSeconds(10)), "first saved again");
            var all = new VoyageHistoryData(file, Data(root)).Load(DateTimeOffset.UtcNow);
            Check(all.Select(Key).SequenceEqual([Key(Record(100, 0, At, 21)), Key(Record(200, 0, At.AddHours(1), 22)), Key(Record(100, 1, At.AddHours(2), 23))]),
                "a third load sees the three voyages: " + string.Join(", ", all.Select(Key)));
        })),
        ("Concurrent data 1169: a voyage read again by the other client is not taken back by a client's older copy; old voyages purged apart (N4)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "VoyageHistory.json");
            var first = new VoyageHistoryStore(file, (_, _) => { }, Data(root));
            first.Observe([Vessel(100, 0, At, 21)]); first.Flush(TimeSpan.FromSeconds(10));
            var second = new VoyageHistoryStore(file, (_, _) => { }, Data(root));
            second.Observe([Vessel(100, 0, At, 21) with { Loot = [new VoyageLoot(21, 1), new VoyageLoot(30, 2)] }]); second.Flush(TimeSpan.FromSeconds(10));
            first.Observe([Vessel(100, 1, At.AddHours(1), 24)]); first.Flush(TimeSpan.FromSeconds(10));
            var all = new VoyageHistoryData(file, Data(root)).Load(DateTimeOffset.UtcNow);
            Check(all.Count == 2 && all[0].Loot.Count == 2, "the other client's newer reading kept: " + all.Count);
            var data = new VoyageHistoryData(file, Data(root));
            data.Save([Record(300, 0, DateTimeOffset.UtcNow.AddDays(-200), 1)], null, DateTimeOffset.UtcNow);
            Check(new VoyageHistoryData(file, Data(root)).Load(DateTimeOffset.UtcNow).All(record => record.HouseId != 300), "older than 180 days: removed by the purge");
        })),
        ("Concurrent data 1169: an older reading of an achievement never takes back a newer one saved by the other client (N9)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "AchievementProgress.json");
            var a = new AchievementProgressData(file, Data(root)); var b = new AchievementProgressData(file, Data(root));
            a.Load(At); b.Load(At);
            b.Save(7, new Dictionary<uint, AchievementProgress> { [1] = new(7, 10, At.AddHours(2)), [2] = new(1, 5, At.AddHours(2)) }, []);
            a.Save(7, new Dictionary<uint, AchievementProgress> { [1] = new(5, 10, At.AddHours(1)), [3] = new(2, 3, At.AddHours(1)) }, []);
            var seen = new AchievementProgressData(file, Data(root)).Load(At)[7];
            Check(seen.Count == 3 && seen[1].Current == 7 && seen[2].Current == 1 && seen[3].Current == 2, "a1 7/10, a2 and a3 all kept: " + string.Join(", ", seen.Select(pair => $"{pair.Key}={pair.Value.Current}")));
            a.Save(7, new Dictionary<uint, AchievementProgress> { [1] = new(9, 10, At.AddHours(3)) }, []);
            Check(new AchievementProgressData(file, Data(root)).Load(At)[7][1].Current == 9, "a newer reading still replaces");
        })),
        ("Concurrent data 1169: a file of before written again after the conversion is merged; a copy already set aside is not read again (N3)", () => InTemp(root =>
        {
            // Voyages.
            var voyages = Path.Combine(root, "VoyageHistory.json");
            VoyageHistory.Write(voyages, [Record(100, 0, At, 21)]);
            new VoyageHistoryData(voyages, Data(root)).Load(At);
            VoyageHistory.Write(voyages, [Record(100, 1, At.AddHours(1), 22)]);   // an older version still running, its own session only
            var merged = new VoyageHistoryData(voyages, Data(root)).Load(At);
            Check(merged.Count == 2 && !File.Exists(voyages), "voyages: both kept, the file set aside: " + merged.Count);
            File.Copy(Aside(root).First(path => File.ReadAllText(path).Contains("\"Slot\":0")), voyages);
            Check(new VoyageHistoryData(voyages, Data(root)).Load(At).Count == 2, "a copy already set aside: not read again");
            // Achievements.
            var achievements = Path.Combine(root, "AchievementProgress.json");
            File.WriteAllText(achievements, AchievementProgressStore.Write(new() { [7] = new() { [1] = new(1, 10, At) } }));
            new AchievementProgressData(achievements, Data(root)).Load(At);
            File.WriteAllText(achievements, AchievementProgressStore.Write(new() { [7] = new() { [2] = new(4, 10, At.AddHours(1)) }, [8] = new() { [1] = new(2, 10, At) } }));
            var progress = new AchievementProgressData(achievements, Data(root)).Load(At);
            Check(progress[7].Count == 2 && progress[8].Count == 1, "achievements: merged");
            // Gil journal.
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var gil = Path.Combine(folder, GilJournalStore.FileName(7));
            var one = new GilEntry(At, 100, 100, GilSource.Character, GilCategory.Other); var two = new GilEntry(At.AddHours(1), 50, 150, GilSource.Character, GilCategory.Other);
            GilJournal.Write(gil, [one]);
            new GilJournalStore(folder, Data(root)).Load(7, At);
            GilJournal.Write(gil, [two]);
            var journal = new GilJournalStore(folder, Data(root)).Load(7, At);
            Check(journal.SequenceEqual([one, two]) && !File.Exists(gil), "gils: both entries, in time order, once");
            Check(GilJournalStore.Merge([one, two], [two, one]).SequenceEqual([one, two]), "merge: every entry once");
        })),
        ("Concurrent data 1169: a second client loading during the conversion counts every voyage once (N28; 0.1171: 20 times, the file moved under it)", () => InTemp(top =>
        {
          for (var round = 0; round < 20; round++)
          {
            var root = Path.Combine(top, "round" + round); Directory.CreateDirectory(root);
            var file = Path.Combine(root, "VoyageHistory.json");
            VoyageHistory.Write(file, [Record(100, 0, At, 21), Record(100, 1, At.AddHours(1), 22), Record(200, 0, At.AddHours(2), 23)]);
            Task<List<VoyageRecord>>? other = null;
            var first = new VoyageHistoryData(file, Data(root));
            first.DuringSave = () => other ??= Task.Run(() => new VoyageHistoryData(file, Data(root)).Load(At));
            Check(first.Load(At).Count == 3, "converted: three voyages");
            Check(other is not null && other.Wait(TimeSpan.FromSeconds(20)) && other.Result.Count == 3, "the second client: three voyages, never an empty history");
            using var open = new TenshoDataDatabase(Path.Combine(root, TenshoDataDatabase.FileName)).Open();
            Check(Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM voyage_records;")) == 3, "three rows");
          }
        })),
        // 0.1197: this test failed once on a file « in use »: one conversion moved Discoveries.json aside while the other read it. Whatever
        // the order the two threads take, the outcome is now the same (each race is replayed by hand in ConversionRaces1197Tests): twenty
        // times here, where once showed little.
        ("Concurrent data 1169: two conversions of the discoveries at once, 20 times: one whole base, the old files kept once, no temporary file left (N11)", () => InTemp(top =>
        {
          for (var round = 0; round < 20; round++)
          {
            var root = Path.Combine(top, "round" + round); Directory.CreateDirectory(root);
            var main = Path.Combine(root, "Discoveries.json");
            var store = new DiscoveryStore(); store.Merge([Fact(4869), Fact(4870)]);
            DiscoveryFiles.WriteSplit(main, store.Snapshot(At), []);
            var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            Parallel.For(0, 2, _ => { try { DiscoveryDatabase.ConvertFiles(root, At, (_, error) => throw error); } catch (Exception error) { errors.Add(error); } });
            Check(errors.IsEmpty, $"round {round}: no conversion failed: " + string.Join("; ", errors.Select(error => error.Message)));
            Check(new DiscoveryDatabase(Path.Combine(root, DiscoveryDatabase.FileName)).Count().Shared == 2, $"round {round}: the base holds both facts");
            Check(Directory.GetFiles(root, DiscoveryDatabase.FileName + ".converting*").Length == 0, $"round {round}: no temporary file left");
            Check(!File.Exists(main) && Aside(root).Count(path => Path.GetFileName(path) == "Discoveries.json") == 1, $"round {round}: the old file kept aside once");
            Check(!Directory.Exists(DiscoveryFiles.SharedFolder(main)) && Directory.GetDirectories(root, "before-sqlite-*").SelectMany(Directory.GetDirectories).Count(path => Path.GetFileName(path).StartsWith("Shared", StringComparison.Ordinal)) == 1, $"round {round}: the shared files kept aside once");
          }
        })),
        ("Concurrent data 1169: wiring (a file of before moved only once the data file is synced; read in an immediate transaction)", () =>
        {
            var database = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/TenshoDataDatabase.cs"));
            Check(database.Contains("PRAGMA wal_checkpoint(FULL);") && database.Contains("if(opened is not null && !opened.Durable())"), "durable before the move (N27)");
            foreach (var path in new[] { "src/TenshoAssistant/FreeCompany/VoyageHistory.cs", "src/TenshoAssistant/Collections/AchievementProgressData.cs" })
                Check(File.ReadAllText(Path.GetFullPath(path)).Contains("BeginTransaction(deferred:false)"), path + ": immediate transaction");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/FreeCompany/VoyageHistory.cs")).Contains("DELETE FROM voyage_records;"), "the whole table is never deleted");
        }),
    ];
}
