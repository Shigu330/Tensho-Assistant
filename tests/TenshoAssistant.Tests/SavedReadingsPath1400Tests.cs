using TenshoAssistant.Collections;

// X-422 M1 (0.1400, review of 0.1390 to 0.1397): X-417's disk copy path as a behaviour, on a real CollectionService in the reader's order:
// the copy restored from the disk is published (its date, « from cache »), then the saved readings are applied to it (CompletionListsData.
// PublishSaved, the step CollectionReader runs): the page gets the readings' dates and states, the copy keeps its date; readings arriving
// after a publication are applied the same way; nothing for another character, while loading or after an error.
internal static class SavedReadingsPath1400Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static readonly DateTimeOffset At = new(2026, 11, 14, 21, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Keep = TimeSpan.FromDays(7);
    private static CollectionEntry Entry(CollectionKind kind, uint id) => new(kind, id, "x" + id, 0, false);

    private static readonly CollectionEntry[] Copy =
    [
        Entry(CollectionKind.Title, 1), Entry(CollectionKind.Title, 2), Entry(CollectionKind.Achievement, 10),
        Entry(CollectionKind.Achievement, 11), Entry(CollectionKind.Mount, 1) with { ConfirmedUnlocked = true },
    ];
    private static Dictionary<CollectionKind, CompletionList> Lists(ulong owner) => new()
    {
        [CollectionKind.Title] = new(owner, CollectionKind.Title, At, [1]),
        [CollectionKind.Achievement] = new(owner, CollectionKind.Achievement, At.AddMinutes(2), [11]),
    };
    private static CollectionAcquisition Of(CollectionService service, CollectionKind kind, uint id) =>
        service.State.Entries.Single(entry => entry.Kind == kind && entry.Id == id).Acquisition;

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Saved readings path 1400: behaviour: the disk copy published, then the readings applied to it", () =>
        {
            var service = new CollectionService();
            var recorded = At.AddMinutes(30);
            service.Publish(7, Copy, [], recorded, true);
            Check(service.TitlesReadAt is null && Of(service, CollectionKind.Title, 2) == CollectionAcquisition.Unknown, "before: the copy alone, no date");
            Check(CompletionListsData.PublishSaved(service, 7, 7, Lists(7), false, false, At.AddHours(1), Keep), "applied to the disk copy");
            Check(service.TitlesReadAt == At && service.AchievementsReadAt == At.AddMinutes(2), "the page names both readings' dates");
            Check(Of(service, CollectionKind.Title, 1) == CollectionAcquisition.Acquired && Of(service, CollectionKind.Title, 2) == CollectionAcquisition.NotAcquired
                && Of(service, CollectionKind.Achievement, 11) == CollectionAcquisition.Acquired && Of(service, CollectionKind.Mount, 1) == CollectionAcquisition.Acquired,
                "the states of the readings on the copy, the other kinds kept");
            Check(service.State.FromCache && service.State.ObservedAt == recorded && service.State.CharacterId == 7 && !service.State.Loading,
                "still the disk copy: its date and « from cache » kept");
            var once = service.State.Entries.ToArray();
            Check(CompletionListsData.PublishSaved(service, 7, 7, Lists(7), false, false, At.AddHours(1), Keep) && service.State.Entries.SequenceEqual(once),
                "applied again (the reader runs it at each arrival): the same entries");
        }),
        ("Saved readings path 1400: behaviour: readings that arrive after the publication, and a complete reading of this session", () =>
        {
            var service = new CollectionService();
            service.Publish(7, Copy, [], At, true);
            Check(CompletionListsData.PublishSaved(service, 7, 7, new Dictionary<CollectionKind, CompletionList>(), false, false, At, Keep)
                && service.TitlesReadAt is null && Of(service, CollectionKind.Title, 1) == CollectionAcquisition.Unknown, "no reading loaded yet: no date, nothing changed");
            Check(CompletionListsData.PublishSaved(service, 7, 7, Lists(7), false, false, At.AddHours(1), Keep)
                && Of(service, CollectionKind.Title, 1) == CollectionAcquisition.Acquired && service.TitlesReadAt == At, "the readings arrive later: applied now");
            var read = new CollectionService();
            read.Publish(7, Copy.Select(entry => entry.Kind == CollectionKind.Title && entry.Id == 1 ? entry with { NegativeConfirmed = true } : entry), [], At.AddHours(1));
            Check(CompletionListsData.PublishSaved(read, 7, 7, Lists(7), true, false, At.AddHours(1), Keep) && read.TitlesReadAt is null
                && Of(read, CollectionKind.Title, 1) == CollectionAcquisition.NotAcquired && !read.State.FromCache,
                "titles read completely this session: the game's answer wins, no saved date");
        }),
        ("Saved readings path 1400: behaviour: nothing for another character, while loading, after an error", () =>
        {
            var service = new CollectionService();
            service.Publish(7, Copy, [], At, true);
            var before = service.State;
            Check(!CompletionListsData.PublishSaved(service, 8, 8, Lists(8), false, false, At, Keep) && service.State == before, "another character logged in");
            Check(!CompletionListsData.PublishSaved(service, 7, 8, Lists(8), false, false, At, Keep) && service.State == before && service.TitlesReadAt is null,
                "readings of another character");
            Check(!CompletionListsData.PublishSaved(service, 0, 0, Lists(7), false, false, At, Keep), "nobody logged in");
            service.Invalidate();
            Check(!CompletionListsData.PublishSaved(service, 7, 7, Lists(7), false, false, At, Keep) && service.State.Loading, "while loading");
            service.Fail(new InvalidOperationException("x"));
            Check(!CompletionListsData.PublishSaved(service, 7, 7, Lists(7), false, false, At, Keep) && service.State.Error == "x", "after an error");
        }),
    ];
}
