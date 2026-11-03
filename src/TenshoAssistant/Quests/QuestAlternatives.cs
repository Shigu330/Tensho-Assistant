namespace TenshoAssistant.Quests;

/// <summary>
/// Quests the character can no longer play because another option was taken (0.286, docs/evidence/0.286-quest-alternatives.md):
/// a completed quest locks it (QuestLock, e.g. the Grand Company choice); or it is one branch of a quest that needs any one of its
/// previous quests (PreviousQuestJoin 2) and another branch is completed; or, by propagation, all its follow-ups are unplayable,
/// or it needs (join 1: all, join 2: any) previous quests that are unplayable. Completed quests are never in the set.
/// </summary>
public static class QuestAlternatives
{
    public const byte AnyPrevious = 2;

    public static HashSet<uint> NotPlayable(IReadOnlyCollection<QuestEntry> entries)
    {
        var byId = entries.ToDictionary(entry => entry.Id);
        bool Done(uint id) => byId.TryGetValue(id, out var entry) && entry.Progress == QuestProgress.Complete;
        var result = new HashSet<uint>();
        void Mark(uint id) { if (byId.ContainsKey(id) && !Done(id)) result.Add(id); }

        foreach (var entry in entries)
        {
            // Mutual locks: a completed quest closes the quests it lists, and a quest listing a completed one is closed.
            if (entry.Progress == QuestProgress.Complete) foreach (var locked in entry.LockIds) Mark(locked);
            else if (entry.LockIds.Any(Done)) Mark(entry.Id);
            // Branches: one completed option of an « any one of » join closes the others.
            if (entry.PreviousQuestJoin == AnyPrevious && entry.PreviousQuestIds.Count > 1 && entry.PreviousQuestIds.Any(Done))
                foreach (var option in entry.PreviousQuestIds) Mark(option);
        }

        // Follow-ups of each quest, to propagate both ways until nothing changes.
        var next = new Dictionary<uint, List<uint>>();
        foreach (var entry in entries)
            foreach (var previous in entry.PreviousQuestIds)
                if (byId.ContainsKey(previous)) (next.TryGetValue(previous, out var list) ? list : next[previous] = []).Add(entry.Id);
        for (var changed = true; changed;)
        {
            changed = false;
            foreach (var entry in entries)
            {
                if (entry.Progress == QuestProgress.Complete || result.Contains(entry.Id)) continue;
                var previous = entry.PreviousQuestIds.Where(byId.ContainsKey).ToArray();
                var blockedBefore = previous.Length > 0 && (entry.PreviousQuestJoin == AnyPrevious ? previous.All(result.Contains) : previous.Any(result.Contains));
                var blockedAfter = next.TryGetValue(entry.Id, out var followUps) && followUps.Count > 0 && followUps.All(result.Contains);
                if (blockedBefore || blockedAfter) { result.Add(entry.Id); changed = true; }
            }
        }
        return result;
    }
}
