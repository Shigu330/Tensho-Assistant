namespace TenshoAssistant.Reminders;

public readonly record struct SquadronStats(int Physical,int Mental,int Tactical)
{
    public static SquadronStats operator +(SquadronStats left,SquadronStats right)
        =>new(left.Physical+right.Physical,left.Mental+right.Mental,left.Tactical+right.Tactical);
}

/// <summary>One of the six requirement sets of a mission (GcArmyExpedition.ExpeditionParams) with the success percent each part adds.</summary>
public sealed record SquadronRequirement(SquadronStats Required,int PercentPhysical,int PercentMental,int PercentTactical,int PercentAll);

/// <summary>A squadron mission from the game sheet: level, base success percent and its requirement sets.</summary>
public sealed record SquadronMission(uint Id,string Name,int Level,uint Type,int PercentBase,IReadOnlyList<SquadronRequirement> Sets);

/// <summary>A member as the game holds it (GcArmyManager), with the attributes the GcArmyMemberGrow sheet gives for its class and level.</summary>
public sealed record SquadronMember(int Slot,uint ClassJob,int Level,bool Busy,SquadronStats? Stats);

/// <summary>A squadron member as the game holds it, copied from GcArmyManager (raw values, for the planner and the probe log).</summary>
public sealed record SquadronRawMember(int Slot,uint ENpcResidentId,byte Race,byte ClassJob,byte Level,uint Experience,byte Flags,
    byte ActiveTrait,byte ActiveTraitCond,byte InactiveTrait,byte InactiveTraitCond);

/// <summary>The squadron as the game holds it once loaded in the barracks (GcArmyData), copied.</summary>
public sealed record SquadronRaw(byte Progress,ushort CurrentExpedition,SquadronStats Bonus,IReadOnlyList<SquadronRawMember> Members);

/// <summary>The game sheets the planner needs: missions, attributes by class (GcArmyMemberGrow, keyed by ClassJob) and class names.</summary>
public sealed record SquadronSheets(IReadOnlyList<SquadronMission> Missions,IReadOnlyDictionary<uint,(IReadOnlyList<SquadronStats> ByIndex,SquadronStats? Sixty)> Grow,
    IReadOnlyDictionary<uint,string> ClassNames);

public sealed record SquadronParty(IReadOnlyList<SquadronMember> Members,SquadronStats Total,int Met,int Percent);

/// <summary>
/// N-06 (0.451, DEC-027, point 1): the squadron mission planner, from the game's sheets only (docs/evidence/0.448-n06-n14-sources.md).
/// Success percent = PercentBase + the percent of each attribute the party reaches + PercentAll when it reaches all three (sheet columns
/// of GcArmyExpedition; the reading is not validated in game). The party's attributes are the four members' (GcArmyMemberGrow by class
/// and level) plus the squadron's training bonus. Chemistry is not counted (its sheet columns are unnamed). Nothing is guessed: a member
/// whose attributes are unknown is left out.
/// </summary>
public static class SquadronPlanner
{
    public const int PartySize=4;

    /// <summary>How many of the three attributes reach the requirement.</summary>
    public static int Met(SquadronStats total,SquadronStats required)
        =>(total.Physical>=required.Physical ? 1:0)+(total.Mental>=required.Mental ? 1:0)+(total.Tactical>=required.Tactical ? 1:0);

    public static int Percent(SquadronMission mission,SquadronRequirement set,SquadronStats total)
    {
        var percent=mission.PercentBase;
        if(total.Physical>=set.Required.Physical)percent+=set.PercentPhysical;
        if(total.Mental>=set.Required.Mental)percent+=set.PercentMental;
        if(total.Tactical>=set.Required.Tactical)percent+=set.PercentTactical;
        if(Met(total,set.Required)==3)percent+=set.PercentAll;
        return Math.Clamp(percent,0,100);
    }

    /// <summary>
    /// The best parties of four free members with known attributes, by success percent, then by attributes reached, then by the smallest
    /// shortfall; at most <paramref name="count"/>.
    /// </summary>
    public static IReadOnlyList<SquadronParty> Best(IReadOnlyList<SquadronMember> members,SquadronStats bonus,SquadronMission mission,SquadronRequirement set,int count=5)
    {
        var free=members.Where(member=>!member.Busy && member.Stats is not null).ToArray();
        if(free.Length<PartySize || count<=0)return [];
        var parties=new List<(SquadronParty Party,int Shortfall)>();
        for(var a=0;a<free.Length;a++)
        for(var b=a+1;b<free.Length;b++)
        for(var c=b+1;c<free.Length;c++)
        for(var d=c+1;d<free.Length;d++)
        {
            SquadronMember[] party=[free[a],free[b],free[c],free[d]];
            var total=party.Aggregate(bonus,(sum,member)=>sum+member.Stats!.Value);
            var shortfall=Math.Max(0,set.Required.Physical-total.Physical)+Math.Max(0,set.Required.Mental-total.Mental)+Math.Max(0,set.Required.Tactical-total.Tactical);
            parties.Add((new(party,total,Met(total,set.Required),Percent(mission,set,total)),shortfall));
        }
        return parties.OrderByDescending(entry=>entry.Party.Percent).ThenByDescending(entry=>entry.Party.Met).ThenBy(entry=>entry.Shortfall)
            .ThenBy(entry=>string.Join(',',entry.Party.Members.Select(member=>member.Slot))).Take(count).Select(entry=>entry.Party).ToArray();
    }

    /// <summary>The members read from the game with their sheet attributes; busy = on a mission or in training (GcArmyMemberFlag 1 and 2).</summary>
    public static IReadOnlyList<SquadronMember> Members(SquadronRaw squadron,SquadronSheets sheets)
        =>squadron.Members.Select(member=>new SquadronMember(member.Slot,member.ClassJob,member.Level,(member.Flags&3)!=0,
            sheets.Grow.TryGetValue(member.ClassJob,out var grow) ? GrowStats(grow.ByIndex,grow.Sixty,member.Level):null)).ToArray();

    /// <summary>
    /// A member's attributes from the GcArmyMemberGrow row of its class: index = level for levels 1 to 59 (index 0 is all zero), and level 60
    /// in the four columns after the collection (Lumina names them Unknown0–3; values proved by the sheet: 1 above level 59). Null otherwise.
    /// </summary>
    public static SquadronStats? GrowStats(IReadOnlyList<SquadronStats> byIndex,SquadronStats? levelSixty,int level)
    {
        if(level>=1 && level<byIndex.Count)return byIndex[level] is { Physical:0, Mental:0, Tactical:0 } ? null:byIndex[level];
        return level==byIndex.Count ? levelSixty:null;
    }
}
