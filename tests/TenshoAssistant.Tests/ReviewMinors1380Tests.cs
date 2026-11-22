using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Alarms;
using TenshoAssistant.FreeCompany;
using TenshoAssistant.Gear;
using TenshoAssistant.Npcs;

// X-336 (0.1380, review of 0.1279 to 0.1317, minors): M1 an alarm validated unchanged is not moved; M4 a file of before written again but
// held is not set aside unmerged (behaviour test, R28.14: the file really held by another handle); M5 the map's quests read at most once a
// second; M7 one line per row in the gear clipper; M8 a shorter ClassJobCategory sheet; M9 no enum name shown; M10 a quest place linked to
// no quest stays shown; M12 the bank checked before the commit. X-574 (0.1793, the user's choice of 17/11): M4's files of before are no longer
// read nor merged at all; its cases say that now (a file held by another program never in the way).
internal static class ReviewMinors1380Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path));

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-1380-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string root) => new(Path.Combine(root, TenshoDataDatabase.FileName), (_, _) => { });
    private static VoyageRecord Record(ulong house, int slot, DateTimeOffset registered, uint item)
        => new(house, VesselKind.Submarine, slot, "Sub " + slot, 50, registered, registered.AddHours(18), [1u], ["Deep-sea Site 1"], [new VoyageLoot(item, 1)]);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Review minors 1380 M1: a daily alarm validated unchanged after it rang keeps what it rang", () =>
        {
            var now = new DateTimeOffset(2026, 11, 15, 8, 58, 0, TimeSpan.Zero);
            var alarm = new TenshoAlarm { Repeat = AlarmRepeat.RealDaily, At = new DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero), LeadMinutes = 5 };
            var shown = AlarmSchedule.EditShown(alarm, now);
            alarm.LastOccurrence = shown;
            Check(!AlarmSchedule.Move(alarm, shown, now) && alarm.LastOccurrence == shown, "unchanged: not moved, still rung");
            Check(!AlarmSchedule.Move(alarm, shown.AddSeconds(20), now), "same minute: unchanged");
            Check(AlarmSchedule.Move(alarm, shown.AddHours(1), now) && alarm.LastOccurrence == default, "a new time is a move");
        }),
        ("Review minors 1380 M4: a voyage file written again and held by another program is never read nor set aside; free, still not read (X-574)", () => InTemp(root =>
        {
            var file = Path.Combine(root, "VoyageHistory.json");
            var at = DateTimeOffset.UtcNow.AddDays(-2);
            new VoyageHistoryData(file, Data(root)).Save([Record(100, 0, at, 21)]);
            VoyageHistory.Write(file, [Record(100, 1, at.AddHours(1), 24)]);
            var bytes = File.ReadAllBytes(file);
            using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Check(new VoyageHistoryData(file, Data(root)).Load(DateTimeOffset.UtcNow).Select(record => record.Loot[0].ItemId).SequenceEqual([21u]), "held: the load goes on, the data file's voyage only");
            Check(File.ReadAllBytes(file).SequenceEqual(bytes) && Directory.GetDirectories(root, "before-sqlite-*").Length == 0, "left in place, not set aside");
            var all = new VoyageHistoryData(file, Data(root)).Load(DateTimeOffset.UtcNow);
            Check(all.Count == 1 && all[0].Loot[0].ItemId == 21, "free: still not read nor merged: " + all.Count);
            Check(File.ReadAllBytes(file).SequenceEqual(bytes), "never moved");
        })),
        ("Review minors 1380 M4: the achievements and the gil journal follow the same rule (X-574: their files of before never read, only said)", () =>
        {
            foreach (var path in new[] { "src/TenshoAssistant/Collections/AchievementProgressData.cs", "src/TenshoAssistant/Inventory/GilJournalStore.cs", "src/TenshoAssistant/FreeCompany/VoyageHistory.cs" })
            {
                var code = Read(path);
                Check(code.Contains("data.LeftAside(\"", StringComparison.Ordinal) && !code.Contains("WrittenAgain", StringComparison.Ordinal)
                    && !code.Contains("StillThere", StringComparison.Ordinal) && !code.Contains(".SetAside(", StringComparison.Ordinal), path);
            }
        }),
        ("Review minors 1380 M8: a ClassJobCategory sheet shorter than a job's column gives no job, never an exception", () =>
        {
            uint[] jobs = [1, 19, 43];
            object? Column(int index) => index < 30 ? true : throw new ArgumentOutOfRangeException(nameof(index));
            Check(GearJobMask.Of(jobs, Column, 30) == ((1UL << 1) | (1UL << 19)), "BST beyond the sheet: left out");
        }),
        ("Review minors 1380 M10: a quest place linked to no quest stays shown", () =>
        {
            Check(NpcShopLinks.QuestLinked(0) == NpcShopLinks.Always && NpcShopLinks.QuestLinked(2) == NpcShopLinks.DuringQuest, "QuestLinked");
            Check(Read("src/TenshoAssistant/Npcs/NpcVendorLoader.Dalamud.cs").Contains("if(NpcShopLinks.QuestLinked(quests.Length)==NpcShopLinks.DuringQuest)", StringComparison.Ordinal), "wired");
        }),
        ("Review minors 1380 M5, M7, M9, M12: wiring", () =>
        {
            var map = Read("src/TenshoAssistant/MainWindow.MapView.cs");
            Check(map.Contains("if(now-mapVendorQuestsCheckedAt<1000)return false;", StringComparison.Ordinal) && map.Contains("|| MapVendorQuestsChanged())", StringComparison.Ordinal), "M5");
            var gear = Read("src/TenshoAssistant/MainWindow.Gear.cs");
            var clipped = gear[gear.IndexOf("clipper.Begin(rows.Length);", StringComparison.Ordinal)..gear.IndexOf("finally{clipper.End();clipper.Destroy();}", StringComparison.Ordinal)];
            Check(!clipped.Contains("SameLineOrNext", StringComparison.Ordinal), "M7: no wrapping inside the clipper");
            Check(!Read("src/TenshoAssistant/MainWindow.AlliedSocieties.cs").Contains("kind.ToString()", StringComparison.Ordinal), "M9");
            var bank = Read("src/TenshoAssistant/Inventory/PerCharacterBankStore.cs");
            Check(bank.IndexOf("InventoryBankService.ValidateEntries(everyone);", StringComparison.Ordinal) is > 0 and var check
                && check < bank.IndexOf("transaction.Commit();", check, StringComparison.Ordinal), "M12: before the commit");
        }),
    ];
}
