using Dalamud.Bindings.ImGui;
using TenshoAssistant.Collections;

namespace TenshoAssistant;

// g, X-03 (0.369): titles — wear the title of the details window, a random obtained title or none, and « title by job » (off by
// default): the title set for a job is worn when the character changes to that job (Collections/TitleChanger).
internal sealed partial class MainWindow
{
    internal TitleChanger? TitleChangerSource { get; set; }

    private void DrawTitleActions(CollectionEntry entry)
    {
        if(TitleChangerSource is not { } changer || !entry.ConfirmedUnlocked || entry.Id>ushort.MaxValue)return;
        if(ImGui.SmallButton(localization.Get("Titles.Wear")+$"##Wear{entry.Id}"))changer.Wear((ushort)entry.Id);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Titles.WearTip"));
        var job=changer.CurrentJob;
        if(job==0)return;
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Format("Titles.ForJob",JobName(job))+$"##ForJob{entry.Id}"))SetJobTitle(job,(ushort)entry.Id);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Titles.ForJobTip"));
    }

    private void SetJobTitle(uint job,ushort? title)
    {
        var map=configuration.JobTitles is { } known ? new Dictionary<uint,ushort>(known):[];
        if(title is { } chosen)map[job]=chosen;else map.Remove(job);
        SaveSetting("Titles.ByJob",()=>configuration.JobTitles,value=>configuration.JobTitles=value,map.Count==0 ? null:map);
    }

    // Above the Titles tab.
    private void DrawTitleTools(CollectionState state)
    {
        if(TitleChangerSource is not { } changer)return;
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("Titles.Tools")+"###TitleTools"))return;
        var obtained=state.Entries.Where(entry=>entry.Kind==CollectionKind.Title && entry.ConfirmedUnlocked && entry.Id is > 0 and <= ushort.MaxValue).ToArray();
        ImGui.BeginDisabled(obtained.Length==0);
        try
        {
            if(ImGui.SmallButton(localization.Get("Titles.Random")))changer.Wear((ushort)obtained[Random.Shared.Next(obtained.Length)].Id);
        }
        finally{ImGui.EndDisabled();}
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get("Titles.RandomTip"));
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("Titles.None")))changer.Wear(0);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Get("Titles.NoneTip"));
        BoolSetting("Titles.ByJob","Titles.ByJobTip",()=>configuration.JobTitlesEnabled,value=>configuration.JobTitlesEnabled=value,c=>c.JobTitlesEnabled);
        if(configuration.JobTitles is not { Count:>0 } map){ImGui.TextDisabled(localization.Get("Titles.ByJobEmpty"));return;}
        foreach(var (job,title) in map.OrderBy(pair=>pair.Key).ToArray())
        {
            var name=state.Entries.FirstOrDefault(entry=>entry.Kind==CollectionKind.Title && entry.Id==title)?.Name ?? $"#{title}";
            if(ImGui.SmallButton(localization.Get("Titles.Remove")+$"##JobTitle{job}"))SetJobTitle(job,null);
            ImGui.SameLine();ImGui.TextUnformatted($"{JobName(job)} → {name}");
        }
    }
}
