using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Duties;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1691, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a duty, wherever it is
    /// named (the Duties page, the deep dungeons' floors, a monster's duty): icon, name, kind and level, zone, progress, the unlocking quest,
    /// the ids in developer mode, then what the caller adds (a click's effect). Before, only the ids, in developer mode.
    /// </summary>
    private void DrawDutyTooltip(DutyTipFacts facts,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(facts.Icon!=0){DrawGameIcon(facts.Icon,new Vector2(32));ImGui.SameLine();}
            TooltipTitle(facts.Name);
            if(TipPart("Duty.Kind") && facts.Kind is { } kind)ImGui.TextDisabled(kind);
            if(TipPart("Duty.Zone") && facts.Zone is { } zone)ImGui.TextUnformatted(zone);
            if(TipPart("Duty.Status") && facts.Status is { } status)ImGui.TextUnformatted(status);
            if(TipPart("Duty.Unlock") && facts.Unlock is { } unlock)ImGui.TextUnformatted(unlock);
            if(facts.Developer is { } ids)ImGui.TextDisabled(ids);
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }

    // X-684 (0.1691): the facts of the duty under the cursor, built once while it stays hovered (its id, its progress, the language, the
    // developer mode), not every frame.
    private (uint Duty,DutyStatus? Status,int Revision,bool Developer,DutyTipFacts Facts)? dutyTip;
    private DutyTipFacts DutyTipOf(DutyDefinition duty)
    {
        var status=DutyProgressSource?.Invoke()?.Of(duty.InstanceId);
        if(dutyTip is { } cached && cached.Duty==duty.DutyId && cached.Status==status && cached.Revision==localization.Revision && cached.Developer==developerMode)
            return cached.Facts;
        var kind=localization.Format("DutyUi.TipKind",duty.Category,duty.Level);
        var progress=status switch
        {
            DutyStatus.Completed=>localization.Get("DutyProgress.Completed"),
            DutyStatus.Unlocked=>localization.Get("DutyProgress.Unlocked"),
            DutyStatus.Locked=>localization.Get("DutyProgress.Locked"),
            _=>null,
        };
        var unlock=duty.UnlockQuest!=0 && QuestOf(duty.UnlockQuest) is { Name.Length:>0 } quest
            ? localization.Get("DutyUi.UnlockedBy")+" "+quest.Name+" · "+QuestProgressLabel(quest):null;
        var facts=DutyTips.Build(duty.Name,duty.Icon,kind,AlarmZoneName(duty.TerritoryId),progress,unlock,developerMode ? DutyTips.Ids(duty):null);
        dutyTip=(duty.DutyId,status,localization.Revision,developerMode,facts);
        return facts;
    }
}
