using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// 0.408 (user request): the item sheet's Craft tab keeps a short view (recipe, ingredients and stocks, links); the full preparation
// below is the Crafting page's « Preparation » tab, linked both ways.
// Crafting › Preparation: crafts needed for a target quantity and the direct ingredients against recorded stocks (live inventory
// and saved Bank; unknown shown as unknown), copy as text; buttons to the Crafting Log panel and the detailed simulator, then the
// preparation cost (MainWindow.PreparationCost.cs) and the full chain (MainWindow.CraftChain.cs). Built in Draw from cached holdings
// (Craft/CraftPlan.cs, direct ingredients only).
internal sealed partial class MainWindow
{
    private void DrawCraftLinks(RecipeDefinition recipe)
    {
        if(ImGui.SmallButton(localization.Get("PlanUi.Simulate"))) OpenCraftLog(recipe);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("PlanUi.Detailed"))) OpenCraftSimulation(recipe);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("PlanUi.OpenPreparation"))) OpenCraftPreparation(recipe);
        Tip("PlanUi.OpenPreparationTip");
        if(craftLogOpenError is { } openError)ImGui.TextWrapped(openError);
        if(craftLog.Error is { } nativeError)DrawDataError(nativeError,"ServiceErrors.CraftLog");
    }

    /// <summary>0.559 (C19): where a material is, from the rows already computed: nothing is read again.</summary>
    private void DrawStorageDetail(Inventory.ItemHoldingsSummary holdings)
    {
        var lines=holdings.Rows.Where(row=>row.Nq+row.Hq>0).Select(row=>row.IsCached && row.ObservedAt is { } seen
            ? localization.Format("PlanUi.StoredCached",Inventory.StorageNames.Of(localization,row),(row.Nq+row.Hq).ToString("N0",Numbers),seen.ToLocalTime().ToString("d"))
            : localization.Format("PlanUi.Stored",Inventory.StorageNames.Of(localization,row),(row.Nq+row.Hq).ToString("N0",Numbers))).ToArray();
        WrappedTooltip(lines.Length>0 ? string.Join("\n",lines):localization.Get("PlanUi.StoredNone"));
    }
    private void OpenCraftPreparation(RecipeDefinition recipe)
    {
        OpenCraftSimulation(recipe);
        focusCraftPage=CraftPageTab.Preparation;
    }

    // An item named in the preparation opens its sheet in Items, on its recipes when it has some.
    private void OpenCraftItem(uint id)
    {
        SelectItem(id);
        page=Page.Items;
        focusCraftTab=recipeCatalog.State.Catalog?.ForResult(id).Count>0;
    }

    private void DrawCraftPlan(RecipeDefinition recipe)
    {
        // 0.424 (user report « recorded: unknown »): the holdings cache was refreshed only by the Items views; opened on its own, the
        // Crafting page read an empty cache and kept it.
        RefreshRecipeHoldingsCache();
        if(ImGui.SmallButton(localization.Get("PlanUi.Simulate"))) OpenCraftLog(recipe);
        if(craftLogOpenError is { } openError)ImGui.TextWrapped(openError);
        if(craftLog.Error is { } nativeError)DrawDataError(nativeError,"ServiceErrors.CraftLog");
        ImGui.SetNextItemWidth(150);
        ImGui.InputInt(localization.Get("PlanUi.Target")+"##CraftPlanTarget", ref craftPlanTarget);
        Tip("PlanUi.TargetTip");
        craftPlanTarget = Math.Clamp(craftPlanTarget, 1, 999999);
        var plan = CraftPlan.Build(recipe, craftPlanTarget, GetRecipeHoldings);
        // W-05 (0.691): the on-screen list follows the recipe and the target shown here.
        BoolSetting("PlanUi.Panel", "PlanUi.PanelTip", () => configuration.PreparationPanel, value => configuration.PreparationPanel = value, c => c.PreparationPanel);
        if (configuration.PreparationPanel) DrawListPanelOptions();
        if (configuration.PreparationPanel && (configuration.PreparationPanelRecipe != recipe.RecipeId || configuration.PreparationPanelTarget != craftPlanTarget))
        {
            configuration.PreparationPanelItem = recipe.ResultItemId; configuration.PreparationPanelRecipe = recipe.RecipeId; configuration.PreparationPanelTarget = craftPlanTarget;
            SaveReminders();
        }
        ImGui.TextColored(Accent, localization.Format("PlanUi.Summary",plan.Crafts.ToString("N0",Numbers),plan.Produced.ToString("N0",Numbers)));
        if (plan.Produced > plan.Requested) ImGui.TextDisabled(localization.Format("PlanUi.Surplus",(plan.Produced-plan.Requested).ToString("N0",Numbers)));
        if (plan.Materials.Count == 0)
        {
            ImGui.TextDisabled(localization.Get("PlanUi.NoIngredients"));
            return;
        }
        if (ImGui.BeginTable("CraftPlanMaterials", 4, ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
        {
            try
            {
                ImGui.TableSetupColumn(localization.Get("PlanUi.Ingredient"), ImGuiTableColumnFlags.WidthStretch, 2);
                ImGui.TableSetupColumn(localization.Get("PlanUi.Required"));
                ImGui.TableSetupColumn(localization.Get("PlanUi.Recorded"));
                ImGui.TableSetupColumn(localization.Get("PlanUi.Missing"));
                ImGui.TableHeadersRow();
                foreach (var material in plan.Materials)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.PushID((int)material.ItemId);
                    try
                    {
                        var item = itemCatalog.State.Catalog?.Find(material.ItemId);
                        DrawItemIcon(item, new Vector2(24));
                        var clicked = ImGui.IsItemClicked();
                        ImGui.SameLine();
                        if (ImGui.SmallButton(item?.Name ?? $"ID {material.ItemId}") || clicked) OpenCraftItem(material.ItemId);
                        ImGui.SameLine();
                        if (ImGui.SmallButton(localization.Get("PlanUi.Locations"))) OpenGathering(material.ItemId);
                    }
                    finally { ImGui.PopID(); }
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(material.Required.ToString("N0",Numbers));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(material.Holdings.HasObservation ? material.Holdings.Total.ToString("N0",Numbers) : localization.Get("PlanUi.Unknown"));
                    if (ImGui.IsItemHovered()) DrawStorageDetail(material.Holdings);
                    ImGui.TableNextColumn();
                    if (!material.Holdings.HasObservation) ImGui.TextDisabled(localization.Get("PlanUi.Verify"));
                    else ImGui.TextColored(material.Uncovered == 0 ? Success : Accent, material.Uncovered.ToString("N0",Numbers));
                }
            }
            finally { ImGui.EndTable(); }
        }
        ImGui.TextWrapped(localization.Get("PlanUi.Help"));
        DrawPreparationCost(recipe,plan);
        DrawCraftChain(recipe);
        if (plan.Materials.Any(material => !material.Holdings.HasObservation || material.Holdings.HasUnknown))
            ImGui.TextWrapped(localization.Get("PlanUi.UnknownStock"));
        if (plan.Materials.Any(material => material.Holdings.HasCached))
            ImGui.TextDisabled(localization.Get("PlanUi.Imported"));
        // A-05 (0.433): the target into the chosen crafting list.
        DrawAddToCraftList(recipe,plan.Requested);
        // A-09 (0.423): the target as a Teamcraft list (import link built here; copied, or opened on click).
        if (TeamcraftExport.Link([(recipe.ResultItemId, recipe.RecipeId, (long)plan.Requested)]) is { } teamcraft)
        {
            CopyButton(localization.Get("PlanUi.TeamcraftCopy"),()=>teamcraft);
            Tip("PlanUi.TeamcraftTip");
            ImGui.SameLine();
            if (ImGui.Button(localization.Get("PlanUi.TeamcraftOpen"))) Dalamud.Utility.Util.OpenLink(teamcraft);
            Tip("PlanUi.TeamcraftTip");
            ImGui.SameLine();
        }
        if (ImGui.Button(localization.Get("PlanUi.Copy")))
        {
            var text = new StringBuilder();
            var name = itemCatalog.State.Catalog?.Find(recipe.ResultItemId)?.Name ?? $"ID {recipe.ResultItemId}";
            text.AppendLine(localization.Format("PlanUi.CopyHeader",name,plan.Requested,plan.Crafts,plan.Produced));
            foreach (var material in plan.Materials)
            {
                var ingredient = itemCatalog.State.Catalog?.Find(material.ItemId)?.Name ?? $"ID {material.ItemId}";
                text.AppendLine(localization.Format("PlanUi.CopyIngredient",ingredient,material.Required)
                    + (material.Holdings.HasObservation ? localization.Format("PlanUi.CopyStock",material.Holdings.Total,material.Uncovered) : localization.Get("PlanUi.UnknownLower"))
                    + (material.Holdings.HasCached || material.Holdings.HasUnknown ? localization.Get("PlanUi.VerifySuffix") : ""));
            }
            ImGui.SetClipboardText(text.ToString());
        }
    }
}
