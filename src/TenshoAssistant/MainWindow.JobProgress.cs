using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// T21 (0.646): every job's level and experience, on the Gear page (the Character page was removed in 0.341). Copied by GameGearsetReader.
internal sealed partial class MainWindow
{
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
            foreach(var row in rows)
            {
                ImGui.TableNextRow();ImGui.TableNextColumn();
                if(!row.Unlocked)
                {
                    ImGui.TextDisabled(JobName(row.Job));ImGui.TableNextColumn();ImGui.TableNextColumn();ImGui.TextDisabled(localization.Get("GameGearsets.JobsLocked"));
                    continue;
                }
                ImGui.TextUnformatted(JobName(row.Job));ImGui.TableNextColumn();ImGui.TextUnformatted(row.Level.ToString());ImGui.TableNextColumn();
                if(row.Needed<=0)ImGui.TextDisabled(localization.Get("GameGearsets.JobsMax"));
                else ImGui.ProgressBar(row.Fraction,new Vector2(-1,0),$"{(row.Experience).ToString("N0",Numbers)} / {(row.Needed).ToString("N0",Numbers)}");
            }
        }
        finally{ImGui.EndTable();}
    }
}
