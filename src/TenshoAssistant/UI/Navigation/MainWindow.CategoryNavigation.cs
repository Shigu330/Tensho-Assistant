using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

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

    // One sidebar category: a header that folds its entries (state kept in the configuration); a folded category holding the
    // current page shows its header highlighted. 0.1038 (night test T-0.912-1): the title takes the theme's text colour, not the muted one,
    // which the thin TrumpGothic strokes made almost invisible in the light theme.
    private bool NavigationCategory(string key,params Page[] pages)
    {
        // 0.1222 (X-257): secondary groups folded until the player chooses (Guide/FirstScreen.cs).
        var collapsed=TenshoAssistant.Guide.FirstScreen.Folded(configuration.NavigationCollapsed).Contains(key);
        ImGui.Spacing();
        var holdsPage=pages.Contains(NavigationPage(page));
        ImGui.PushStyleColor(ImGuiCol.Text,collapsed && holdsPage ? Accent:Text);
        bool clicked;
        try
        {
            // Game-font safe arrow: Font Awesome (shipped by Dalamud) instead of a Unicode triangle.
            ImGui.PushFont(Dalamud.Interface.UiBuilder.IconFont);
            try{ImGui.TextUnformatted((collapsed ? Dalamud.Interface.FontAwesomeIcon.CaretRight:Dalamud.Interface.FontAwesomeIcon.CaretDown).ToIconString());}
            finally{ImGui.PopFont();}
            ImGui.SameLine();
            using (TenshoAssistant.UI.UiComponents.PushSectionFont())
                clicked=ImGui.Selectable($"{localization.Get("Navigation.Category"+key)}###NavCategory{key}");
        }
        finally{ImGui.PopStyleColor();}
        Tip(collapsed ? "Navigation.Expand":"Navigation.Collapse");
        if(clicked)
        {
            var set=configuration.NavigationCollapsed=TenshoAssistant.Guide.FirstScreen.ToSave(configuration.NavigationCollapsed);
            if(!set.Remove(key))set.Add(key);
            try{save();}catch(Exception error){log.Warning(error,"Navigation state could not be saved.");}
            collapsed=!collapsed;
        }
        return !collapsed;
    }

    private void DrawCategoryNavigation()
    {
        switch(page)
        {
            case Page.CraftSimulation:
            case Page.CraftPresets:
            case Page.CraftLive:
                if(ImGui.SmallButton(localization.Get("CraftSimulation.Notebook")+"###CategoryRecipes")) {if(simulationRecipe is { } shown)SelectItem(shown.ResultItemId);page=Page.Items;focusCraftTab=true;}
                ImGui.SameLine();
                CategoryLink("CraftSimulation.Title",Page.CraftSimulation);
                CategoryLink("CraftPresets.Browse",Page.CraftPresets);
                CategoryLink("CraftSimulation.Live",Page.CraftLive);
                break;
            case Page.Gathering:
            case Page.GatheringGp:
            case Page.GatheringCollectables:
                CategoryLink("Navigation.Gathering",Page.Gathering);
                CategoryLink("Category.GatheringGp",Page.GatheringGp);
                CategoryLink("Category.GatheringCollectables",Page.GatheringCollectables);
                break;
            case Page.Collections:
            case Page.BlueMagic:
                CategoryLink("Navigation.Collection",Page.Collections);
                CategoryLink("Category.BlueMagic",Page.BlueMagic);
                break;
            default:return;
        }
        ImGui.NewLine();
        ImGui.Separator();
    }
}
