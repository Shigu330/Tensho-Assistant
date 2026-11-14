namespace TenshoAssistant.Societies;

/// <summary>W-10 (0.713): an allied society, from BeastTribe. MapId and X, Z: its Level row, 0 when it has none. docs/evidence/0.713-allied-societies.md.</summary>
/// <summary>X-265 (0.1306): Kind, what its quests ask (combat, crafting, gathering, or several), None when the sheets did not say.</summary>
public sealed record AlliedSociety(uint Row,string Name,string Expansion,string Place,int MaxRank,uint Currency,int Order,uint MapId=0,float X=0,float Z=0,AlliedKind Kind=AlliedKind.None);
/// <summary>X-265 (0.1306): KindNames, the game's names of the kinds (ClassJobCategory 34, 33, 32), empty when unread.</summary>
public sealed record AlliedCatalog(IReadOnlyList<AlliedSociety> Societies,IReadOnlyList<string> RankNames)
{
    public IReadOnlyDictionary<AlliedKind,string> KindNames { get; init; }=new Dictionary<AlliedKind,string>();
}

/// <summary>X-265 (0.1306): the kind of an allied society, from the ClassJobCategory of its quests (Quest.BeastTribe, ClassJobCategory0):
/// a class is gathering or crafting by its own ClassJobCategory (32, 33), any other combat; probed on 14/11 (tools/AlliedKindProbe): 14
/// combat societies (category 142), 5 crafting (33), 3 gathering (32), the Namazu crafting and gathering (35).</summary>
[Flags]
public enum AlliedKind { None=0,Combat=1,Crafting=2,Gathering=4 }

public static class AlliedKinds
{
    /// <summary>ClassJobCategory rows: a class's own category for gathering and crafting, and the categories naming each kind.</summary>
    public const uint GatheringCategory=32,CraftingCategory=33,CombatNameCategory=34;
    public static readonly AlliedKind[] Shown=[AlliedKind.Combat,AlliedKind.Crafting,AlliedKind.Gathering];

    /// <summary>A class's kind from its own ClassJobCategory; 0 (the adventurer) is none.</summary>
    public static AlliedKind OfClass(uint classCategory)=>classCategory switch{0=>AlliedKind.None,GatheringCategory=>AlliedKind.Gathering,CraftingCategory=>AlliedKind.Crafting,_=>AlliedKind.Combat};

    /// <summary>A ClassJobCategory's kinds: those of the classes it allows (allowed[i] for the ClassJob row i).</summary>
    public static AlliedKind OfCategory(IReadOnlyList<bool> allowed,IReadOnlyList<uint> classCategories)
    {
        var kind=AlliedKind.None;
        for(var index=0;index<allowed.Count && index<classCategories.Count;index++)
            if(allowed[index])kind|=OfClass(classCategories[index]);
        return kind;
    }

    /// <summary>Whether a society is listed under the kind chosen: None lists all; a society of several kinds is listed under each.</summary>
    public static bool Listed(AlliedSociety society,AlliedKind chosen)=>chosen==AlliedKind.None || (society.Kind&chosen)!=0;

    /// <summary>The kinds' names joined, « crafting, gathering »; empty when unknown.</summary>
    public static string Names(AlliedKind kind,IReadOnlyDictionary<AlliedKind,string> names)=>
        string.Join(", ",Shown.Where(shown=>(kind&shown)!=0).Select(shown=>names.GetValueOrDefault(shown) ?? "").Where(name=>name.Length>0));
}

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
