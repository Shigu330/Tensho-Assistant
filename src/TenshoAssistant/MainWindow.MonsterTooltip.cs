using Dalamud.Bindings.ImGui;
using TenshoAssistant.Maps;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1673, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a monster, wherever it
    /// is named (the Tensho map's monsters, hunt marks, leve targets and beasts to tame, the overlay above the monsters, the Monsters page):
    /// name, level or rank, zone, every reason Tensho points at it, then what the caller adds (a click's effect).
    /// </summary>
    private void DrawMonsterTooltip(MonsterTipFacts facts,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            TooltipTitle(facts.Name);
            if(TipPart("Monster.Level") && facts.Level is { } level)ImGui.TextDisabled(level);
            if(TipPart("Monster.Zone") && facts.Zone is { } zone)ImGui.TextUnformatted(zone);
            if(TipPart("Monster.Notes"))foreach(var note in facts.Notes)ImGui.TextUnformatted(note);
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }

    // X-684 (0.1673): the Tensho map writes its tooltips as text and draws the last one set; a monster's facts go with the text they were set
    // with, so a later layer's tooltip still wins (the same text reference, or the plain text).
    private (string Text,MonsterTipFacts Facts,string? After)? mapMonsterTip;
    private string MapMonsterTip(MonsterTipFacts facts,string? after=null)
    {
        var text=facts.Name+(facts.Level is { } level ? "\n"+level:"")+(facts.Notes.Count>0 ? "\n"+string.Join("\n",facts.Notes):"")+(after is { Length:>0 } ? "\n"+after:"");
        mapMonsterTip=(text,facts,after);
        return text;
    }
}
