namespace TenshoAssistant.Gathering;

/// <summary>0.518: what a gathering point asks of the player, from the game's sheets. <paramref name="TomeBit"/> and <paramref name="Tome"/> are null for a point without a folklore tome.</summary>
public sealed record GatherPointNeed(uint JobId,int? TomeBit,string? Tome);

/// <summary>C27 (0.647): the kind of a gathering point, from GatheringPoint.Type (docs/evidence/0.647-gather-point-kinds.md).</summary>
public enum GatherPointKind{Normal,Timed,Ephemeral,Legendary}
public static class GatherPointKinds
{
    public static GatherPointKind Of(byte type)=>type switch {2=>GatherPointKind.Timed,4=>GatherPointKind.Ephemeral,5=>GatherPointKind.Legendary,_=>GatherPointKind.Normal};
}

/// <summary>What the logged-in character has, copied on the framework thread.</summary>
public sealed record GatherKnowledge(int MinerLevel,int BotanistLevel,IReadOnlySet<int> MinerTomes,IReadOnlySet<int> BotanistTomes);

/// <summary>0.526: the tomes set in a job's array. By its bytes: FFXIVClientStructs declares 11 bits, and the sheet's twelfth tome is bit 11 (docs/evidence/0.526-folklore-tomes.md).</summary>
public static class GatherTomeBits
{
    public static IReadOnlySet<int> Of(ReadOnlySpan<byte> bytes)
    {
        var set=new HashSet<int>();
        for(var bit=0;bit<bytes.Length*8;bit++)if((bytes[bit>>3]>>(bit&7)&1)!=0)set.Add(bit);
        return set;
    }
}

/// <summary>What stops the player at a point: a tome not read, a job level under the point's. Null: nothing Tensho knows of.</summary>
public sealed record GatherPointAccess(string? TomeMissing,uint JobId,int? JobLevel)
{
    public const uint Miner=16,Botanist=17;

    /// <summary>The job of a GatheringType row: 0 mining and 1 quarrying, 2 logging and 3 harvesting (docs/evidence/0.518-gather-tomes.md); fishing is not a point here.</summary>
    public static uint JobOf(uint gatheringType)=>gatheringType switch {0 or 1=>Miner,2 or 3=>Botanist,_=>0};

    public static GatherPointAccess? Of(GatherPointNeed? need,GatherKnowledge? known)
    {
        if(need is null || known is null || need.JobId is not (Miner or Botanist))return null;
        var tomes=need.JobId==Miner ? known.MinerTomes:known.BotanistTomes;
        var missing=need.TomeBit is { } bit && !tomes.Contains(bit) ? need.Tome:null;
        var level=need.JobId==Miner ? known.MinerLevel:known.BotanistLevel;
        return new(missing,need.JobId,level>0 ? level:null);
    }
}
