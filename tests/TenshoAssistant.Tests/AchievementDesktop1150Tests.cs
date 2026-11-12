using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;

// 0.1150 (X-119 with X-173): the achievement progress in the desktop application, read from its copy of the player's data file (TenshoData.db):
// the character whose progress was seen last (as the gil journal, D-142). Behaviour tests on real files: two characters, the copy made by SQLite's
// backup, read like the application does; a plugin that has not converted its file of before yet: the copy shows nothing and the application never
// converts; the plugin's own file of before left untouched.
internal static class AchievementDesktop1150Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026,11,10,8,0,0,TimeSpan.Zero);

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(),"tensho-ach-1150-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root,true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }
    private static TenshoDataFile Data(string database) => new(database,(_,_) => { });
    private static Dictionary<uint,AchievementProgress> Seen(params (uint Id,uint Current,uint Max,int Hours)[] rows)
        => rows.ToDictionary(row => row.Id,row => new AchievementProgress(row.Current,row.Max,At.AddHours(row.Hours)));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Achievements desktop 1150: the character seen last is chosen (ties: the smaller id; none seen: none)",() =>
        {
            var all = new Dictionary<ulong,Dictionary<uint,AchievementProgress>>
            {
                [0x8000_0000_0000_0001UL] = Seen((1,3,10,5)),
                [7] = Seen((1,1,10,1),(2,4,5,9)),
                [9] = [],
            };
            Check(AchievementProgressData.Latest(all) == 7,"newest reading: character 7");
            all[3] = Seen((5,1,2,9));
            Check(AchievementProgressData.Latest(all) == 3,"a tie: the smaller id");
            Check(AchievementProgressData.Latest(new Dictionary<ulong,Dictionary<uint,AchievementProgress>> { [4] = [] }) is null,"nothing seen: none");
        }),
        ("Achievements desktop 1150: the application's copy shows the progress the plugin saved, for the character seen last",() => InTemp(root =>
        {
            var database = Path.Combine(root,TenshoDataDatabase.FileName);
            var plugin = new AchievementProgressData(Path.Combine(root,"AchievementProgress.json"),Data(database));
            plugin.Load(At);
            var first = Seen((100,2,10,1));var second = Seen((100,9,10,3),(200,1,1,2));
            plugin.Save(11,first,new() { [11] = first });
            plugin.Save(22,second,new() { [11] = first,[22] = second });
            var mirror = Path.Combine(root,"mirror");var copy = Path.Combine(mirror,TenshoDataDatabase.FileName);
            Check(TenshoDataDatabase.CopyForDesktop(database,copy).Copied == 1,"copied");
            var app = new AchievementProgressData(Path.Combine(mirror,"AchievementProgress.json"),Data(copy));
            var all = app.Load(DateTimeOffset.UtcNow);
            Check(AchievementProgressData.Latest(all) == 22 && all[22].Count == 2 && all[22][100] == second[100] && all[22][200] == second[200],"character 22's progress as saved");
            Check(!Directory.GetDirectories(mirror,"before-sqlite-*").Any() && !File.Exists(Path.Combine(mirror,"AchievementProgress.json")),"nothing converted or made in the copy's folder");
        })),
        ("Achievements desktop 1150: a plugin that has not converted yet: the copy shows nothing, the application never converts, the file of before untouched",() => InTemp(root =>
        {
            var database = Path.Combine(root,TenshoDataDatabase.FileName);
            var json = Path.Combine(root,"AchievementProgress.json");
            AtomicFile.Write(json,AchievementProgressStore.Write(new() { [5] = Seen((1,1,3,1)) }));
            var bytes = File.ReadAllBytes(json);
            _ = new TenshoDataDatabase(database);
            SqliteConnection.ClearAllPools();
            var mirror = Path.Combine(root,"mirror");var copy = Path.Combine(mirror,TenshoDataDatabase.FileName);
            Check(TenshoDataDatabase.CopyForDesktop(database,copy).Copied == 1,"copied");
            var all = new AchievementProgressData(Path.Combine(mirror,"AchievementProgress.json"),Data(copy)).Load(DateTimeOffset.UtcNow);
            Check(AchievementProgressData.Latest(all) is null,"nothing to show yet");
            Check(File.ReadAllBytes(json).SequenceEqual(bytes) && !Directory.GetDirectories(root,"before-sqlite-*").Any(),"the plugin's file of before untouched");
        })),
        ("Achievements desktop 1150: the page reads the copy when there is no game, the « Ask » button only in the game; read off the frame, the newest reading kept",() =>
        {
            var page = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.AchievementProgress.cs"));
            Check(page.Contains("var seen=(AchievementProgressSource?.Progress ?? Volatile.Read(ref desktopAchievements))?.GetValueOrDefault(id);",StringComparison.Ordinal)
                && page.Contains("if(AchievementProgressSource is null)return;",StringComparison.Ordinal)
                && page.IndexOf("if(AchievementProgressSource is null)return;",StringComparison.Ordinal) < page.IndexOf("AchievementProgress.Ask",StringComparison.Ordinal),"page");
            var window = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DesktopData.cs")).Replace("\r\n","\n");
            Check(window.Contains("UseDesktopBank(folder,warn);\n                UseDesktopAchievements(folder,warn);",StringComparison.Ordinal)
                && window.Contains("_=Task.Run(()=>\n        {\n            IReadOnlyDictionary<uint,Collections.AchievementProgress>? seen=null;",StringComparison.Ordinal)
                && window.Contains("if(Volatile.Read(ref desktopAchievementsGeneration)==generation)Volatile.Write(ref desktopAchievements,seen);",StringComparison.Ordinal)
                && window.Contains("if(Collections.AchievementProgressData.Latest(all) is { } character)seen=all[character];",StringComparison.Ordinal),"desktop data");
        }),
    ];
}
