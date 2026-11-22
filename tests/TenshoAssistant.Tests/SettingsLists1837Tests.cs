using System.Text.Json.Nodes;
using TenshoAssistant;
using TenshoAssistant.Alarms;
using TenshoAssistant.Maps;
using TenshoAssistant.Reminders;

// 0.1837 (X-831, security review I3, R28.14): two game clients on one Dalamud profile share Settings.json (fair lock, three-way merge), but a
// list was taken whole from the last writer: client A adds an alarm and saves, client B ticks another alarm and saves, and A's alarm was gone at
// the next start, without a word. Now a list of objects with a unique « Id » (alarms, map bookmarks, personal tasks) is merged element by
// element. Here: two SettingsFile instances on one file, as two clients.
internal static class SettingsLists1837Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static void InTemp(Action<string> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-settings-1837-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { run(root); }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static SettingsFile<Configuration> Client(string root) =>
        new(Path.Combine(root, "TenshoAssistant", SettingsFile<Configuration>.FileName), null, (_, _) => { });

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Settings lists 1837: behaviour: two clients change different alarms, bookmarks and tasks of one profile; both changes kept (X-831)", () => InTemp(root =>
        {
            var kept = new TenshoAlarm { Label = "Kept", EtMinute = 60 };
            var gone = new TenshoAlarm { Label = "Removed by A" };
            var mark = new MapBookmark(Guid.NewGuid(), "Camp", 1, 128, 10, 20);
            var task = new PersonalTask { Name = "Daily", Count = 0 };
            Check(Client(root).Save(new Configuration { Alarms = [kept, gone], MapBookmarks = [mark], PersonalTasks = [task] }), "the profile saved once");
            var fileA = Client(root); var a = fileA.Load(out _);
            var fileB = Client(root); var b = fileB.Load(out _);
            // A adds an alarm, a bookmark and a task, and removes an alarm; B ticks the kept alarm, renames the bookmark, counts the task.
            a.Alarms.Add(new TenshoAlarm { Label = "Added by A" });
            a.Alarms.RemoveAll(alarm => alarm.Label == "Removed by A");
            a.MapBookmarks.Add(new MapBookmark(Guid.NewGuid(), "Added by A", 2, 129, 1, 2));
            a.PersonalTasks!.Add(new PersonalTask { Name = "Added by A" });
            Check(fileA.Save(a), "A saves");
            b.Alarms.Single(alarm => alarm.Id == kept.Id).EtMinute = 120;
            b.MapBookmarks[0] = b.MapBookmarks[0] with { Name = "Camp (B)" };
            b.PersonalTasks!.Single().Count = 1;
            Check(fileB.Save(b), "B saves after A");
            var next = Client(root).Load(out var origin);
            Check(origin == SettingsOrigin.Own, "read back: " + origin);
            Check(next.Alarms.Select(alarm => alarm.Label).SequenceEqual(["Kept", "Added by A"]), "A's alarm kept, the one A removed stays removed: " + string.Join(", ", next.Alarms.Select(alarm => alarm.Label)));
            Check(next.Alarms.Single(alarm => alarm.Id == kept.Id).EtMinute == 120, "B's change to the other alarm kept");
            Check(next.MapBookmarks.Select(bookmark => bookmark.Name).SequenceEqual(["Camp (B)", "Added by A"]), "bookmarks: " + string.Join(", ", next.MapBookmarks.Select(bookmark => bookmark.Name)));
            Check(next.PersonalTasks!.Select(item => (item.Name, item.Count)).SequenceEqual([("Daily", 1), ("Added by A", 0)]), "tasks: " + string.Join(", ", next.PersonalTasks!.Select(item => item.Name + " " + item.Count)));
            // A saves again without having seen B's changes: its unchanged elements take the file's, nothing of B's lost.
            a.DeveloperMode = !a.DeveloperMode;
            Check(fileA.Save(a), "A saves again");
            var last = Client(root).Load(out _);
            Check(last.Alarms.Single(alarm => alarm.Id == kept.Id).EtMinute == 120 && last.MapBookmarks[0].Name == "Camp (B)" && last.PersonalTasks!.Single(item => item.Id == task.Id).Count == 1
                && last.Alarms.Count == 2 && last.MapBookmarks.Count == 2 && last.PersonalTasks!.Count == 2, "A's next save keeps B's changes");
        })),
        ("Settings lists 1837: the identity merge, case by case; a list without unique ids is still taken whole (X-831)", () =>
        {
            JsonObject Node(string text) => SettingsJson.Node(text);
            var before = Node("""{"L":[{"Id":"a","V":1},{"Id":"b","V":1},{"Id":"c","V":1}]}""");
            var mine = Node("""{"L":[{"Id":"a","V":2},{"Id":"b","V":1},{"Id":"d","V":1}]}""");            // a changed, c removed, d added
            var theirs = Node("""{"L":[{"Id":"a","V":1,"W":5},{"Id":"b","V":3},{"Id":"c","V":1},{"Id":"e","V":1}]}"""); // a: W added, b changed, e added
            var merged = SettingsJson.Merge(before, mine, theirs).ToJsonString();
            Check(merged == """{"L":[{"Id":"a","V":2,"W":5},{"Id":"b","V":3},{"Id":"d","V":1},{"Id":"e","V":1}]}""", "merged by id: " + merged);
            var removedThere = SettingsJson.Merge(before, Node("""{"L":[{"Id":"a","V":1},{"Id":"b","V":1},{"Id":"c","V":1}]}"""), Node("""{"L":[{"Id":"a","V":1}]}""")).ToJsonString();
            Check(removedThere == """{"L":[{"Id":"a","V":1}]}""", "removed by the other client, unchanged here: stays removed: " + removedThere);
            var plain = SettingsJson.Merge(Node("""{"L":[1,2]}"""), Node("""{"L":[1,2,3]}"""), Node("""{"L":[1]}""")).ToJsonString();
            Check(plain == """{"L":[1,2,3]}""", "plain values: taken whole, as before: " + plain);
            var twice = SettingsJson.Merge(null, Node("""{"L":[{"Id":"a"},{"Id":"a"}]}"""), Node("""{"L":[{"Id":"b"}]}""")).ToJsonString();
            Check(twice == """{"L":[{"Id":"a"},{"Id":"a"}]}""", "an id that repeats: taken whole: " + twice);
        }),
    ];
}
