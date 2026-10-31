using TenshoAssistant.Quests;

internal static class QuestLinkTests
{
    private static QuestEntry Q(uint id, QuestProgress progress = QuestProgress.Unconfirmed, byte join = 1, params uint[] previous)
        => new(id, $"Quest {id}", "Category", 0, progress, null) { PreviousQuestIds = previous, PreviousQuestJoin = join };
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Followed quest settings migrate without losing full native identities or foreign preferences", () =>
        {
            var legacy = System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>("{\"Version\":1,\"OpenOnLoad\":true}")!;
            Check(legacy.FollowedQuests.Count == 0 && legacy.OpenOnLoad, "legacy preferences preserved with empty follow list");
            legacy.FollowedQuests = [65537,70001];
            var copied = System.Text.Json.JsonSerializer.Deserialize<TenshoAssistant.Configuration>(System.Text.Json.JsonSerializer.Serialize(legacy))!;
            Check(copied.FollowedQuests.SequenceEqual(new uint[] {65537,70001}) && copied.OpenOnLoad, "quest row identities survive settings reload");
        }),
        ("Quest details preserve native map territory issuer and level identity through invalidation", () =>
        {
            var service = new QuestService();
            service.Publish(42,[Q(65537,QuestProgress.Active) with
            { Level=50, AdditionalLevel=10, Zone="Zone", IssuerId=1000123, IssuerMapId=42, IssuerTerritoryId=100 }]);
            service.Invalidate();
            var entry = new QuestLinks(service.State.Entries).Find(65537)!;
            Check(entry.Level == 50 && entry.AdditionalLevel == 10 && entry.Zone == "Zone", "static metadata survives logout");
            Check(entry.IssuerId == 1000123 && entry.IssuerMapId == 42 && entry.IssuerTerritoryId == 100, "map territory and untyped issuer IDs remain distinct");
            Check(entry.Progress == QuestProgress.Unconfirmed && entry.Sequence is null, "static details imply no character eligibility");
        }),
        ("Quest links retain full row identities and direct reverse links without recursive cycles", () =>
        {
            var links = new QuestLinks([Q(65537, previous: [65538,65538,0]), Q(65538, previous: [65537]), Q(1)]);
            Check(links.Find(65537)!.PreviousQuestIds.SequenceEqual(new uint[] {65538}), "duplicates and zero removed");
            Check(links.Following(65537).Single().Id == 65538 && links.Following(65538).Single().Id == 65537, "cycles are finite direct links");
            Check(links.Find(1)!.Id == 1 && links.Following(1).Count == 0, "full ID not truncated to ushort");
        }),
        ("Quest all and any joins only confirm completion with positive evidence", () =>
        {
            var links = new QuestLinks([Q(1,QuestProgress.Complete),Q(2,QuestProgress.Active),Q(3)]);
            Check(links.CompletionEvidence(Q(4,join:2,previous:[1,2])), "one positive confirms ANY completion condition");
            Check(!links.CompletionEvidence(Q(4,previous:[1,2])), "active does not confirm completion for ALL");
            Check(links.CompletionEvidence(Q(4,previous:[1])), "all complete confirmed");
            Check(!links.CompletionEvidence(Q(4,join:2,previous:[2,3,999])), "unconfirmed and missing never count as complete");
        }),
        ("Quest unknown joins missing rows and absent links never invent satisfied conditions", () =>
        {
            var links = new QuestLinks([Q(1,QuestProgress.Complete)]);
            Check(!links.CompletionEvidence(Q(2,join:99,previous:[1])), "unknown join preserved");
            Check(!links.CompletionEvidence(Q(2)) && !links.CompletionEvidence(Q(2,previous:[999])), "no vacuous success");
            Check(QuestLinks.JoinLabel(99).Contains("99") && links.Find(999) is null, "raw mode and unresolved identity retained");
        }),
        ("Quest publication snapshots nested links and logout removes character progression", () =>
        {
            var ids = new uint[] { 1,2 };
            var service = new QuestService();
            service.Publish(42,[Q(1,QuestProgress.Complete),Q(3,previous:ids) with { IsRepeatable = true, PreviousQuestStep = 4 }]);
            ids[0] = 999;
            var entry = service.State.Entries.Single(q => q.Id == 3);
            Check(entry.PreviousQuestIds[0] == 1 && entry.PreviousQuestStep == 4 && entry.IsRepeatable, "managed data detached with raw metadata");
            var links = new QuestLinks(service.State.Entries);
            service.Invalidate();
            Check(links.Find(1)!.Progress == QuestProgress.Complete && service.State.Entries.All(q => q.Progress == QuestProgress.Unconfirmed), "old snapshot and ownerless state distinct");
            Check(!new QuestLinks(service.State.Entries).CompletionEvidence(Q(4,previous:[1])), "old positive does not leak after logout");
        }),
        ("Quest reverse index owns producer lists and preserves unresolved prerequisite links", () =>
        {
            var ids = new List<uint> {999};
            var source = new List<QuestEntry> { Q(2) with { PreviousQuestIds = ids } };
            var links = new QuestLinks(source); ids.Clear(); source.Clear();
            Check(links.Following(999).Single().Id == 2 && links.Find(2)!.PreviousQuestIds.Single() == 999, "missing named row retained, source detached");
            try { ((IList<uint>)links.Find(2)!.PreviousQuestIds).Clear(); throw new Exception("mutable link list"); }
            catch (NotSupportedException) { }
        })
    ];
}
