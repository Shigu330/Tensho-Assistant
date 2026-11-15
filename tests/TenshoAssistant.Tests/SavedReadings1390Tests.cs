using TenshoAssistant.Collections;

// X-417 (0.1390, review of 0.1383 to 0.1389, I1): the saved title and achievement readings reach what is published on the usual path (the disk
// copy after a restart within the cache duration) and when they arrive after a publication; the page then names the reading's date.
internal static class SavedReadings1390Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 14, 21, 0, 0, TimeSpan.Zero);
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + path)).Replace("\r\n", "\n");
    private static CollectionEntry Entry(CollectionKind kind, uint id) => new(kind, id, "x" + id, 0, false);

    private static readonly CollectionEntry[] Restored =
    [
        Entry(CollectionKind.Title, 1), Entry(CollectionKind.Title, 2), Entry(CollectionKind.Achievement, 10),
        Entry(CollectionKind.Achievement, 11), Entry(CollectionKind.Mount, 1) with { ConfirmedUnlocked = true },
    ];
    private static readonly Dictionary<CollectionKind, CompletionList> Lists = new()
    {
        [CollectionKind.Title] = new(7, CollectionKind.Title, At, [1]),
        [CollectionKind.Achievement] = new(7, CollectionKind.Achievement, At.AddMinutes(2), [11]),
    };

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Saved readings 1390: a restored copy gets the saved readings and their dates (the usual path of I1)", () =>
        {
            var (entries, titles, achievements) = CompletionListsData.ApplySaved(Restored, Lists, false, false, At.AddHours(1), TimeSpan.FromDays(7));
            Check(titles == At && achievements == At.AddMinutes(2), "the page gets both readings' dates (« Reading of <date> », not « not loaded »)");
            Check(entries[0].Acquisition == CollectionAcquisition.Acquired && entries[1].Acquisition == CollectionAcquisition.NotAcquired, "titles: obtained, and not obtained while fresh");
            Check(entries[2].Acquisition == CollectionAcquisition.NotAcquired && entries[3].Acquisition == CollectionAcquisition.Acquired, "achievements from their own reading");
            Check(entries[4] == Restored[4], "other kinds untouched");
            var (again, _, _) = CompletionListsData.ApplySaved(entries, Lists, false, false, At.AddHours(1), TimeSpan.FromDays(7));
            Check(again.SequenceEqual(entries), "applied twice (readings arriving after the copy): the same entries");
        }),
        ("Saved readings 1390: a complete reading of this session wins; old readings keep only what was obtained; none, no date", () =>
        {
            var seen = Restored.Select(entry => entry.Kind == CollectionKind.Title ? entry with { NegativeConfirmed = entry.Id == 1 } : entry).ToArray();
            var (complete, titles, achievements) = CompletionListsData.ApplySaved(seen, Lists, true, false, At.AddHours(1), TimeSpan.FromDays(7));
            Check(titles is null && achievements == At.AddMinutes(2), "titles sent this session: no saved date for them");
            Check(complete[0].Acquisition == CollectionAcquisition.NotAcquired, "the game's own answer this session is not overwritten by the reading");
            var (old, _, _) = CompletionListsData.ApplySaved(Restored, Lists, false, false, At.AddDays(10), TimeSpan.FromDays(7));
            Check(old[0].Acquisition == CollectionAcquisition.Acquired && old[1].Acquisition == CollectionAcquisition.Unknown, "past the cache duration: obtained kept, the rest unknown");
            var (none, noTitles, noAchievements) = CompletionListsData.ApplySaved(Restored, null, false, false, At, TimeSpan.FromDays(7));
            Check(none.SequenceEqual(Restored) && noTitles is null && noAchievements is null, "no reading kept: nothing changed, no date");
        }),
        ("Saved readings 1390: wiring: the disk copy and a late arrival publish the saved readings", () =>
        {
            var reader = Read("Collections/CollectionReader.Dalamud.cs");
            var completion = Read("Collections/CollectionReader.Completion.Dalamud.cs");
            var restore = reader.IndexOf("service.Publish(owner,saved.Value.Entries,", StringComparison.Ordinal);
            var back = reader.IndexOf("scanning=false;nextRefresh=0;PublishSavedReadings(owner);return;", StringComparison.Ordinal);
            Check(restore > 0 && back > restore && reader.IndexOf("return;", restore, StringComparison.Ordinal) > back - 1, "the disk copy path applies the readings before it returns");
            Check(reader.Contains("completeTitles=false;completeAchievements=false;"), "the disk copy is no complete reading of this session");
            Check(reader.Contains("if(!scanning && restore is null)PublishSavedReadings(owner);"), "readings arriving later are applied to what is published");
            Check(completion.Contains("CompletionListsData.PublishSaved(service,owner,listsOwner,completionLists,completeTitles,completeAchievements,"),
                "one step applies them and sets the dates (X-422 M1: CompletionListsData.PublishSaved, run by SavedReadingsPath1400Tests)");
        }),
    ];
}
