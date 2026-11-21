using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1674, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of an action or a status:
    /// its game icon, its name, a detail line (level, CP, durability for a crafting action), the game's description, then what the caller
    /// adds (a step's number, why it is greyed out, what was detected). Everything read from the catalogue, nothing worked out under the mouse.
    /// </summary>
    private void DrawActionTooltip(uint icon,string name,string? description,string? detail=null,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(icon!=0){DrawGameIcon(icon,new Vector2(32));ImGui.SameLine();}
            ImGui.BeginGroup();
            ImGui.TextColored(Accent,name);
            if(detail is { Length:>0 })ImGui.TextDisabled(detail);
            ImGui.EndGroup();
            if(description is { Length:>0 })ImGui.TextUnformatted(description);
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }

    /// <summary>X-684 (0.1674): a crafting action (Crafting simulator, the craft log's steps, the recorder, the projection, the Manipulation box).</summary>
    private void DrawCraftActionTooltip(CraftActionMetadata action,string? title=null,string? after=null)
        =>DrawActionTooltip(action.Icon,title ?? action.Name,action.Description,CraftActionTips.Detail(action.Spec,localization.Get("CraftLogUi.ActionTipDetail"),localization.Get("CraftLogUi.ActionTipDurability")),after);
}
