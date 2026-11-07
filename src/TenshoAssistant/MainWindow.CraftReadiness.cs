using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Gear;

namespace TenshoAssistant;

// A-12 (0.427): what the recipe needs against the character — the job's level (game's job levels, read with the gear sets), and, with
// the job worn, Craftsmanship and Control against the recipe's minimum (fresh attributes); « Equip (job) » wears the job's gear set
// with the lowest number, on click only (GameGearsetReader.Equip, checked). Unknown values are said, not guessed.
internal sealed partial class MainWindow
{
    /// <summary>« Equip (job) » for the recipe's job (its gear set with the lowest number), on click only; said when there is none.</summary>
    private void DrawEquipRecipeJob(RecipeDefinition recipe,string id)
    {
        var job=recipe.CraftTypeId+8;
        GearsetSource?.Want();
        if(GearsetSource?.State?.Sets is not { } sets){ImGui.NewLine();return;}
        if(GameGearsets.ForJob(sets,job) is not { } set){ImGui.TextDisabled(localization.Format("CraftReadiness.NoSet",JobName(job)));return;}
        if(ImGui.SmallButton(localization.Format("GatheringDetails.Equip",JobName(job))+"###"+id))GearsetSource!.Equip(set.Number);
        if(ImGui.IsItemHovered())WrappedTooltip(localization.Format("GatheringDetails.EquipTip",set.Number,set.Name.Length>0 ? set.Name:JobName(job)));
    }

    /// <summary>0.600 (C14): HQ, quick synthesis, master book, specialist, required item and status. Whether the book is read is not known here.</summary>
    private void DrawRecipeFacts(RecipeDefinition recipe)
    {
        if(recipe.Simulation is not { } p)return;
        var parts=RecipeFacts.Of(p,id=>itemCatalog.State.Catalog?.Find(id)?.Name).Select(fact=>fact.Key=="QuickFrom"
            ? localization.Format("RecipeFacts.QuickFrom",fact.Value.Split('|')[0],fact.Value.Split('|')[1])
            : localization.Format("RecipeFacts."+fact.Key,fact.Value));
        ImGui.TextDisabled(string.Join(" · ",parts));
    }

    private void DrawCraftReadiness(RecipeDefinition recipe)
    {
        DrawRecipeFacts(recipe);
        if(recipe.Simulation is not { } p)return;
        var job=recipe.CraftTypeId+8;
        GearsetSource?.Want();
        var gearsets=GearsetSource?.State;
        var parts=new List<(string Text,bool? Ok)>();
        var stats=characterStats.State;
        // 0.464 (in-game campaign): the job worn gives its level too: the gear set reader may not have answered yet, and the line stayed grey.
        int? level=gearsets?.JobLevels.TryGetValue(job,out var known)==true ? known:stats.JobId==job && stats.Level>0 ? stats.Level:null;
        parts.Add((localization.Format("CraftReadiness.Level",recipe.Level,level?.ToString() ?? "?"),level is { } have ? have>=recipe.Level:null));
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
        if(!worn)DrawEquipRecipeJob(recipe,"CraftEquip");
        else ImGui.NewLine();
        if(!worn)ImGui.TextDisabled(localization.Get("CraftReadiness.WearJob"));
    }
}
