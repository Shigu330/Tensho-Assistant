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

    /// <summary>
    /// X-79 (0.891, user request: the whole list always shown, the fish in sight highlighted): every fish of the point, in a fixed order (size, speed, id) so that
    /// it does not move while fish come and go.
    /// </summary>
    public static IReadOnlyList<SpearfishRow> Rows(IEnumerable<uint> fish,Func<uint,(int Size,int Speed)?> seen,Func<uint,(int Size,int Speed)?> community,IReadOnlyList<SpearfishSlot> slots)
    {
        var inSight=slots.Where(slot=>slot.Available).SelectMany(Shadows).ToHashSet();
        return fish.Distinct().Select(id=>
        {
            var own=seen(id);var shadow=own ?? community(id);
            return new SpearfishRow(id,shadow?.Size,shadow?.Speed,own is not null,shadow is { } known && inSight.Contains(known));
        }).OrderBy(row=>row.Size ?? 9).ThenBy(row=>row.Speed ?? int.MaxValue).ThenBy(row=>row.Fish).ToArray();
    }

    /// <summary>X-79 (0.897, user request: less text, sizes S M L, speeds as chevrons): a shadow at a glance, e.g. « M >>> ».</summary>
    public static string SizeLetter(int size)=>size switch{1=>"S",2=>"M",3=>"L",_=>"?"};
    /// <summary>X-79 (0.897): one chevron per speed step of 50 above 50 (100 → 1, 150 → 2, 200 → 3 … 600 → 11), 0 when unknown.</summary>
    public static int SpeedStep(int speed)=>speed<=50 ? 0:Math.Min(12,(speed-50)/50);
    public static string ShortShadow(int size,int speed)=>$"{SizeLetter(size)} {new string('>',SpeedStep(speed))}".TrimEnd();

    /// <summary>0.896: the shadows a slot may stand for: its speed, and half of it (a shadow's speed doubles while it swims, docs/evidence/0.896-spearfishing-lag.md).</summary>
    public static IEnumerable<(int Size,int Speed)> Shadows(SpearfishSlot slot)
    {
        yield return (slot.Size,slot.Speed);
        if(slot.Speed>0 && slot.Speed%2==0)yield return (slot.Size,slot.Speed/2);
    }

    public static SpearfishCandidates Candidates(IEnumerable<GatherBuddyFish> fish,SpearfishSlot slot,Func<uint,bool> inZone)
    {
        if(!slot.Available || slot.Size is < 1 or > 3 || slot.Speed<=0)return new([],[]);
        var matches=fish.Where(entry=>entry.SpearSize==slot.Size && entry.SpearSpeed==slot.Speed).Select(entry=>entry.FishId).Distinct().Order().ToArray();
        return new(matches.Where(inZone).ToArray(),matches.Where(id=>!inZone(id)).ToArray());
    }
}

/// <summary>
/// R-15 (0.417): what Tensho read in the game's spearfishing window, with the zone and the window's place on screen. 0.891, 0.894: BaseId = the
/// GatheringPointBase row of the node targeted when the window opened (0 when none was seen).
/// </summary>
public sealed record SpearfishFrame(IReadOnlyList<SpearfishSlot> Slots,uint TerritoryId,float X,float Y,float Width,DateTimeOffset ObservedAt,uint BaseId=0);

/// <summary>X-79 (0.891): one fish of the point in the spearfishing panel: its shadow (yours first, else GatherBuddy's), and whether a shadow like it is in sight.</summary>
public sealed record SpearfishRow(uint Fish,int? Size,int? Speed,bool Seen,bool InSight);
