using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// Item sheet › Craft › preparation: the full intermediate-craft chain for the target quantity, computed on « Calculate » on a worker
// (Craft/CraftChainSession, results of another context rejected or marked stale): raw materials still missing after the live inventory
// and the saved Bank, a recipe choice for items with several recipes, links to gathering and prices, copy as text. Recipes from the game's
// Recipe sheet (RecipeCatalog); unknown or imported stocks are flagged, never taken as zero.
internal sealed partial class MainWindow
{
    private readonly CraftChainSession craftChain = new();
    private readonly Dictionary<uint, uint> craftChainChoices = [];
    private int craftChainChoiceRevision;
    // 0.591 (C18): the recipes ticked as done, for this calculation only: a new one starts from the stocks as they are.
    private readonly HashSet<uint> craftChainDone = [];
    private (CraftChainResult Source,IReadOnlyList<CraftChainStep> Steps)? craftChainMerged;
    private IReadOnlyList<CraftChainStep> MergedChainSteps(CraftChainResult result)
    {
        if (craftChainMerged is { } known && ReferenceEquals(known.Source, result)) return known.Steps;
        var steps = CraftChain.Merged(result.Steps);
        craftChainMerged = (result, steps);
        return steps;
    }

    private void DrawCraftChain(RecipeDefinition recipe)
    {
        if (recipeCatalog.State.Catalog is not { } catalog) return;
        var character = bankBridge.State.CharacterId;
        craftChain.ValidateContext(character, recipe.ResultItemId, craftPlanTarget, catalog);
        var state = craftChain.State;
        if (ImGui.Button(localization.Get("ChainUi.Calculate")))
        {
            if(preparationPrices.State.Context?.IsChain==true) preparationPrices.Cancel();
            craftChainDone.Clear();
            _ = craftChain.StartAsync(new(character, recipe.ResultItemId, recipe, craftPlanTarget, catalog, inventory.Current, bank.State,
                craftChainChoices, craftChainChoiceRevision, id => ClassifyCrystal(id) is not null));
        }
        if (craftChainChoices.Count > 0)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton(localization.Get("ChainUi.Reset")))
            { craftChainChoices.Clear(); craftChainChoiceRevision++; craftChain.Cancel(); if(preparationPrices.State.Context?.IsChain==true) preparationPrices.Cancel(); return; }
        }
        if (state.Input?.Recipe.RecipeId != recipe.RecipeId) return;
        ImGui.SameLine();
        if (ImGui.SmallButton(localization.Get("ChainUi.Clear"))) { craftChain.Cancel(); if(preparationPrices.State.Context?.IsChain==true) preparationPrices.Cancel(); return; }
        if (state.Loading) { ImGui.TextDisabled(localization.Get("ChainUi.Loading")); return; }
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Craft");
        if (state.Result is not { } result) return;
        var stale = !ReferenceEquals(state.Input.Live, inventory.Current) || !ReferenceEquals(state.Input.Bank, bank.State);
        ImGui.TextDisabled(localization.Format("ChainUi.Calculated",state.CalculatedAt?.ToLocalTime().ToString("T") ?? "—",result.Requested.ToString("N0"),result.Produced.ToString("N0")));
        if (stale) ImGui.TextColored(Accent, localization.Get("ChainUi.Stale"));
        if (state.Input.ChoiceRevision != craftChainChoiceRevision)
            ImGui.TextColored(Accent, localization.Get("ChainUi.ChoicesChanged"));
        if (result.HasUnknown) ImGui.TextWrapped(localization.Get("ChainUi.Incomplete"));
        if (result.HasCached) ImGui.TextDisabled(localization.Get("ChainUi.Imported"));
        if (result.HasUnresolved) ImGui.TextColored(Accent, localization.Get("ChainUi.Partial"));
        ImGui.TextWrapped(localization.Get("ChainUi.Help"));
        var merged = MergedChainSteps(result);
        var doneCount = merged.Count(step => craftChainDone.Contains(step.Recipe.RecipeId));
        if (TenshoAssistant.UI.UiComponents.Header(localization.Format("ChainUi.Steps",merged.Count)+(doneCount>0 ? " · "+localization.Format("ChainUi.StepsDone",doneCount,merged.Count):"")+"###ChainSteps"))
        {
            DrawChainList("ChainStepsList", merged.Count, index =>
            {
                var step = merged[index];
                ImGui.PushID($"ChainStep{step.Recipe.RecipeId}");
                try
                {
                    var done = craftChainDone.Contains(step.Recipe.RecipeId);
                    if (ImGui.Checkbox("###ChainStepDone", ref done))
                    {
                        if (done) craftChainDone.Add(step.Recipe.RecipeId);
                        else craftChainDone.Remove(step.Recipe.RecipeId);
                    }
                    Tip("ChainUi.DoneTip");
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Log"))) OpenCraftLog(step.Recipe);
                    if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("ChainUi.LogTip"));
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Simulate"))) OpenCraftSimulation(step.Recipe);
                    if (ImGui.IsItemHovered()) WrappedTooltip(localization.Get("ChainUi.SimulateTip"));
                    ImGui.SameLine();
                    DrawChainItem(step.Recipe.ResultItemId);
                    ImGui.SameLine();
                    ImGui.TextDisabled(localization.Format("ChainUi.Step",step.Crafts.ToString("N0"),step.Produced.ToString("N0"),step.Recipe.CraftType,step.Recipe.Level));
                }
                finally { ImGui.PopID(); }
            });
        }
        if (TenshoAssistant.UI.UiComponents.Header(localization.Format("ChainUi.Materials",result.Materials.Count)+"##ChainMaterials", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (result.Materials.Count == 0) ImGui.TextColored(Success, localization.Get("ChainUi.Covered"));
            DrawChainList("ChainMaterialsList", result.Materials.Count, index =>
            {
                var material = result.Materials[index];
                ImGui.PushID($"ChainMaterial{material.ItemId}/{material.Reason}");
                try
                {
                    DrawChainItem(material.ItemId);
                    ImGui.SameLine(); ImGui.TextColored(material.Reason == CraftChainReason.RawMaterial ? Muted : Accent,
                        $"× {material.Amount:N0} · {ChainReason(material.Reason)}");
                    if (ImGui.IsItemHovered() && result.Observations.TryGetValue(material.ItemId, out var stored)) DrawStorageDetail(stored);
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Locations"))) OpenGathering(material.ItemId);
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Prices"))) { SelectItem(material.ItemId); OpenContextTool("Price Insight"); }

                }
                finally { ImGui.PopID(); }
            });
        }

        // 0.416 (g, R-14): the raw materials by zone.
        DrawChainGatheringRoute(result);
        var choices = result.Materials.Where(material => material.Reason == CraftChainReason.RecipeChoice).ToArray();
        if (choices.Length > 0 && TenshoAssistant.UI.UiComponents.Header(localization.Get("ChainUi.Choose")+"##ChainChoices", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawChainList("ChainChoicesList", choices.Length, index =>
            {
                var material = choices[index];
                ImGui.PushID($"RecipeChoice{material.ItemId}");
                try
                {
                    ImGui.TextUnformatted(itemCatalog.State.Catalog?.Find(material.ItemId)?.Name ?? $"ID {material.ItemId}");
                    var variants = catalog.ForResult(material.ItemId);
                    var chosen = craftChainChoices.TryGetValue(material.ItemId, out var selected)
                        ? variants.FirstOrDefault(variant => variant.RecipeId == selected) : null;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.BeginCombo("##ChainRecipeChoice", chosen is null ? localization.Get("ChainUi.ChooseHint") : ChainVariant(chosen)))
                    {
                        try
                        {
                            foreach (var variant in variants)
                                if (ImGui.Selectable(ChainVariant(variant), variant.RecipeId == chosen?.RecipeId))
                                { craftChainChoices[material.ItemId] = variant.RecipeId; craftChainChoiceRevision++; }
                        }
                        finally { ImGui.EndCombo(); }
                    }
                    Tip("ChainUi.ChoiceTip");
                }
                finally { ImGui.PopID(); }
            });
        }
        DrawChainCost(recipe,state,stale);
        if (ImGui.Button(localization.Get("ChainUi.Copy")))
        {
            string Name(uint id) => itemCatalog.State.Catalog?.Find(id)?.Name ?? $"ID {id}";
            var text = new StringBuilder(localization.Format("ChainUi.CopyHeader",Name(recipe.ResultItemId),result.Requested,result.Produced,state.CalculatedAt?.ToLocalTime().ToString("G") ?? "—"));
            if (stale || result.HasUnknown || result.HasCached) text.AppendLine(localization.Get("ChainUi.StockWarning"));
            if (state.Input.ChoiceRevision != craftChainChoiceRevision) text.AppendLine(localization.Get("ChainUi.ChoicesWarning"));
            if (result.HasUnresolved) text.AppendLine(localization.Get("ChainUi.PartialWarning"));
            foreach (var step in merged) text.AppendLine(localization.Format("ChainUi.CopyStep",step.Crafts,Name(step.Recipe.ResultItemId),step.Produced,step.Recipe.CraftType));
            text.AppendLine(localization.Get("ChainUi.Missing"));
            foreach (var material in result.Materials) text.AppendLine($"{Name(material.ItemId)} × {material.Amount} — {ChainReason(material.Reason)}");
            ImGui.SetClipboardText(text.ToString());
        }
    }

    private void DrawChainItem(uint id)
    {
        var item = itemCatalog.State.Catalog?.Find(id);
        DrawItemIcon(item, new Vector2(24));
        var clicked = ImGui.IsItemClicked();
        ImGui.SameLine();
        if (ImGui.SmallButton(item?.Name ?? $"ID {id}") || clicked) OpenCraftItem(id);
    }
    private static void DrawChainList(string id, int count, Action<int> drawRow)
    {
        if (count == 0) return;
        var rowHeight = 24 + ImGui.GetStyle().ItemSpacing.Y;
        var visible = ImGui.BeginChild(id, new Vector2(0, Math.Clamp(count * rowHeight + 8, 40, 260)), true);
        try
        {
            if (!visible) return;
            var clipper = ImGui.ImGuiListClipper();
            try
            {
                clipper.Begin(count);
                while (clipper.Step())
                    for (var index = clipper.DisplayStart; index < clipper.DisplayEnd; index++) drawRow(index);
            }
            finally { clipper.End(); clipper.Destroy(); }
        }
        finally { ImGui.EndChild(); }
    }
    private string ChainReason(CraftChainReason reason) => reason switch
    {
        CraftChainReason.RawMaterial => localization.Get("ChainUi.Raw"),
        CraftChainReason.RecipeChoice => localization.Get("ChainUi.ChoiceReason"),
        CraftChainReason.Cycle => localization.Get("ChainUi.Cycle"),
        CraftChainReason.DepthLimit => localization.Get("ChainUi.Depth"),
        _ => localization.Get("ChainUi.NoIngredients"),
    };
    private string ChainVariant(RecipeDefinition recipe) => localization.Format("ChainUi.Variant",recipe.CraftType,recipe.Level,recipe.ResultAmount)+(developerMode ? $" · ID {recipe.RecipeId}" : "");
}
