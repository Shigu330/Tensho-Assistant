using TenshoAssistant.Duties;

internal static class DutyLootTests
{
    private static void Check(bool value) { if (!value) throw new Exception("Duty loot assertion failed."); }
    private static void Throws(Action action) { try { action(); } catch (Exception e) when (e is InvalidDataException or System.Text.Json.JsonException or InvalidOperationException) { return; } throw new Exception("Malformed loot accepted."); }
    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Duty name-only direct fight drops remain explicitly outside parsed chest coverage", () =>
        {
            var parsed = DutyLootParser.Parse("""{"instance":{"id":4,"fights":[{"drops":[{"name":"Unresolved boss drop"}]}]}}""",4,DateTimeOffset.UtcNow);
            Check(parsed.HasUnparsedFightDrops && parsed.HasFights && parsed.Entries.Count == 0 && !parsed.IsExhaustive);
        }),
        ("Duty loot parser separates categories and preserves multi-source item provenance", () =>
        {
            var parsed = DutyLootParser.Parse("""{"instance":{"id":4,"rewards":[1608,1608],"fights":[{"name":"Boss","coffer":{"items":[1608,2684]}},{"coffer":{"items":[2684]}}],"coffers":[{"items":[3781,1608]}]},"partials":[{"id":999}]}""",4,DateTimeOffset.UtcNow);
            Check(parsed.Entries.Count == 6 && parsed.Entries.Count(e => e.ItemId == 1608) == 3);
            Check(parsed.HasRewards && parsed.HasFights && parsed.HasCoffers && !parsed.IsExhaustive);
            Check(parsed.Entries.Single(e => e.ItemId == 3781).Kind == DutyLootSourceKind.ZoneCoffer);
        }),
        ("Duty loot parser preserves missing categories and unknown item identities without invented empty coverage", () =>
        {
            var parsed = DutyLootParser.Parse("""{"instance":{"id":4,"coffers":[{"items":[4000000000]}]}}""",4,DateTimeOffset.Now);
            Check(!parsed.HasRewards && !parsed.HasFights && parsed.HasCoffers && !parsed.IsExhaustive);
            Check(parsed.Entries.Single().ItemId == 4000000000 && parsed.FetchedAt.Offset == TimeSpan.Zero);
            var absent = DutyLootParser.Parse("""{"instance":{"id":4}}""",4,DateTimeOffset.UtcNow);
            Check(absent.Entries.Count == 0 && !absent.HasCoffers && !absent.IsExhaustive);
        }),
        ("Duty loot parser rejects identity mismatch malformed categories IDs and oversized responses", () =>
        {
            foreach (var json in new[] { "{}", """{"instance":{"id":5}}""", """{"instance":{"id":4,"rewards":null}}""", """{"instance":{"id":4,"rewards":[0]}}""", """{"instance":{"id":4,"coffers":[{}]}}""", """{"instance":{"id":4,"rewards":[-1]}}""" })
                Throws(() => DutyLootParser.Parse(json,4,DateTimeOffset.UtcNow));
            Throws(() => DutyLootParser.Parse(new string(' ',DutyLootParser.MaximumBytes+1),4,DateTimeOffset.UtcNow));
        }),
        ("Duty loot parser cancellation and immutable publication prevent deferred mutations", () =>
        {
            var parsed = DutyLootParser.Parse("""{"instance":{"id":4,"rewards":[1]}}""",4,DateTimeOffset.UtcNow);
            try { ((IList<DutyLootEntry>)parsed.Entries).Clear(); throw new Exception("Mutable loot"); } catch (NotSupportedException) { }
            try { DutyLootParser.Parse("{}",4,DateTimeOffset.UtcNow,new CancellationToken(true)); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { }
        })
    ];
}
