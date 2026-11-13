using Dalamud.Bindings.ImGui;
using TenshoAssistant.Character;

namespace TenshoAssistant;

// X-218 (0.1228, user's request of 12/11, K-32): Storage › Characters shows each character's job levels and gear sets, copied at login and kept
// in TenshoData.db (CharacterJobsData). Read off the frame when the page shows and after each save; the « Jobs » cell selects the character
// whose details are drawn under the table.
internal sealed partial class MainWindow
{
    internal CharacterJobsData? CharacterJobs { get; set; }
    private Task<Dictionary<ulong,Character.CharacterJobs>>? characterJobsLoading;
    private Dictionary<ulong,Character.CharacterJobs> characterJobs=[];
    private volatile bool characterJobsStale=true;
    private bool characterJobsFailed;
    private ulong characterJobsShown;

    /// <summary>A reading was saved: the page reads the data file again the next time it shows.</summary>
    internal void CharacterJobsChanged()=>characterJobsStale=true;

    private void RefreshCharacterJobs()
    {
        if(characterJobsLoading is { IsCompleted:true } done)
        {
            characterJobsLoading=null;
            if(done.IsCompletedSuccessfully){characterJobs=done.Result;characterJobsFailed=false;}
            else characterJobsFailed=true;
        }
        if(characterJobsLoading is null && characterJobsStale && CharacterJobs is { } data)
        {
            characterJobsStale=false;
            characterJobsLoading=Task.Run(data.Load);
        }
    }

    private void DrawCharacterJobsCell(ulong character)
    {
        if(!characterJobs.TryGetValue(character,out var jobs)){ImGui.TextDisabled("—");return;}
        var label=localization.Format("CharactersUi.JobsCell",jobs.Levels.Count,jobs.Levels.Values.Max(),jobs.Gearsets.Count);
        if(ImGui.Selectable(label+"###jobs",characterJobsShown==character))characterJobsShown=characterJobsShown==character ? 0:character;
        Tip("CharactersUi.JobsCellTip");
    }

    private void DrawCharacterJobsDetails(IReadOnlyList<CharacterSummary> summaries)
    {
        if(characterJobsFailed)ImGui.TextDisabled(localization.Get("CharactersUi.JobsUnreadable"));
        if(characterJobsShown==0 || !characterJobs.TryGetValue(characterJobsShown,out var jobs))return;
        var name=summaries.FirstOrDefault(summary=>summary.Id==characterJobsShown)?.Name ?? $"…{characterJobsShown%10000:0000}";
        ImGui.Separator();
        ImGui.TextColored(Accent,localization.Format("CharactersUi.JobsTitle",name,jobs.SeenAt.ToLocalTime().ToString("g")));
        const int pairs=4;
        if(ImGui.BeginTable("CharacterJobLevels",pairs*2,ImGuiTableFlags.SizingFixedFit|ImGuiTableFlags.RowBg))
        {
            try
            {
                var index=0;
                foreach(var (job,level) in jobs.Levels.OrderByDescending(pair=>pair.Value).ThenBy(pair=>JobName(pair.Key),StringComparer.CurrentCultureIgnoreCase))
                {
                    if(index++%pairs==0)ImGui.TableNextRow();
                    ImGui.TableNextColumn();ImGui.TextUnformatted(JobName(job));
                    ImGui.TableNextColumn();ImGui.TextUnformatted(level.ToString(Numbers));
                }
            }
            finally{ImGui.EndTable();}
        }
        if(jobs.Gearsets.Count==0){ImGui.TextDisabled(localization.Get("CharactersUi.NoGearsets"));return;}
        ImGui.TextDisabled(localization.Format("CharactersUi.GearsetsTitle",jobs.Gearsets.Count));
        foreach(var set in jobs.Gearsets)
            ImGui.TextUnformatted(localization.Format("CharactersUi.GearsetLine",set.Number,set.Name.Length>0 ? set.Name:JobName(set.Job),JobName(set.Job),set.ItemLevel));
    }
}
