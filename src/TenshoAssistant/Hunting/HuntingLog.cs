namespace TenshoAssistant.Hunting;

/// <summary>A monster to defeat for a hunting log entry: its name, the kills needed and the zones the game names.</summary>
public sealed record HuntTarget(string Monster,int Needed,string Zones);

/// <summary>A hunting log entry (MonsterNote row = log key × 10 000 + rank × 10 + number, 1-based number).</summary>
public sealed record HuntEntry(uint RowId,string Name,IReadOnlyList<HuntTarget> Targets)
{
    public uint LogKey=>RowId/10000;
    public int Rank=>(int)((RowId%10000-1)/10);
    public int Number=>(int)((RowId%10000-1)%10);
}

/// <summary>One log as the game holds it: the current rank and, for that rank only, the kills of each entry's targets (10 × 4).</summary>
public sealed record HuntSlot(int Rank,IReadOnlyList<IReadOnlyList<byte>> Counts);

/// <summary>
/// g, C-12 (0.364): hunting log. Logs of the nine classes and three Grand Companies (MonsterNote sheet, docs/evidence/0.364-hunting-log.md);
/// the game keeps only the current rank's kills — earlier ranks are complete, later ones not started. Original code.
/// </summary>
public static class HuntingLog
{
    /// <summary>Slot of a log in MonsterNoteManager.RankData (order learned from Hunty, MIT, read only); null for an unknown log.</summary>
    public static int? MemorySlot(uint logKey)=>logKey switch
    {
        >= 1 and <= 7=>(int)logKey-1,
        26=>7,
        100=>8,
        200=>9,
        300=>10,
        29=>11,
        _=>null,
    };

    /// <summary>
    /// 0.393 (user report « you do not look at the character's company »): a Grand Company log (keys 100, 200, 300 = Grand Company rows
    /// 1, 2, 3) is shown only for the character's own company, like the game and Hunty; none without a company; all three while the
    /// company is not read yet (null). Class logs are always shown.
    /// </summary>
    public static bool Shown(uint logKey,uint? grandCompany)=>logKey is not (100 or 200 or 300) || grandCompany is not { } company || logKey==company*100;

    /// <summary>Kills of a target: all of them for a rank already passed, none for a rank not reached, the game's count otherwise; null = not read.</summary>
    public static int? Killed(HuntEntry entry,int target,HuntSlot? slot)
    {
        if(slot is null || target<0 || target>=entry.Targets.Count)return null;
        if(slot.Rank>entry.Rank)return entry.Targets[target].Needed;
        if(slot.Rank<entry.Rank)return 0;
        return entry.Number<slot.Counts.Count && target<slot.Counts[entry.Number].Count
            ? Math.Min(entry.Targets[target].Needed,(int)slot.Counts[entry.Number][target]):null;
    }

    /// <summary>True when every target of the entry is done; null when not read.</summary>
    public static bool? Done(HuntEntry entry,HuntSlot? slot)
    {
        if(slot is null)return null;
        for(var target=0;target<entry.Targets.Count;target++)
            if(Killed(entry,target,slot) is not { } killed)return null;
            else if(killed<entry.Targets[target].Needed)return false;
        return true;
    }
}
