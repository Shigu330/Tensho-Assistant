using TenshoAssistant.Community;

namespace TenshoAssistant.Gathering;

/// <summary>
/// R-15 (0.417): spearfishing help. The game's window (AddonSpearFishing.Fish, three slots) shows each fish's size (SpearfishSize:
/// Small 1, Average 2, Large 3) and speed (a number). GatherBuddy (Apache-2.0) names the speeds; their values, from its
/// SpearfishSpeed enum, are used as the game's speed numbers (docs/evidence/0.417-spearfishing.md; to confirm in game). A slot's
/// candidates are the GatherBuddy fish with that size and speed, those gathered in the current zone first.
/// </summary>
public sealed record SpearfishSlot(int Index,bool Available,int Size,int Speed,bool Large,bool Reverse);
public sealed record SpearfishCandidates(IReadOnlyList<uint> InZone,IReadOnlyList<uint> Elsewhere);

public static class Spearfishing
{
    private static readonly Dictionary<string,int> Speeds=new(StringComparer.Ordinal)
    {
        ["SuperSlow"]=100,["ExtremelySlow"]=150,["VerySlow"]=200,["Slow"]=250,["Average"]=300,["Fast"]=350,
        ["VeryFast"]=400,["ExtremelyFast"]=450,["SuperFast"]=500,["HyperFast"]=550,["LynFast"]=600,
    };
    private static readonly Dictionary<string,int> Sizes=new(StringComparer.Ordinal){["Small"]=1,["Average"]=2,["Large"]=3};

    public static int? Speed(string? name)=>name is not null && Speeds.TryGetValue(name,out var value) ? value:null;
    public static int? Size(string? name)=>name is not null && Sizes.TryGetValue(name,out var value) ? value:null;
    /// <summary>The GatherBuddy name of a speed (language files key « Spearfishing.Speed{name} »), or null for a value it does not name.</summary>
    public static string? SpeedName(int speed)=>Speeds.FirstOrDefault(pair=>pair.Value==speed).Key;

    public static SpearfishCandidates Candidates(IEnumerable<GatherBuddyFish> fish,SpearfishSlot slot,Func<uint,bool> inZone)
    {
        if(!slot.Available || slot.Size is < 1 or > 3 || slot.Speed<=0)return new([],[]);
        var matches=fish.Where(entry=>entry.SpearSize==slot.Size && entry.SpearSpeed==slot.Speed).Select(entry=>entry.FishId).Distinct().Order().ToArray();
        return new(matches.Where(inZone).ToArray(),matches.Where(id=>!inZone(id)).ToArray());
    }
}

/// <summary>R-15 (0.417): what Tensho read in the game's spearfishing window, with the zone and the window's place on screen.</summary>
public sealed record SpearfishFrame(IReadOnlyList<SpearfishSlot> Slots,uint TerritoryId,float X,float Y,float Width,DateTimeOffset ObservedAt);
