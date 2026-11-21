using System.Numerics;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>
    /// X-684 (0.1692, the user's request of 20/11: « every tooltip gives everything Tensho knows »): the one tooltip of a recipe, wherever it is
    /// named (the craft book's list, the crafts of a list panel, the crafts from stock): crafter's icon, item made, crafter and level, the
    /// recipe's numbers, minimum attributes, what it allows and asks, crafted before or not, ingredients, then what the caller adds.
    /// </summary>
    private void DrawRecipeTooltip(RecipeTipFacts facts,string? after=null)
    {
        using(TooltipScope.Begin())
        {
            if(facts.Icon!=0){DrawGameIcon(facts.Icon,new Vector2(32));ImGui.SameLine();}
            TooltipTitle(facts.Name);
            if(facts.Job is { } job)ImGui.TextDisabled(job);
            foreach(var line in facts.Lines)ImGui.TextUnformatted(line);
            if(after is { Length:>0 }){ImGui.Separator();ImGui.TextDisabled(after);}
        }
    }

    /// <summary>0.600 (C14), X-684 (0.1692): HQ, quick synthesis, master book, specialist, expert, required item and status, as one line (the
    /// recipe's readiness line and its tooltip).</summary>
    private string RecipeFactsText(CraftRecipeParameters p)
        =>string.Join(" · ",RecipeFacts.Of(p,id=>itemCatalog.State.Catalog?.Find(id)?.Name).Select(fact=>fact.Key=="QuickFrom"
            ? localization.Format("RecipeFacts.QuickFrom",fact.Value.Split('|')[0],fact.Value.Split('|')[1])
            : localization.Format("RecipeFacts."+fact.Key,fact.Value)));

    // X-684 (0.1692): the facts of the recipe under the cursor, built once while it stays hovered (the recipe, the language, the item
    // catalogue, the crafting log's state), not every frame.
    private (uint Recipe,int Revision,object? Items,bool? Crafted,RecipeTipFacts Facts)? recipeTip;
    private RecipeTipFacts RecipeTipOf(RecipeDefinition recipe)
    {
        var items=itemCatalog.State.Catalog;
        RecipeLog?.Want();
        var crafted=RecipeLog?.State?.WasCrafted(recipe.RecipeId);
        if(recipeTip is { } cached && cached.Recipe==recipe.RecipeId && cached.Revision==localization.Revision && ReferenceEquals(cached.Items,items) && cached.Crafted==crafted)
            return cached.Facts;
        string Name(uint id)=>items?.Find(id)?.Name ?? $"#{id}";
        var name=Name(recipe.ResultItemId)+(recipe.ResultAmount>1 ? " × "+recipe.ResultAmount.ToString("N0",Numbers):"");
        var lines=new List<string?>();
        if(recipe.Simulation is { } p)
        {
            lines.Add(string.Join(" · ",RecipeTips.Stats(p).Select(stat=>localization.Get("CraftSequence."+stat.Key)+" "+stat.Value.ToString("N0",Numbers))));
            var minimums=RecipeTips.Minimums(p).Select(stat=>localization.Get("CraftSimulation."+stat.Key)+" "+stat.Value.ToString("N0",Numbers)).ToList();
            if(minimums.Count>0)lines.Add(localization.Format("RecipeTip.Minimum",string.Join(" · ",minimums)));
            lines.Add(RecipeFactsText(p));
        }
        if(crafted is { } done)lines.Add(localization.Get(done ? "RecipeUi.Crafted":"RecipeUi.NeverCrafted"));
        var ingredients=Npcs.NpcTips.Lines(recipe.Ingredients.Select(ingredient=>Name(ingredient.ItemId)+" × "+ingredient.Amount.ToString("N0",Numbers)),
            RecipeTips.MaxIngredients,more=>localization.Format("MapView.HoverMore",more));
        if(ingredients.Count>0)lines.Add(localization.Format("RecipeTip.Ingredients",string.Join(", ",ingredients)));
        var facts=RecipeTips.Build(name,recipe.CraftTypeId<8 ? 62108+recipe.CraftTypeId:0,localization.Format("RecipeUi.JobLevel",recipe.CraftType,recipe.Level),lines);
        recipeTip=(recipe.RecipeId,localization.Revision,items,crafted,facts);
        return facts;
    }
}
