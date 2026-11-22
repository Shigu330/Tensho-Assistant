using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Inventory;

// 0.1205 (relay 37, point 3 of the master's order): a family's file of before held by another program when the plugin loads (read as
// nothing, nothing marked) left the whole session without a save of that family: every save refused « not converted yet ». The first save
// that finds the file free now converts it first; while it is still held, the save is refused as before and nothing is written. The gil
// journal's save replaces the character's rows: what the file held (never loaded this session) is merged in, never dropped. Behaviour
// tests (R28): real files, a real data file, the file held by a real handle without sharing (as another program would). X-574 (0.1793, the
// user's choice of 17/11): nothing is converted any more: a file of before, held or not, readable or not, never refuses a save nor is read;
// the cases say that now.
internal static class Conversions1205Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 13, 3, 0, 0, TimeSpan.Zero);
    private const ulong Character = 0x8000_0000_0000_0042UL;

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1205-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string root) => new(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
    private static FileStream Hold(string file) => new(file, FileMode.Open, FileAccess.Read, FileShare.None);
    private static int Aside(string folder) => Directory.GetDirectories(folder, "before-sqlite-*").Sum(directory => Directory.GetFiles(directory).Length);
    private static bool Refused(Action save)
    {
        try { save(); return false; }
        catch (InvalidOperationException) { return true; }
    }
    private static long ConvertedMarks(string root)
    {
        using var open = new TenshoDataDatabase(Path.Combine(root, TenshoDataDatabase.FileName)).Open();
        return Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM converted;")) + Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM gil_converted;"));
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Conversions 1205: behaviour: achievements: file of before held at the load: the load and the save go on, the file never read nor moved (X-574)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "AchievementProgress.json");
            var before = new Dictionary<ulong, Dictionary<uint, AchievementProgress>> { [Character] = new() { [3811] = new(1, 5, At.AddDays(-2)), [42] = new(2, 10, At.AddDays(-2)) } };
            File.WriteAllText(file, AchievementProgressStore.Write(before)); var bytes = File.ReadAllBytes(file);
            var store = new AchievementProgressData(file, Data(root));
            var seen = new Dictionary<uint, AchievementProgress> { [42] = new(9, 10, At) };
            using (Hold(file))
            {
                Check(store.Load(At).Count == 0, "held at the load: nothing read, no error");
                Check(!Refused(() => store.Save(Character, seen, [])), "held at the save: saved all the same");
            }
            store.Save(Character, seen, []);
            var all = new AchievementProgressData(file, Data(root)).Load(At)[Character];
            Check(all.Count == 1 && all[42] == new AchievementProgress(9, 10, At), "the session's progress only: " + all.Count);
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root) == 0 && ConvertedMarks(root) == 0, "the file left as it is; nothing set aside nor marked");
        })),
        ("Conversions 1205: behaviour: voyages: file of before held at the load: the load and the save go on, the file never read nor moved (X-574)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "VoyageHistory.json");
            VoyageRecord Voyage(int slot, DateTimeOffset at) => new(17, VesselKind.Submarine, slot, "Vessel " + slot, 50, at, at.AddHours(18), [1u, 5u], ["Deep-sea Site 1"], [new VoyageLoot(22500, slot)]);
            VoyageHistory.Write(file, [Voyage(0, At.AddDays(-3)), Voyage(2, At.AddDays(-2))]); var bytes = File.ReadAllBytes(file);
            var store = new VoyageHistoryData(file, Data(root));
            var mine = Voyage(4, At);
            using (Hold(file))
            {
                Check(store.Load(At).Count == 0, "held at the load: nothing read");
                Check(!Refused(() => store.Save([mine], [mine], At)), "held at the save: saved all the same");
            }
            var history = new VoyageHistoryData(file, Data(root)).Load(At);
            Check(history.Select(voyage => voyage.Slot).SequenceEqual([4]), "the session's voyage only: " + string.Join(",", history.Select(voyage => voyage.Slot)));
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Aside(root) == 0 && ConvertedMarks(root) == 0, "the file left as it is; nothing set aside nor marked");
        })),
        ("Conversions 1205: behaviour: gil journal: file of before held at the load: the load and the save go on, the file never read nor moved (X-574)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var json = Path.Combine(folder, GilJournalStore.FileName(Character));
            List<GilEntry> before = [new(At.AddHours(-3), 100, 1100, GilSource.Character, GilCategory.Other), new(At.AddHours(-2), -50, 1050, GilSource.Character, GilCategory.Other)];
            GilJournal.Write(json, before); var bytes = File.ReadAllBytes(json);
            var store = new GilJournalStore(folder, Data(root));
            var session = new List<GilEntry>();
            using (Hold(json))
            {
                session.AddRange(store.Load(Character, At));
                Check(session.Count == 0, "held at the load: nothing read");
                session.Add(new(At, 10, 1060, GilSource.Character, GilCategory.Other));
                Check(!Refused(() => store.Save(Character, session)), "held at the save: saved all the same");
            }
            var journal = new GilJournalStore(folder, Data(root)).Load(Character, At);
            Check(journal.SequenceEqual(session), "the session's entries only: " + journal.Count);
            Check(File.ReadAllBytes(json).SequenceEqual(bytes) && Aside(folder) == 0 && ConvertedMarks(root) == 0, "the file left as it is; nothing set aside nor marked");
        })),
        ("Conversions 1205: behaviour: an unreadable gil journal or voyage history of before no longer refuses the save, and is never written over (X-574)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var json = Path.Combine(folder, GilJournalStore.FileName(Character)); File.WriteAllText(json, "{ not a journal");
            Check(!Refused(() => new GilJournalStore(folder, Data(root)).Save(Character, [new(At, 1, 1, GilSource.Character, GilCategory.Other)])) && File.ReadAllText(json) == "{ not a journal", "gil journal");
            var file = Path.Combine(root, "VoyageHistory.json"); File.WriteAllText(file, "{ not a history");
            Check(!Refused(() => new VoyageHistoryData(file, Data(root)).Save([], [], At)) && File.ReadAllText(file) == "{ not a history", "voyage history");
            Check(new GilJournalStore(folder, Data(root)).Load(Character, At).Count == 1, "the journal saved in the data file");
        })),
    ];
}
