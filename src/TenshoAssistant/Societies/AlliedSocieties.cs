namespace TenshoAssistant.Societies;

/// <summary>W-10 (0.713): an allied society, from BeastTribe. MapId and X, Z: its Level row, 0 when it has none. docs/evidence/0.713-allied-societies.md.</summary>
public sealed record AlliedSociety(uint Row,string Name,string Expansion,string Place,int MaxRank,uint Currency,int Order,uint MapId=0,float X=0,float Z=0);
public sealed record AlliedCatalog(IReadOnlyList<AlliedSociety> Societies,IReadOnlyList<string> RankNames);

public enum AlliedReadStatus { Loaded,NotLoaded,Unavailable }
public readonly record struct AlliedStanding(int Rank,int Reputation,int Needed);
public sealed record AlliedSnapshot(AlliedReadStatus Status,IReadOnlyDictionary<uint,AlliedStanding> Standings,int? AllowancesLeft)
{
    public static AlliedSnapshot Of(AlliedReadStatus status)=>new(status,new Dictionary<uint,AlliedStanding>(),null);
}

public static class AlliedBoard
{
    /// <summary>A rank beyond the sheets means the game has changed, or the index is wrong: nothing is shown rather than a wrong rank.</summary>
    public static AlliedReadStatus Check(IReadOnlyList<AlliedSociety> societies,IReadOnlyDictionary<uint,AlliedStanding> standings,int ranks)
    {
        foreach(var society in societies)
            if(standings.TryGetValue(society.Row,out var standing) && (standing.Rank<0 || standing.Rank>=ranks || standing.Reputation<0))return AlliedReadStatus.Unavailable;
        return AlliedReadStatus.Loaded;
    }

    /// <summary>The game's name of a rank; rank 0 is no rank yet, a rank outside the sheet its number.</summary>
    public static string RankName(IReadOnlyList<string> names,int rank)=>rank>=0 && rank<names.Count && names[rank].Length>0 ? names[rank]:rank.ToString();

    /// <summary>By the game's order: expansion as listed, then its own order.</summary>
    public static IReadOnlyList<AlliedSociety> Ordered(IEnumerable<AlliedSociety> societies)=>societies.OrderBy(society=>society.Row).ToArray();

    public static bool AtMax(AlliedSociety society,AlliedStanding standing)=>society.MaxRank>0 && standing.Rank>=society.MaxRank;
}
