using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-600 (0.1573, user request of 18/11, D-186): the one header of a page with sub-pages, drawn from the page list (MainWindow.Pages.cs):
// the family's title (its navigation entry), the description of the page shown, and the tab bar of the family, as the Storage page draws
// its own. It replaces the row of small buttons written by hand for crafting, gathering and the collections. A page alone (no Tab in the
// list) keeps its own heading.
internal sealed partial class MainWindow
{
    private TenshoAssistant.UI.PageTabSync<Page> pageTabs;
    // X-605 (0.1697): the family's tabs as the bar placed them in this frame, the bar's number (a new bar shows the order kept), a drag under way.
    private readonly List<(string Id,float X)> pageTabPlaces=[];
    private int pageTabGeneration;
    private bool pageTabDragging;

    private void DrawPageHeader()
    {
        var root=NavigationPage(page);
        if(PageEntryFor(root) is not { Tab: not null, Label: { } title } family || PageEntryFor(page) is not { Tab: not null } shown)
        {
            pageTabs.Reset();
            return;
        }
        Heading(title(),shown.About?.Invoke() ?? "");
        // X-605 (0.1697, the user's request of 18/11): the tabs dragged with the mouse (Reorderable); each tab's id is its page (not its place).
        if(!ImGui.BeginTabBar("PageTabs"+root+"###PageTabs"+root+"/"+pageTabGeneration,ImGuiTabBarFlags.Reorderable)){pageTabs.Reset();return;}
        var key="PageTabs."+root;var dragged=false;
        pageTabPlaces.Clear();
        try
        {
            var current=page;
            Page? chosen=null;
            var natural=new List<PageEntry>();
            foreach(var entry in PageEntries)
            {
                if(entry.Tab is null || (entry.Page!=root && entry.Parent!=root))continue;
                // X-601 (0.1677): a tab that cannot be reached here (developer mode, or listed on the other side only) is not drawn,
                // unless a link opened it.
                if(entry.Page!=current && !Reachable(entry.Page))continue;
                natural.Add(entry);
            }
            // X-605 (0.1697): in the order the player kept (Settings › Appearance puts the list's order back).
            foreach(var entry in TenshoAssistant.UI.TabOrder.Arrange(natural,configuration.TabOrders.GetValueOrDefault(key),entry=>entry.Page.ToString()))
            {
                var flags=pageTabs.Force(current,entry.Page) ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
                var open=ImGui.BeginTabItem(entry.Tab!()+"###PageTab"+entry.Page,flags);
                pageTabPlaces.Add((entry.Page.ToString(),ImGui.GetItemRectMin().X));
                if(ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))dragged=true;
                if(!open)continue;
                ImGui.EndTabItem();
                chosen??=pageTabs.Chosen(current,entry.Page);
            }
            if(family.TabAction is { } action && ImGui.TabItemButton(action.Label()+"###PageTabAction",ImGuiTabItemFlags.Trailing))
            {
                pageTabs.Reset();
                action.Run();
                return;
            }
            if(chosen is { } next)page=next;
            pageTabs.Shown(page);
            familyLastTab[root]=page;
        }
        finally{ImGui.EndTabBar();}
        PageTabsAfterDrag(key,dragged);
    }

    /// <summary>
    /// X-605 (0.1697): when a drag ends, the order the bar shows is kept (Configuration.TabOrders, by family), saved; otherwise the order kept
    /// is the truth: a bar showing another one (a tab came into reach or left it) is made again in the order kept, the page shown selected.
    /// </summary>
    private void PageTabsAfterDrag(string key,bool dragged)
    {
        if(dragged){pageTabDragging=true;return;}
        var shown=TenshoAssistant.UI.TabOrder.Shown(pageTabPlaces);
        var same=shown.SequenceEqual(pageTabPlaces.Select(place=>place.Id));
        if(pageTabDragging)
        {
            pageTabDragging=false;
            if(same)return;
            configuration.TabOrders=new Dictionary<string,List<string>>(configuration.TabOrders)
                {[key]=TenshoAssistant.UI.TabOrder.Keep(shown,configuration.TabOrders.GetValueOrDefault(key))};
            TrySave("TabOrders");
            return;
        }
        if(!same){pageTabGeneration++;pageTabs.Reset();}
    }

    /// <summary>X-605 (0.1697): every family's tabs back in the list's order (a new bar, the page shown kept). 0.1698: and every detail bar's.</summary>
    private void ResetTabOrders()
    {
        configuration.TabOrders=new Dictionary<string,List<string>>();
        pageTabGeneration++;pageTabs.Reset();
        foreach(var drag in tabBarDrags.Values)drag.Renew();
        TrySave("TabOrders");
    }
}
