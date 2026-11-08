using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;

namespace TenshoAssistant;

// g, C-15 (0.368): achievement progress (seen when the game received it, or asked on the player's click) and the tracked achievements:
// « Track » in the achievement window, and the tracked list above the Achievements tab with each one's progress.
internal sealed partial class MainWindow
{
    internal AchievementProgressWatcher? AchievementProgressSource { get; set; }

    private void DrawAchievementProgress(CollectionEntry entry)
    {
        var tracked=configuration.TrackedAchievements?.Contains(entry.Id)==true;
        if(ImGui.Checkbox(localization.Get("AchievementProgress.Track")+$"##Track{entry.Id}",ref tracked))ToggleTracked(entry.Id);
        Tip("AchievementProgress.TrackTip");
        if(entry.ConfirmedUnlocked)return;
        DrawProgressLine(entry.Id);
    }

    private void DrawProgressLine(uint id)
    {
        var seen=AchievementProgressSource?.Progress.GetValueOrDefault(id);
        if(seen is not null)ImGui.TextColored(Accent,localization.Format("AchievementProgress.Line",seen.Current.ToString("N0"),seen.Max.ToString("N0"),seen.SeenAt.ToLocalTime().ToString("t")));
        else ImGui.TextDisabled(localization.Get("AchievementProgress.Unknown"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("AchievementProgress.Ask")+$"##Ask{id}") && AchievementProgressSource?.Request(id)==false)
            saveError=localization.Get("AchievementProgress.Wait");
        Tip("AchievementProgress.AskTip");
    }

    private void ToggleTracked(uint id)
    {
        var list=configuration.TrackedAchievements?.ToList() ?? [];
        if(!list.Remove(id))
        {
            if(list.Count>=100){saveError=localization.Get("AchievementProgress.Full");return;}
            list.Add(id);
        }
        SaveSetting("AchievementProgress.Track",()=>configuration.TrackedAchievements,value=>configuration.TrackedAchievements=value,list.Count==0 ? null:list);
    }

    // Above the Achievements tab: the tracked achievements, not yet done first.
    private void DrawTrackedAchievements(CollectionState state)
    {
        if(configuration.TrackedAchievements is not { Count:>0 } tracked)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Format("AchievementProgress.Tracked",tracked.Count)+"###TrackedAchievements",ImGuiTreeNodeFlags.DefaultOpen))return;
        var entries=tracked.Select(id=>state.Entries.FirstOrDefault(entry=>entry.Kind==CollectionKind.Achievement && entry.Id==id)).OfType<CollectionEntry>()
            .OrderBy(entry=>entry.ConfirmedUnlocked).ToArray();
        foreach(var entry in entries)
        {
            ImGui.PushID((int)entry.Id);
            try
            {
                if(ImGui.SmallButton(localization.Get("AchievementProgress.Untrack")))ToggleTracked(entry.Id);
                ImGui.SameLine();
                if(entry.ConfirmedUnlocked){ImGui.TextColored(Success,localization.Format("AchievementProgress.Done",entry.Name));continue;}
                if(ImGui.Selectable(entry.Name+"##TrackedOpen",false,ImGuiSelectableFlags.None,new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X*0.45f,0)))OpenCompletion(entry);
                ImGui.SameLine();
                DrawProgressLine(entry.Id);
            }
            finally{ImGui.PopID();}
        }
    }
}
