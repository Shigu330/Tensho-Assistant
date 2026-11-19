using TenshoAssistant;
using TenshoAssistant.Beastmaster;

// 0.1555 (X-617, review of block 0.1551 to 0.1554, I1): the plans' file held by another program, then ranks, a board's result and a bestiary
// page read, then the file let go: all read meanwhile is joined to the file and written, nothing lost (behaviour test, R28.14).
internal static class BeastHeld1555Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow.AddHours(-1);
    private static BeastTeams.BookRead Page(uint beast, int rank) => new(beast, rank, rank, new BeastStats(1, 1, 1, 1, 1), 100, 10);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1555-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast held 1555: X-617, ranks and a result read while the plans' file is held are joined to it once let go, then written", () => InTemp(root =>
        {
            var file = Path.Combine(root, "BeastTeams.json");
            var old = new BoardResult(At.AddDays(-2), 100, [new BeastGain(3, 11, 0, 12, 5)]);
            File.WriteAllText(file, BeastTeams.Write(new BeastTeams.SavedPlans([], new() { [1] = "key" }, new() { [3] = 12, [5] = 4 }, old, null, new() { [5] = Page(5, 4) })));
            var reader = new BeastPlansFile();
            BeastTeams.SavedPlans memory;
            bool save;
            // The window's first look, the file held: empty plans in memory, nothing to write.
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) (memory, save) = reader.Load(file, null, At);
            Check(reader.Blocked && !save && memory.Ranks is null, "held: empty plans, nothing written");
            // Meanwhile (as MainWindow.BeastResultRead, BeastRanksRead and BeastBookRead do): a board's result, its ranks, a bestiary page.
            var late = new BoardResult(At, 900, [new BeastGain(7, 1, 0, 2, 40)]);
            memory = memory with { Result = late, Ranks = BeastResults.Merge(memory.Ranks, late), Book = new() { [5] = Page(5, 6) } };
            memory = BeastTeams.WithBook(memory, Page(5, 6)) ?? memory;
            // Still held at the next look: the memory kept, nothing written.
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) (memory, save) = reader.Load(file, memory, At + BeastPlansFile.RetryEvery);
            Check(reader.Blocked && !save && memory.Result == late, "still held: the memory kept");
            // Let go: the file read and joined with what was read meanwhile.
            (memory, save) = reader.Load(file, memory, At + BeastPlansFile.RetryEvery * 2);
            Check(!reader.Blocked && reader.MayWrite && save, "let go: to be written");
            Check(memory.Keys[1] == "key" && memory.Ranks![3] == 12 && memory.Ranks[7] == 2 && memory.Ranks[5] == 6, "the file's ranks and those read meanwhile: " + string.Join(",", memory.Ranks!));
            Check(memory.Result == late && memory.Book![5].Rank == 6, "the most recent result and the higher bestiary page");
            // Written (as MainWindow.SaveBeastPlans), then read again by a new start: nothing lost.
            var writer = new BackgroundWriter((error, message) => throw new Exception(message, error));
            BeastTeams.SaveLater(writer, file, memory);
            Check(writer.Flush(TimeSpan.FromSeconds(5)), "written");
            var again = new BeastPlansFile().Load(file, null, At).Plans;
            Check(again.Ranks![7] == 2 && again.Ranks[3] == 12 && again.Result!.Score == 900 && again.Keys[1] == "key", "read again after a restart: all kept");
        })),
        ("Beast held 1555: X-617, read once without a hold: the file as it is, nothing to write; a file gone meanwhile: the memory written", () => InTemp(root =>
        {
            var file = Path.Combine(root, "BeastTeams.json");
            File.WriteAllText(file, BeastTeams.Write(new BeastTeams.SavedPlans([], new() { [1] = "key" }, new() { [3] = 12 })));
            var (plans, save) = new BeastPlansFile().Load(file, null, At);
            Check(!save && plans.Ranks![3] == 12, "a plain read: nothing written");
            var reader = new BeastPlansFile();
            BeastTeams.SavedPlans memory;
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) (memory, save) = reader.Load(file, null, At);
            memory = memory with { Ranks = new() { [4] = 2 } };
            File.Delete(file);
            (memory, save) = reader.Load(file, memory, At + BeastPlansFile.RetryEvery);
            Check(save && reader.MayWrite && memory.Ranks![4] == 2, "the file gone: what was read meanwhile is written");
            // Held, nothing read meanwhile, let go: the joined plans are the file's, no write.
            File.WriteAllText(file, BeastTeams.Write(new BeastTeams.SavedPlans([], new() { [1] = "key" }, new() { [3] = 12 })));
            var quiet = new BeastPlansFile();
            using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None)) (memory, save) = quiet.Load(file, null, At);
            (memory, save) = quiet.Load(file, memory, At + BeastPlansFile.RetryEvery);
            Check(!save && memory.Ranks![3] == 12, "nothing read meanwhile: nothing to write");
        })),
        ("Beast held 1555: X-617, the join never lowers a rank, keeps exact over a floor, the most recent result, the plans made meanwhile", () =>
        {
            var file = new BeastTeams.SavedPlans(new() { [1] = new BoardPlan(1, [], [], false) }, new() { [1] = "a", [2] = "b" }, new() { [1] = 8, [2] = 5, [3] = 4 },
                new BoardResult(At, 10, []), [2, 3]);
            var meanwhile = new BeastTeams.SavedPlans(new() { [2] = new BoardPlan(2, [], [], false) }, new() { [2] = "c" }, new() { [1] = 6, [2] = 5, [3] = 7 },
                new BoardResult(At.AddDays(-1), 20, []), [3]);
            var joined = BeastTeams.Join(file, meanwhile);
            Check(joined.Ranks![1] == 8 && joined.Ranks[2] == 5 && joined.Ranks[3] == 7, "the higher rank each time");
            Check(!joined.Floors!.Contains(2) && joined.Floors.Contains(3), "equal: exact over a floor; higher floor: a floor");
            Check(joined.Result!.Score == 10, "the older result read meanwhile does not replace a newer one");
            Check(joined.Keys[1] == "a" && joined.Keys[2] == "c" && joined.Plans.ContainsKey(2), "the plans made meanwhile over the file's");
            var wiring = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Beastmaster.cs"));
            Check(wiring.Contains("var (plans, save) = beastFile.Load(file, beastPlans, DateTimeOffset.UtcNow,", StringComparison.Ordinal)
                && wiring.Contains("if (save) SaveBeastPlans();", StringComparison.Ordinal), "the window joins and writes");
        }),
    ];
}
