using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawCraftPlan(RecipeDefinition recipe)
    {
        if(ImGui.SmallButton(localization.Get("PlanUi.Simulate"))) OpenCraftLog(recipe);
        ImGui.SameLine();
        if(ImGui.SmallButton(localization.Get("PlanUi.Detailed"))) OpenCraftSimulation(recipe);
        if(craftLogOpenError is { } openError)ImGui.TextWrapped(openError);
        if(craftLog.Error is { } nativeError)DrawDataError(nativeError,"ServiceErrors.CraftLog");
        if (!ImGui.CollapsingHeader(localization.Get("PlanUi.Prepare")+"##CraftPlan", ImGuiTreeNodeFlags.DefaultOpen)) return;
        ImGui.SetNextItemWidth(150);
        ImGui.InputInt(localization.Get("PlanUi.Target")+"##CraftPlanTarget", ref craftPlanTarget);
        craftPlanTarget = Math.Clamp(craftPlanTarget, 1, 999999);
        var plan = CraftPlan.Build(recipe, craftPlanTarget, GetRecipeHoldings);
        ImGui.TextColored(Accent, localization.Format("PlanUi.Summary",plan.Crafts.ToString("N0"),plan.Produced.ToString("N0")));
        if (plan.Produced > plan.Requested) ImGui.TextDisabled(localization.Format("PlanUi.Surplus",(plan.Produced-plan.Requested).ToString("N0")));
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
                        if (ImGui.SmallButton(item?.Name ?? $"ID {material.ItemId}") || clicked) SelectItem(material.ItemId);
                        ImGui.SameLine();
                        if (ImGui.SmallButton(localization.Get("PlanUi.Locations"))) OpenGathering(material.ItemId);
                    }
                    finally { ImGui.PopID(); }
                    ImGui.TableNextColumn(); ImGui.TextUnformatted(material.Required.ToString("N0"));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(material.Holdings.HasObservation ? material.Holdings.Total.ToString("N0") : localization.Get("PlanUi.Unknown"));
                    ImGui.TableNextColumn();
                    if (!material.Holdings.HasObservation) ImGui.TextDisabled(localization.Get("PlanUi.Verify"));
                    else ImGui.TextColored(material.Uncovered == 0 ? Success : Accent, material.Uncovered.ToString("N0"));
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
