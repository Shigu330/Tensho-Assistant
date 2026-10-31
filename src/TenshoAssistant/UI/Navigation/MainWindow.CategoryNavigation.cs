using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void CategoryLink(string key, Page target)
    {
        var label=localization.Get(key);
        if(ImGui.GetContentRegionAvail().X < ImGui.CalcTextSize(label).X+ImGui.GetStyle().FramePadding.X*2)
            ImGui.NewLine();
        if(TenshoAssistant.UI.UiComponents.CategoryItem(label+"###Category"+target,page==target))page=target;
        ImGui.SameLine();
    }

    private void DrawCategoryNavigation()
    {
        switch(page)
        {
            case Page.CraftSimulation:
            case Page.CraftPresets:
            case Page.CraftLive:
                if(ImGui.SmallButton(localization.Get("CraftSimulation.Notebook")+"###CategoryRecipes")) {page=Page.Items;focusCraftTab=true;}
                ImGui.SameLine();
                CategoryLink("CraftSimulation.Title",Page.CraftSimulation);
                CategoryLink("CraftPresets.Browse",Page.CraftPresets);
                CategoryLink("CraftSimulation.Live",Page.CraftLive);
                break;
            case Page.Gathering:
            case Page.GatheringGp:
                CategoryLink("Navigation.Gathering",Page.Gathering);
                CategoryLink("Category.GatheringGp",Page.GatheringGp);
                break;
            case Page.Collections:
            case Page.BlueMagic:
                CategoryLink("Navigation.Collection",Page.Collections);
                CategoryLink("Category.BlueMagic",Page.BlueMagic);
                break;
            case Page.Maps:
            case Page.Weather:
            case Page.DutyLoot:
            case Page.MonsterLoot:
            case Page.NpcVendors:
                CategoryLink("Navigation.Map",Page.Maps);
                CategoryLink("Category.Weather",Page.Weather);
                CategoryLink("Category.DutyLoot",Page.DutyLoot);
                CategoryLink("Category.MonsterLoot",Page.MonsterLoot);
                CategoryLink("Category.NpcVendors",Page.NpcVendors);
                break;
            case Page.MacroChain:
            case Page.SortPreview:
                CategoryLink("Category.MacroChain",Page.MacroChain);
                CategoryLink("Category.Sorting",Page.SortPreview);
                break;
            case Page.Fashion:
            case Page.MiniCactpot:
            case Page.TripleTriad:
                CategoryLink("Category.Fashion",Page.Fashion);
                CategoryLink("Category.Cactpot",Page.MiniCactpot);
                CategoryLink("Category.Triad",Page.TripleTriad);
                break;
            default:return;
        }
        ImGui.NewLine();
        ImGui.Separator();
    }
}
