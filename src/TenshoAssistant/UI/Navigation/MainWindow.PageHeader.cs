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
        if(!ImGui.BeginTabBar("PageTabs"+root)){pageTabs.Reset();return;}
        try
        {
            var current=page;
            Page? chosen=null;
            foreach(var entry in PageEntries)
            {
                if(entry.Tab is not { } tab || (entry.Page!=root && entry.Parent!=root))continue;
                // X-601 (0.1677): a tab that cannot be reached here (developer mode, or listed on the other side only) is not drawn,
                // unless a link opened it.
                if(entry.Page!=current && !Reachable(entry.Page))continue;
                var flags=pageTabs.Force(current,entry.Page) ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None;
                if(!ImGui.BeginTabItem(tab()+"###PageTab"+entry.Page,flags))continue;
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
    }
}
