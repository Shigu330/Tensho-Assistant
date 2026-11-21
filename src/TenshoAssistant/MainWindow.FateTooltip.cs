using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Fields;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1676, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a FATE, wherever it is
    /// named (the FATEs page, the field operations' FATEs and engagements, the Tensho map's list of FATEs under way): icon, name, level and
    /// kind, place, its state now, the game's description, the players it takes, then what the caller adds (a click's effect).
    /// </summary>
    private void DrawFateTooltip(FateTipFacts facts,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(facts.Icon!=0){DrawGameIcon(facts.Icon,new Vector2(32));ImGui.SameLine();}
            ImGui.BeginGroup();
            TooltipTitle(facts.Name);
            var level=facts.Level is { } value ? localization.Format("MapView.MonsterTipLevel",value):null;
            if(level is not null || facts.Kind is not null)ImGui.TextDisabled(string.Join(" · ",new[]{facts.Kind,level}.Where(part=>part is not null)));
            ImGui.EndGroup();
            if(facts.Place is { } place)ImGui.TextUnformatted(place);
            if(facts.Now is { } now)ImGui.TextColored(Success,now);
            if(facts.Description is { } description)ImGui.TextUnformatted(description);
            if(facts.Participants>0)ImGui.TextDisabled(localization.Format("FieldContent.Participants",facts.Participants));
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }
}
