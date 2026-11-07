using System.Text.RegularExpressions;
using TenshoAssistant.Duties;

namespace TenshoAssistant.DeepDungeons;

/// <summary>W-15 (0.718): a pomander, a stone or a demiclone, as the sheet has it. docs/evidence/0.718-deep-dungeons.md.</summary>
public sealed record DeepDungeonThing(uint Id,string Name,string Tooltip,uint Icon);
public sealed record DeepDungeonEntry(uint Id,string Name,IReadOnlyList<DeepDungeonThing> Pomanders,IReadOnlyList<DeepDungeonThing> Others);

public static partial class DeepDungeonBoard
{
    /// <summary>ItemSupplementSource in LuminaSupplemental: 8 Palace of the Dead, 9 Heaven-on-High, 10 Eureka Orthos, 17 Pilgrim's Traverse.</summary>
    public static int LootKind(uint dungeon)=>dungeon switch { 1=>8,2=>9,3=>10,4=>17,_=>0 };

    /// <summary>The duties of a dungeon, in the game's order: their names hold the dungeon's, in every language checked.</summary>
    public static IReadOnlyList<DutyDefinition> Floors(IEnumerable<DutyDefinition> duties,string dungeon)=>
        dungeon.Length==0 ? []:duties.Where(duty=>duty.Name.Contains(dungeon,StringComparison.CurrentCultureIgnoreCase)).OrderBy(duty=>duty.DutyId).ToArray();

    /// <summary>What « Others » holds, by the sheet's slots. Pilgrim's Traverse has slots with no sheet of their own, 5, 4 and 5: read as its three incenses, stone 5 and demiclones 4 and 5, the only rows of that kind.</summary>
    public static (IReadOnlyList<uint> Stones,IReadOnlyList<uint> Demiclones) Others(uint dungeon,IEnumerable<uint> slots)
    {
        var used=slots.Where(slot=>slot!=0).ToArray();
        return dungeon switch
        {
            2=>(used,[]),
            3=>([],used),
            4=>([5],[4,5]),
            _=>([],[]),
        };
    }

    /// <summary>The sheet's text says the floor's word through the game's number 109, the dungeon: 1 to 4. ExtractText would leave a hole there.</summary>
    public static string Text(string macro,uint dungeon)
    {
        var text=SwitchMacro().Replace(macro,match=>
        {
            var words=match.Groups[1].Value.Split(',');
            return dungeon>=1 && dungeon<=words.Length ? words[dungeon-1]:"";
        });
        text=IfMacro().Replace(text,match=>match.Groups[1].Value==dungeon.ToString() ? match.Groups[2].Value:match.Groups[3].Value);
        text=text.Replace("<br>","\n",StringComparison.Ordinal);
        return Whitespace().Replace(OtherMacro().Replace(text,""),"$1").Trim();
    }

    [GeneratedRegex(@"<switch\(gnum109,([^<>]*)\)>")] private static partial Regex SwitchMacro();
    [GeneratedRegex(@"<if\(\[gnum109==(\d+)\],([^,<>]*),([^,<>]*)\)>")] private static partial Regex IfMacro();
    [GeneratedRegex(@"<[^<>]*>")] private static partial Regex OtherMacro();
    [GeneratedRegex(@"([ \t])[ \t]+")] private static partial Regex Whitespace();
}
