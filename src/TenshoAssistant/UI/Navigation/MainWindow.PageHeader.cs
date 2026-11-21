using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// X-600 (0.1573, user request of 18/11, D-186): the one header of a page with sub-pages, drawn from the page list (MainWindow.Pages.cs):
// the family's title (its navigation entry), the description of the page shown, and the tab bar of the family, as the Storage page draws
// its own. It replaces the row of small buttons written by hand for crafting, gathering and the collections. A page alone (no Tab in the
// list) keeps its own heading.
internal sealed partial class MainWindow
{
    private TenshoAssistant.UI.PageTabSync<Page> pageTabs;

    private void DrawPageHeader()
    {
        var root=NavigationPage(page);
        if(PageEntryFor(root) is not { Tab: not null, Label: { } title } family || PageEntryFor(page) is not { Tab: not null } shown)
        {
            pageTabs.Reset();
            return;
        }
        Heading(title(),shown.About?.Invoke() ?? "");
        var key="PageTabs."+root;var dragged=false;
        var current=page;
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
        var arranged=TenshoAssistant.UI.TabOrder.Arrange(natural,configuration.TabOrders.GetValueOrDefault(key),entry=>entry.Page.ToString());
        // X-605 (0.1697, the user's request of 18/11): the tabs dragged with the mouse (Reorderable); each tab's id is its page (not its place).
        // X-709 (0.1700): the bar's memory is the detail tabs' (TabBarDrag, by family): a tab that comes (opened by a link) or another order
        // shown settles the same bar (one frame without Reorderable), never a new bar.
        if(!tabBarDrags.TryGetValue(key,out var drag))tabBarDrags[key]=drag=new();
        if(!ImGui.BeginTabBar("PageTabs"+root+"###PageTabs"+root+"/"+drag.Generation,drag.Settle(arranged,entry=>entry.Page.ToString()) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable)){pageTabs.Reset();return;}
        // A new bar (put back in order): the page shown selected in it.
        if(drag.TakeFresh())pageTabs.Reset();
        drag.Places.Clear();
        try
        {
            Page? chosen=null;
            foreach(var entry in arranged)
            {
                var flags=pageTabs.Force(current,entry.Page) ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
                var open=ImGui.BeginTabItem(entry.Tab!()+"###PageTab"+entry.Page,flags);
                drag.Places.Add((entry.Page.ToString(),ImGui.GetItemRectMin().X));
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
        // X-605 (0.1697): when a drag ends, the order the bar shows is kept (Configuration.TabOrders, by family), saved.
        TabBarAfter(key,drag,dragged);
    }

    /// <summary>X-605 (0.1697): every family's tabs back in the list's order (a new bar, the page shown kept). 0.1698: and every detail bar's
    /// (X-709, 0.1700: the families' bars are in the same list).</summary>
    private void ResetTabOrders()
    {
        configuration.TabOrders=new Dictionary<string,List<string>>();
        pageTabs.Reset();
        foreach(var drag in tabBarDrags.Values)drag.Renew();
        TrySave("TabOrders");
    }
}
