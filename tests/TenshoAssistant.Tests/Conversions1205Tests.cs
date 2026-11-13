using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Inventory;

// 0.1205 (relay 37, point 3 of the master's order): a family's file of before held by another program when the plugin loads (read as
// nothing, nothing marked) left the whole session without a save of that family: every save refused « not converted yet ». The first save
// that finds the file free now converts it first; while it is still held, the save is refused as before and nothing is written. The gil
// journal's save replaces the character's rows: what the file held (never loaded this session) is merged in, never dropped. Behaviour
// tests (R28): real files, a real data file, the file held by a real handle without sharing (as another program would).
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

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Conversions 1205: behaviour: achievements: file of before held at the load: the save is refused while it is held, then converts it and keeps both", () => InTemp(root =>
        {
            var file = Path.Combine(root, "AchievementProgress.json");
            var before = new Dictionary<ulong, Dictionary<uint, AchievementProgress>> { [Character] = new() { [3811] = new(1, 5, At.AddDays(-2)), [42] = new(2, 10, At.AddDays(-2)) } };
            File.WriteAllText(file, AchievementProgressStore.Write(before));
            var store = new AchievementProgressData(file, Data(root));
            var seen = new Dictionary<uint, AchievementProgress> { [42] = new(9, 10, At) };
            using (Hold(file))
            {
                Check(store.Load(At).Count == 0, "held at the load: nothing read, no error");
                Check(Refused(() => store.Save(Character, seen, [])), "held at the save: refused");
            }
            using (var open = new TenshoDataDatabase(Path.Combine(root, TenshoDataDatabase.FileName)).Open())
                Check(!TenshoDataDatabase.Converted(open, AchievementProgressData.Family) && Convert.ToInt64(TenshoDataDatabase.Scalar(open, "SELECT COUNT(*) FROM achievement_progress;")) == 0, "nothing written while held");
            store.Save(Character, seen, []);
            var all = new AchievementProgressData(file, Data(root)).Load(At)[Character];
            Check(all.Count == 2 && all[3811] == new AchievementProgress(1, 5, At.AddDays(-2)) && all[42] == new AchievementProgress(9, 10, At), "the file's progress kept, the newer reading saved over the older");
            Check(!File.Exists(file) && Aside(root) == 1, "converted by the save, then set aside");
        })),
        ("Conversions 1205: behaviour: voyages: file of before held at the load: refused while held, then converted by the save, every voyage kept", () => InTemp(root =>
        {
            var file = Path.Combine(root, "VoyageHistory.json");
            VoyageRecord Voyage(int slot, DateTimeOffset at) => new(17, VesselKind.Submarine, slot, "Vessel " + slot, 50, at, at.AddHours(18), [1u, 5u], ["Deep-sea Site 1"], [new VoyageLoot(22500, slot)]);
            VoyageHistory.Write(file, [Voyage(0, At.AddDays(-3)), Voyage(2, At.AddDays(-2))]);
            var store = new VoyageHistoryData(file, Data(root));
            var mine = Voyage(4, At);
            using (Hold(file))
            {
                Check(store.Load(At).Count == 0, "held at the load: nothing read");
                Check(Refused(() => store.Save([mine], [mine], At)), "held at the save: refused");
            }
            store.Save([mine], [mine], At);
            var history = new VoyageHistoryData(file, Data(root)).Load(At);
            Check(history.Select(voyage => voyage.Slot).OrderBy(slot => slot).SequenceEqual([0, 2, 4]), "the file's voyages and the new one: " + string.Join(",", history.Select(voyage => voyage.Slot)));
            Check(!File.Exists(file) && Aside(root) == 1, "converted by the save, then set aside");
        })),
        ("Conversions 1205: behaviour: gil journal: file of before held at the load: refused while held; the save that converts it merges, nothing dropped", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var json = Path.Combine(folder, GilJournalStore.FileName(Character));
            List<GilEntry> before = [new(At.AddHours(-3), 100, 1100, GilSource.Character, GilCategory.Other), new(At.AddHours(-2), -50, 1050, GilSource.Character, GilCategory.Other)];
            GilJournal.Write(json, before);
            var store = new GilJournalStore(folder, Data(root));
            var session = new List<GilEntry>();
            using (Hold(json))
            {
                session.AddRange(store.Load(Character, At));
                Check(session.Count == 0, "held at the load: nothing read");
                session.Add(new(At, 10, 1060, GilSource.Character, GilCategory.Other));
                Check(Refused(() => store.Save(Character, session)), "held at the save: refused");
            }
            store.Save(Character, session);
            var journal = new GilJournalStore(folder, Data(root)).Load(Character, At);
            Check(journal.SequenceEqual(before.Append(session[0])), "the file's entries kept, merged with the session's in time order: " + journal.Count);
            Check(!File.Exists(json) && Aside(folder) == 1, "converted by the save, then set aside");
        })),
        ("Conversions 1205: behaviour: an unreadable gil journal or voyage history of before still refuses the save (never written over)", () => InTemp(root =>
        {
            var folder = Path.Combine(root, "GilJournal"); Directory.CreateDirectory(folder);
            var json = Path.Combine(folder, GilJournalStore.FileName(Character)); File.WriteAllText(json, "{ not a journal");
            Check(Refused(() => new GilJournalStore(folder, Data(root)).Save(Character, [new(At, 1, 1, GilSource.Character, GilCategory.Other)])) && File.ReadAllText(json) == "{ not a journal", "gil journal");
            var file = Path.Combine(root, "VoyageHistory.json"); File.WriteAllText(file, "{ not a history");
            Check(Refused(() => new VoyageHistoryData(file, Data(root)).Save([], [], At)) && File.ReadAllText(file) == "{ not a history", "voyage history");
        })),
    ];
}
