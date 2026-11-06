using System.Globalization;

namespace TenshoAssistant.Hunting;

public enum SRankRefKind { Mob, Item, Minion, Fate, Weather, Action }
/// <summary>What the sources say of an attempt once the mark's window is open.</summary>
public enum SRankOdds { Unknown, Guaranteed, Chance }
/// <summary>A game row named by a condition; <see cref="Count"/> when the condition asks for a number of it.</summary>
public readonly record struct SRankRef(SRankRefKind Kind,uint Id,int Count=0);
/// <summary>An S rank mark: its names are game rows, its condition a text of the plugin. Territory 0: every zone of the expansion. Hours of the window: normal, then after a maintenance.</summary>
public sealed record SRankMark(int Expansion,uint Name,uint Territory,string Condition,SRankRef[] Refs,string? Place,SRankOdds Odds,int Minimum,int Maximum,int MaintenanceMinimum,int MaintenanceMaximum);
public sealed record SRankRow(SRankMark Mark,string Name,string Zone,string Expansion,string Text);

/// <summary>
/// N-12 (0.670): the spawn conditions of the S ranks. They are not in the game's data: community knowledge, crossed between several sources and
/// written in Tensho's own words (docs/evidence/0.670-srank-conditions.md). The rows were found from the names through XIVAPI; nothing is counted.
/// </summary>
public static class SRankConditions
{
    public static readonly IReadOnlyList<SRankMark> Marks=
    [
        new(5,13444,1188,"MinionSpot",[new(SRankRefKind.Minion,180)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(5,13360,1187,"WeatherMoon",[new(SRankRefKind.Weather,4)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(5,12754,1189,"DiscardAtLeast",[new(SRankRefKind.Item,7767,50)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(5,13399,1190,"FateThree",[new(SRankRefKind.Fate,1862)],"14, 23.5",SRankOdds.Guaranteed,84,132,50,80),
        new(5,13156,1191,"CraftHq",[new(SRankRefKind.Item,44091)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(5,13437,1192,"BlueSpell",[new(SRankRefKind.Action,18297)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(5,13406,0,"Extraordinary",[new(SRankRefKind.Mob,13407)],null,SRankOdds.Unknown,84,132,50,80),
        new(4,10617,956,"MinionDay",[new(SRankRefKind.Minion,434),new(SRankRefKind.Weather,1),new(SRankRefKind.Weather,2)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(4,10618,957,"Kill",[new(SRankRefKind.Mob,10698,100),new(SRankRefKind.Mob,10697,100),new(SRankRefKind.Mob,10701,100)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(4,10619,958,"WearDie",[new(SRankRefKind.Item,6223),new(SRankRefKind.Item,6224)],null,SRankOdds.Unknown,84,132,50,80),
        new(4,10620,959,"Kill",[new(SRankRefKind.Mob,10463,100),new(SRankRefKind.Mob,10462,100),new(SRankRefKind.Mob,10461,100)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(4,10621,961,"DiscardStack",[new(SRankRefKind.Item,36256,5)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(4,10622,960,"TenPlayers",[new(SRankRefKind.Minion,423)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(4,10615,0,"Extraordinary",[new(SRankRefKind.Mob,10616)],null,SRankOdds.Unknown,84,132,50,80),
        new(3,8905,813,"Discard",[new(SRankRefKind.Item,27850)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(3,8653,816,"MinionSpot",[new(SRankRefKind.Minion,303)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(3,8910,814,"Gather",[new(SRankRefKind.Item,27759,50)],null,SRankOdds.Unknown,84,132,50,80),
        new(3,8900,815,"BlueSpell",[new(SRankRefKind.Action,11408)],null,SRankOdds.Unknown,84,132,50,80),
        new(3,8890,817,"Kill",[new(SRankRefKind.Mob,8598,100),new(SRankRefKind.Mob,8599,100),new(SRankRefKind.Mob,8789,100)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(3,8895,818,"Leeches",[new(SRankRefKind.Mob,8632),new(SRankRefKind.Mob,8618),new(SRankRefKind.Action,17378)],null,SRankOdds.Unknown,84,132,50,80),
        new(3,8915,0,"Extraordinary",[new(SRankRefKind.Mob,8916)],null,SRankOdds.Unknown,84,132,50,80),
        new(2,5987,612,"Kill",[new(SRankRefKind.Mob,5671,100),new(SRankRefKind.Mob,5685,100)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(2,5985,614,"MinionFly",[new(SRankRefKind.Minion,215)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(2,5984,613,"KillFullMoon",[new(SRankRefKind.Mob,5750,100),new(SRankRefKind.Mob,5751,100)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(2,5988,620,"Porter",[],null,SRankOdds.Chance,84,132,50,80),
        new(2,5986,622,"FateFly",[new(SRankRefKind.Fate,1259)],"12, 14",SRankOdds.Guaranteed,84,132,50,80),
        new(2,5989,621,"DiscardTimes",[],null,SRankOdds.Guaranteed,84,132,50,80),
        new(1,4374,397,"MinionFly",[new(SRankRefKind.Minion,148)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(1,4375,398,"FateFive",[new(SRankRefKind.Fate,831)],"19, 27",SRankOdds.Guaranteed,84,132,50,80),
        new(1,4377,400,"GatherEither",[new(SRankRefKind.Item,12634),new(SRankRefKind.Item,12536)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(1,4378,401,"Ability",[new(SRankRefKind.Mob,4358),new(SRankRefKind.Action,4523)],null,SRankOdds.Chance,84,132,50,80),
        new(1,4376,399,"TreasureMap",[new(SRankRefKind.Item,12242)],null,SRankOdds.Chance,84,132,50,80),
        new(1,4380,402,"Kill",[new(SRankRefKind.Mob,3540,50),new(SRankRefKind.Mob,3556,50),new(SRankRefKind.Mob,3580,50)],null,SRankOdds.Guaranteed,84,132,50,80),
        new(0,2962,134,"NodeEvening",[new(SRankRefKind.Item,7760)],null,SRankOdds.Chance,65,75,39,45),
        new(0,2963,135,"FullMoon",[],null,SRankOdds.Guaranteed,50,50,30,30),
        new(0,2965,138,"NodeMorning",[new(SRankRefKind.Item,7593)],null,SRankOdds.Chance,65,75,39,45),
        new(0,2964,137,"Timer",[new(SRankRefKind.Weather,7)],null,SRankOdds.Guaranteed,42,48,21,29),
        new(0,2966,139,"AnyMinion",[],null,SRankOdds.Unknown,47,53,28,32),
        new(0,2967,180,"PlayerDies",[],null,SRankOdds.Unknown,65,71,39,43),
        new(0,2958,141,"Food",[],null,SRankOdds.Unknown,66,78,39,47),
        new(0,2957,140,"Fish",[new(SRankRefKind.Item,7714)],null,SRankOdds.Chance,57,63,34,38),
        new(0,2959,145,"Leves",[],null,SRankOdds.Chance,66,78,39,47),
        new(0,2960,146,"FatesHour",[],null,SRankOdds.Guaranteed,44,54,26,33),
        new(0,2961,147,"Kill",[new(SRankRefKind.Mob,113,100)],null,SRankOdds.Guaranteed,57,63,34,38),
        new(0,2953,148,"SecondWeather",[new(SRankRefKind.Weather,7)],null,SRankOdds.Unknown,42,48,25,29),
        new(0,2955,153,"NewMoonMidnight",[],null,SRankOdds.Guaranteed,50,50,30,30),
        new(0,2954,152,"Leves",[],null,SRankOdds.Chance,66,78,39,47),
        new(0,2956,154,"FishHours",[new(SRankRefKind.Item,7695)],null,SRankOdds.Chance,57,63,34,38),
        new(0,2968,155,"Cliffs",[],null,SRankOdds.Guaranteed,60,84,36,51),
        new(0,2969,156,"TreasureAny",[],null,SRankOdds.Chance,60,84,36,51),
    ];

    /// <summary>The arguments of a condition's text: all the references with a count form one list, otherwise one argument each; then the place.</summary>
    public static string[] Arguments(SRankMark mark,Func<SRankRef,string> name)
    {
        List<string> arguments=mark.Refs.Length>0 && mark.Refs.All(item=>item.Count>0)
            ? [string.Join(", ",mark.Refs.Select(item=>$"{item.Count.ToString(CultureInfo.InvariantCulture)} {name(item)}"))]
            : [.. mark.Refs.Select(name)];
        if(mark.Place is { } place)arguments.Add(place);
        return [.. arguments];
    }

    public static bool Matches(SRankRow row,string query)
        =>query.Length==0 || row.Name.Contains(query,StringComparison.OrdinalIgnoreCase) || row.Zone.Contains(query,StringComparison.OrdinalIgnoreCase) || row.Text.Contains(query,StringComparison.OrdinalIgnoreCase);
}
