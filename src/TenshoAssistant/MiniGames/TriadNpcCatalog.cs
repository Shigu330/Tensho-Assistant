namespace TenshoAssistant.MiniGames;

/// <summary>
/// Triple Triad NPCs and where cards come from (0.168, probes docs/evidence/0.159.0-triad-npc-probe.log and
/// docs/evidence/0.168.0-triad-sources-probe.log): NPC = ENpcBase linked to a TripleTriad row (rules, possible card rewards); card item =
/// Item whose ItemAction is 3357, first data value = card id; card pack = TripleTriadCardResident acquisition of type 8 (an Item).
/// </summary>
public sealed record TriadNpc(uint MatchId, uint NpcId, string Name, uint MapId, string Zone, float X, float Y, IReadOnlyList<uint> RewardCards,
    IReadOnlyList<uint> RuleIds, int Fee);
public sealed record TriadCardSource(uint CardId, uint ItemId, IReadOnlyList<uint> NpcMatchIds, uint PackItemId, string PackName);
public sealed record TriadNpcCatalog(IReadOnlyList<TriadNpc> Npcs, IReadOnlyDictionary<uint, TriadCardSource> Cards)
{
    /// <summary>NPCs to face: not beaten yet, or holding a reward card not owned; beaten state unknown counts as not beaten.</summary>
    public IReadOnlyList<(TriadNpc Npc, bool? Beaten, IReadOnlyList<uint> Missing)> ToFace(Func<uint, bool?> beaten, Func<uint, bool> owned, bool onlyOpen)
    {
        var rows = new List<(TriadNpc, bool?, IReadOnlyList<uint>)>();
        foreach (var npc in Npcs)
        {
            var state = beaten(npc.MatchId);
            var missing = npc.RewardCards.Where(card => !owned(card)).ToArray();
            if (onlyOpen && state == true && missing.Length == 0) continue;
            rows.Add((npc, state, Array.AsReadOnly(missing)));
        }
        return rows.OrderBy(row => row.Item2 == true).ThenByDescending(row => row.Item3.Count).ThenBy(row => row.Item1.Zone, StringComparer.CurrentCulture)
            .ThenBy(row => row.Item1.Name, StringComparer.CurrentCulture).ToArray();
    }
}
public sealed record TriadNpcState(TriadNpcCatalog? Catalog, bool Loading, string? Error, IReadOnlySet<uint>? Beaten);

public sealed class TriadNpcService
{
    private TriadNpcState state = new(null, true, null, null);
    public TriadNpcState State => Volatile.Read(ref state);
    internal void Publish(TriadNpcCatalog catalog) { var current = State; Volatile.Write(ref state, current with { Catalog = catalog, Loading = false, Error = null }); }
    internal void Fail(Exception error) { var current = State; Volatile.Write(ref state, current with { Loading = false, Error = error.Message }); }
    internal void SetBeaten(IReadOnlySet<uint>? beaten) { var current = State; Volatile.Write(ref state, current with { Beaten = beaten }); }
}
