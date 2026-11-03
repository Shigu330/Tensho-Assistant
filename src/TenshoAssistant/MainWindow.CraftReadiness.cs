using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// A-12 (0.427): what the recipe needs against the character — the job's level (game's job levels, read with the gear sets), and, with
// the job worn, Craftsmanship and Control against the recipe's minimum (fresh attributes); « Equip (job) » wears the job's gear set
// with the lowest number, on click only (GameGearsetReader.Equip, checked). Unknown values are said, not guessed.
internal sealed partial class MainWindow
{
    private void DrawCraftReadiness(RecipeDefinition recipe)
    {
        if(recipe.Simulation is not { } p)return;
        var job=recipe.CraftTypeId+8;
        GearsetSource?.Want();
        var gearsets=GearsetSource?.State;
        var parts=new List<(string Text,bool? Ok)>();
        int? level=gearsets?.JobLevels.TryGetValue(job,out var known)==true ? known:null;
        parts.Add((localization.Format("CraftReadiness.Level",recipe.Level,level?.ToString() ?? "?"),level is { } have ? have>=recipe.Level:null));
        var stats=characterStats.State;
        var worn=stats.JobId==job && stats.Values.ContainsKey("Craftsmanship") && stats.Values.ContainsKey("Control");
        if(p.RequiredCraftsmanship>0)parts.Add((localization.Format("CraftReadiness.Craftsmanship",p.RequiredCraftsmanship,worn ? stats.Values["Craftsmanship"].ToString():"?"),
            worn ? stats.Values["Craftsmanship"]>=p.RequiredCraftsmanship:null));
        if(p.RequiredControl>0)parts.Add((localization.Format("CraftReadiness.Control",p.RequiredControl,worn ? stats.Values["Control"].ToString():"?"),
            worn ? stats.Values["Control"]>=p.RequiredControl:null));
        foreach(var (text,ok) in parts)
        {
            ImGui.TextColored(ok switch{true=>UI.FfxivTheme.Success,false=>UI.FfxivTheme.Error,null=>Muted},text);
            ImGui.SameLine();
        }
        if(!worn && gearsets?.Sets is { } sets && GameGearsets.ForJob(sets,job) is { } set)
        {
            if(ImGui.SmallButton(localization.Format("GatheringDetails.Equip",JobName(job))+"###CraftEquip"))GearsetSource!.Equip(set.Number);
            if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("GatheringDetails.EquipTip",set.Number,set.Name.Length>0 ? set.Name:JobName(job)));
        }
        else ImGui.NewLine();
        if(!worn)ImGui.TextDisabled(localization.Get("CraftReadiness.WearJob"));
    }
}
