using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private readonly CraftChainSession craftChain = new();
    private readonly Dictionary<uint, uint> craftChainChoices = [];
    private int craftChainChoiceRevision;

    private void DrawCraftChain(RecipeDefinition recipe)
    {
        if (recipeCatalog.State.Catalog is not { } catalog) return;
        var character = bankBridge.State.CharacterId;
        craftChain.ValidateContext(character, (uint)Math.Max(0, itemId), craftPlanTarget, catalog);
        var state = craftChain.State;
        if (ImGui.Button(localization.Get("ChainUi.Calculate")))
        {
            if(preparationPrices.State.Context?.IsChain==true) preparationPrices.Cancel();
            _ = craftChain.StartAsync(new(character, (uint)Math.Max(0, itemId), recipe, craftPlanTarget, catalog, inventory.Current, bank.State,
                craftChainChoices, craftChainChoiceRevision));
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
        if (ImGui.CollapsingHeader(localization.Format("ChainUi.Steps",result.Steps.Count)+"##ChainSteps"))
        {
            DrawChainList("ChainStepsList", result.Steps.Count, index =>
            {
                var step = result.Steps[index];
                ImGui.PushID($"ChainStep{index}");
                try
                {
                    DrawChainItem(step.Recipe.ResultItemId);
                    ImGui.SameLine();
                    ImGui.TextDisabled(localization.Format("ChainUi.Step",step.Crafts.ToString("N0"),step.Produced.ToString("N0"),step.Recipe.CraftType,step.Recipe.Level));
                }
                finally { ImGui.PopID(); }
            });
        }
        if (ImGui.CollapsingHeader(localization.Format("ChainUi.Materials",result.Materials.Count)+"##ChainMaterials", ImGuiTreeNodeFlags.DefaultOpen))
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
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Locations"))) OpenGathering(material.ItemId);
                    ImGui.SameLine();
                    if (ImGui.SmallButton(localization.Get("ChainUi.Prices"))) { SelectItem(material.ItemId); OpenContextTool("Price Insight"); }

                }
                finally { ImGui.PopID(); }
            });
        }

        var choices = result.Materials.Where(material => material.Reason == CraftChainReason.RecipeChoice).ToArray();
        if (choices.Length > 0 && ImGui.CollapsingHeader(localization.Get("ChainUi.Choose")+"##ChainChoices", ImGuiTreeNodeFlags.DefaultOpen))
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
            foreach (var step in result.Steps) text.AppendLine(localization.Format("ChainUi.CopyStep",step.Crafts,Name(step.Recipe.ResultItemId),step.Produced,step.Recipe.CraftType));
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
        if (ImGui.SmallButton(item?.Name ?? $"ID {id}") || clicked) SelectItem(id);
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
