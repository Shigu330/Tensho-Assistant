namespace TenshoAssistant.MiniGames;

/// <summary>
/// Triple Triad NPCs and where cards come from (0.168, probes docs/evidence/0.159.0-triad-npc-probe.log and
/// docs/evidence/0.168.0-triad-sources-probe.log): NPC = ENpcBase linked to a TripleTriad row (rules, possible card rewards); card item =
/// Item whose ItemAction is 3357, first data value = card id; card pack = TripleTriadCardResident acquisition of type 8 (an Item).
/// </summary>
public sealed record TriadRequiredQuest(uint QuestId, string Name);
/// <summary>0.493 (user report): Quests = TripleTriad.PreviousQuest of this NPC, AnyQuest = PreviousQuestJoin 2 (docs/evidence/0.493-triad-npc-requirements.md).</summary>
public sealed record TriadNpc(uint MatchId, uint NpcId, string Name, uint MapId, string Zone, float X, float Y, IReadOnlyList<uint> RewardCards,
    IReadOnlyList<uint> RuleIds, int Fee, IReadOnlyList<TriadRequiredQuest>? Quests = null, bool AnyQuest = false)
{
    public IReadOnlyList<TriadRequiredQuest> RequiredQuests => Quests ?? [];
    /// <summary>The quests the game asks for are done: true, false, or null when the quests are not read. Only those: nothing else is in the game's data.</summary>
    public bool? Available(IReadOnlySet<uint>? done)
    {
        if (RequiredQuests.Count == 0) return true;
        if (done is null) return null;
        return AnyQuest ? RequiredQuests.Any(quest => done.Contains(quest.QuestId)) : RequiredQuests.All(quest => done.Contains(quest.QuestId));
    }
}
public sealed record TriadCardSource(uint CardId, uint ItemId, IReadOnlyList<uint> NpcMatchIds, uint PackItemId, string PackName);
public sealed record TriadNpcCatalog(IReadOnlyList<TriadNpc> Npcs, IReadOnlyDictionary<uint, TriadCardSource> Cards, TriadRequiredQuest? Trial = null)
{
    /// <summary>NPCs to face: not beaten yet, or holding a reward card not owned; beaten state unknown counts as not beaten.</summary>
    public IReadOnlyList<(TriadNpc Npc, bool? Beaten, IReadOnlyList<uint> Missing)> ToFace(Func<uint, bool?> beaten, Func<uint, bool> owned, bool onlyOpen,
        Func<TriadNpc, bool?>? available = null, bool onlyAvailable = false)
    {
        var rows = new List<(TriadNpc, bool?, IReadOnlyList<uint>)>();
        foreach (var npc in Npcs)
        {
            var state = beaten(npc.MatchId);
            var missing = npc.RewardCards.Where(card => !owned(card)).ToArray();
            if (onlyOpen && state == true && missing.Length == 0) continue;
            if (onlyAvailable && available?.Invoke(npc) == false) continue;
            rows.Add((npc, state, Array.AsReadOnly(missing)));
        }
        return rows.OrderBy(row => available?.Invoke(row.Item1) == false).ThenBy(row => row.Item2 == true).ThenByDescending(row => row.Item3.Count).ThenBy(row => row.Item1.Zone, StringComparer.CurrentCulture)
            .ThenBy(row => row.Item1.Name, StringComparer.CurrentCulture).ToArray();
    }
}
/// <summary>BeatenUnavailable (0.331, R-14): the native beaten-NPC read failed (maybe after a game update); Beaten is then null.</summary>
public sealed record TriadNpcState(TriadNpcCatalog? Catalog, bool Loading, string? Error, IReadOnlySet<uint>? Beaten, bool BeatenUnavailable = false, IReadOnlySet<uint>? QuestsDone = null);

public sealed class TriadNpcService
{
    // 0.331 (audit): Publish/Fail run on the loader's worker, SetBeaten on the framework thread; the gate keeps a stale copy from
    // writing the catalogue back to null.
    private readonly object gate = new();
    private TriadNpcState state = new(null, true, null, null);
    public TriadNpcState State => Volatile.Read(ref state);
    internal void Publish(TriadNpcCatalog catalog) { lock (gate) { var current = State; Volatile.Write(ref state, current with { Catalog = catalog, Loading = false, Error = null }); } }
    internal void Fail(Exception error) { lock (gate) { var current = State; Volatile.Write(ref state, current with { Loading = false, Error = error.Message }); } }
    internal void SetBeaten(IReadOnlySet<uint>? beaten, bool unavailable = false, IReadOnlySet<uint>? questsDone = null) { lock (gate) { var current = State; Volatile.Write(ref state, current with { Beaten = beaten, QuestsDone = questsDone, BeatenUnavailable = unavailable && beaten is null }); } }
}
