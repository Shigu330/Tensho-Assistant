using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// T21 (0.646): every job's level and experience, on the Gear page (the Character page was removed in 0.341). Copied by GameGearsetReader.
internal sealed partial class MainWindow
{
    /// <summary>X-221 (0.1760): the Graces of the character logged in (GraceWatcher); null or empty: none.</summary>
    internal Func<IReadOnlyList<GraceShown>?>? GraceSource { get; set; }

    private void DrawJobProgress()
    {
        if(GearsetSource is not { } source)return;
        source.Want();
        var rows=source.State?.Jobs;
        var title=localization.Format("GameGearsets.JobsTitle",rows?.Count(row=>row.Unlocked) ?? 0,rows?.Count ?? 0);
        if(!TenshoAssistant.UI.UiComponents.Header(title+"###GameJobs"))return;
        if(rows is null){ImGui.TextDisabled(localization.Get(source.Unavailable ? "GameGearsets.Unavailable":"GameGearsets.Reading"));return;}
        if(!ImGui.BeginTable("GameJobsTable",3,ImGuiTableFlags.RowBg|ImGuiTableFlags.SizingStretchProp))return;
        try
        {
            // X-221 (0.1760): a manual's Grace under the last job it boosts (crafters, gatherers).
            var graces=GraceSource?.Invoke() ?? [];
            for(var index=0;index<rows.Count;index++)
            {
                var row=rows[index];
                ImGui.TableNextRow();ImGui.TableNextColumn();
                if(!row.Unlocked)
                {
                    ImGui.TextDisabled(JobName(row.Job));ImGui.TableNextColumn();ImGui.TableNextColumn();ImGui.TextDisabled(localization.Get("GameGearsets.JobsLocked"));
                }
                else
                {
                    ImGui.TextUnformatted(JobName(row.Job));ImGui.TableNextColumn();ImGui.TextUnformatted(row.Level.ToString());ImGui.TableNextColumn();
                    if(row.Needed<=0)ImGui.TextDisabled(localization.Get("GameGearsets.JobsMax"));
                    else ImGui.ProgressBar(row.Fraction,new Vector2(-1,0),$"{(row.Experience).ToString("N0",Numbers)} / {(row.Needed).ToString("N0",Numbers)}");
                }
                var boosted=ManualGrace.StatusFor(row.Job);
                if(boosted==0 || index+1<rows.Count && ManualGrace.StatusFor(rows[index+1].Job)==boosted)continue;
                foreach(var grace in graces)
                    if(grace.Status==boosted)DrawGrace(grace);
            }
        }
        finally{ImGui.EndTable();}
    }

    // X-221 (0.1760): « Crafter's Grace: about N bonus points left », or unknown when Tensho did not see the manual's start.
    private void DrawGrace(GraceShown grace)
    {
        ImGui.TableNextRow();ImGui.TableNextColumn();
        ImGui.TextUnformatted(grace.Name);
        ImGui.TableNextColumn();ImGui.TableNextColumn();
        if(grace.Left is { } left)
        {
            ImGui.TextUnformatted(localization.Format("GameGearsets.GraceLeft",left.ToString("N0",Numbers)));
            if(TipHovered())TipText(localization.Format("GameGearsets.GraceTip",grace.Param.ToString(Numbers),(grace.Cap ?? 0).ToString("N0",Numbers)));
        }
        else
        {
            ImGui.TextDisabled(localization.Get("GameGearsets.GraceUnknown"));
            Tip(grace.Cap is null ? "GameGearsets.GraceManualTip":"GameGearsets.GraceUnknownTip");
        }
    }
}
