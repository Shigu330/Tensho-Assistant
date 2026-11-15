using Microsoft.Data.Sqlite;
using TenshoAssistant;
using TenshoAssistant.Collections;

// X-274 (0.1387, user's request of 13/11, test T-0.1140-1): the titles and achievements as the game last sent them, kept per character in
// TenshoData.db and applied after a reload while the game has not sent the list again. Behaviour (R28.14): written, then read back through
// a new data file object (a reload), two characters kept apart, an older reading never written over a newer one.
internal static class CompletionLists1387Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 13, 21, 10, 0, TimeSpan.Zero);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");

    private static void InTemp(Action<Func<CompletionListsData>> run)
    {
        var root = Path.Combine(Path.GetTempPath(), "tensho-completion-1387-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var database = Path.Combine(root, TenshoDataDatabase.FileName);
        try { run(() => new CompletionListsData(new TenshoDataFile(database, (error, message) => throw new Exception(message, error)))); }
        finally { SqliteConnection.ClearAllPools(); try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
    }

    private static CollectionEntry Entry(CollectionKind kind, uint id) => new(kind, id, "x" + id, 0, false);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Completion lists 1387: behaviour: written, read back after a reload, two characters apart", () => InTemp(data =>
        {
            var first = data();
            Check(first.Save(new(1, CollectionKind.Title, At, [5, 3, 9])), "character 1's titles written");
            Check(first.Save(new(1, CollectionKind.Achievement, At.AddMinutes(1), [100, 200])), "character 1's achievements written");
            Check(first.Save(new(2, CollectionKind.Title, At, [7])), "character 2's titles written");
            var reloaded = data();
            var one = reloaded.Load(1); var two = reloaded.Load(2);
            Check(one.Count == 2 && one[CollectionKind.Title].Ids.SequenceEqual(new uint[] { 3, 5, 9 }) && one[CollectionKind.Title].ReadAt == At, "character 1's titles and their date");
            Check(one[CollectionKind.Achievement].Ids.SequenceEqual(new uint[] { 100, 200 }) && one[CollectionKind.Achievement].ReadAt == At.AddMinutes(1), "character 1's achievements");
            Check(two.Count == 1 && two[CollectionKind.Title].Ids.SequenceEqual(new uint[] { 7 }), "character 2 keeps its own titles only");
            Check(reloaded.Load(3).Count == 0 && reloaded.Load(0).Count == 0, "nothing for another character");
        })),
        ("Completion lists 1387: behaviour: an older reading is never written over a newer one; bounds refused", () => InTemp(data =>
        {
            var store = data();
            Check(store.Save(new(1, CollectionKind.Title, At, [1, 2, 3])), "first reading");
            Check(!store.Save(new(1, CollectionKind.Title, At.AddMinutes(-5), [1])), "an older reading (another client) refused");
            Check(!store.Save(new(1, CollectionKind.Title, At, [1])), "the same date: nothing written");
            Check(store.Save(new(1, CollectionKind.Title, At.AddMinutes(5), [1, 2, 3, 4])), "a newer reading replaces it");
            Check(data().Load(1)[CollectionKind.Title].Ids.Count == 4, "the newer one kept");
            Check(!store.Save(new(0, CollectionKind.Title, At, [1])) && !store.Save(new(1, CollectionKind.Mount, At, [1]))
                && !store.Save(new(1, CollectionKind.Title, At.AddHours(1), [0])) && !store.Save(new(1, CollectionKind.Title, At.AddHours(1), [4, 4])), "no character, another kind, id 0, a double: refused");
            Check(TenshoDataDatabase.CharacterTables.Contains("completion_lists"), "erased with the character");
        })),
        ("Completion lists 1387: a reading applied to fresh entries: obtained kept, not obtained only while fresh enough", () =>
        {
            var list = new CompletionList(1, CollectionKind.Achievement, At, [10, 11]);
            Check(CompletionListsData.Apply(Entry(CollectionKind.Achievement, 10), list, false).Acquisition == CollectionAcquisition.Acquired, "obtained stays obtained, whatever its age");
            Check(CompletionListsData.Apply(Entry(CollectionKind.Achievement, 12), list, true).Acquisition == CollectionAcquisition.NotAcquired, "not in the reading: not obtained while fresh");
            Check(CompletionListsData.Apply(Entry(CollectionKind.Achievement, 12), list, false).Acquisition == CollectionAcquisition.Unknown, "older than the cache duration: unknown");
            var seen = Entry(CollectionKind.Achievement, 12) with { ConfirmedUnlocked = true };
            Check(CompletionListsData.Apply(seen, list, true) == seen, "a fresh positive wins");
            Check(CompletionListsData.Apply(Entry(CollectionKind.Title, 10), list, true).Acquisition == CollectionAcquisition.Unknown, "another kind untouched");
        }),
        ("Completion lists 1387: wiring: the reader loads, applies and keeps the readings; the page says the reading's date", () =>
        {
            var reader = Read("Collections/CollectionReader.Dalamud.cs");
            var completion = Read("Collections/CollectionReader.Completion.Dalamud.cs");
            var page = Read("MainWindow.Collections.cs");
            var plugin = Read("Plugin.cs");
            Check(reader.Contains("listsLoad=owner==0 ? null:Task.Run(()=>lists.Load(who));") && reader.Contains("if(character!=0)KeepCompleteReadings(character,"), "the reader loads in the background and keeps complete readings");
            Check(completion.Contains("CompletionListsData.Apply(entry,savedTitles,") && completion.Contains("CompletionListsData.Apply(entry,savedAchievements,")
                && completion.Contains("try{lists.Save(list);}"), "applied while the list is not sent, saved in the background");
            Check(page.Contains("localization.Format(\"CollectionsBrowser.TitlesFromReading\"") && page.Contains("localization.Format(\"CollectionsBrowser.AchievementsFromReading\""), "the red line names the reading");
            Check(plugin.Contains("new CompletionListsData(tenshoData)"), "the plugin gives the reader the data file");
        }),
    ];
}
