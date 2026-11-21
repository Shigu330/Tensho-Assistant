using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

/// <summary>
/// X-605 (0.1701): a tab of a bar the player arranges (<see cref="MainWindow.OrderedTabs"/>): its key (its id in the bar and in the order
/// kept), its label, its body (drawn while the tab is open; it may only note which tab is open) and whether a link asks for it (selected once).
/// </summary>
internal readonly record struct OrderedTab(string Key,string Label,Action Draw,bool Select=false);

// R-06 step 3 (0.272): the detail sheet template — a header drawn by the caller, then one tab bar where only the open tab is
// computed. A requested tab (from a menu or another tool) is selected once through « focus ».
internal sealed partial class MainWindow
{
    // X-605 (0.1698): each bar's memory between frames (where its tabs were drawn, its number, the tab shown), by bar id.
    private readonly Dictionary<string,TenshoAssistant.UI.TabBarDrag> tabBarDrags=[];

    /// <summary>The detail tabs: <see cref="OrderedTabs"/> with each body in its own child window (its own scroll).</summary>
    private void DetailTabs(string id,IReadOnlyList<(string Key,string Label,Action Draw)> tabs,ref string? focus)
    {
        var requested=focus;focus=null;
        var ordered=new List<OrderedTab>(tabs.Count);
        foreach(var (key,label,draw) in tabs)
            ordered.Add(new(key,label,()=>{if(ImGui.BeginChild(id+key+"Body",System.Numerics.Vector2.Zero,false))draw();ImGui.EndChild();},key==requested));
        if(!OrderedTabs(id,ordered))focus??=requested;
    }

    /// <summary>
    /// X-605 (0.1698 for the detail tabs, 0.1701 for every page's bar, the user's request of 18/11): a tab bar the player arranges with the
    /// mouse (Reorderable), its tabs drawn in the order kept (Configuration.TabOrders, by bar id; Settings › Appearance puts them back); each
    /// tab's id stays its key, not its place; only the open tab's body is drawn, in the page. False when the bar is not drawn.
    /// </summary>
    private bool OrderedTabs(string id,IReadOnlyList<OrderedTab> tabs,ImGuiTabBarFlags flags=ImGuiTabBarFlags.None)
    {
        if(!tabBarDrags.TryGetValue(id,out var drag))tabBarDrags[id]=drag=new();
        var arranged=TenshoAssistant.UI.TabOrder.Arrange(tabs,configuration.TabOrders.GetValueOrDefault(id),tab=>tab.Key);
        // X-709 (0.1700): a tab that comes, or another order shown, settles the same bar (one frame without Reorderable), never a new bar.
        if(!ImGui.BeginTabBar(id+"/"+drag.Generation,flags|(drag.Settle(arranged,tab=>tab.Key) ? ImGuiTabBarFlags.None:ImGuiTabBarFlags.Reorderable)))return false;
        var dragged=false;var fresh=drag.TakeFresh();
        var asked=false;
        foreach(var tab in arranged)asked|=tab.Select;
        drag.Places.Clear();
        try
        {
            foreach(var (key,label,draw,select) in arranged)
            {
                var open=ImGui.BeginTabItem(label+"###"+id+key,select || fresh && !asked && key==drag.Shown ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None);
                drag.Places.Add((key,ImGui.GetItemRectMin().X));
                if(ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))dragged=true;
                if(!open)continue;
                drag.Shown=key;
                try{draw();}
                finally{ImGui.EndTabItem();}
            }
        }
        finally{ImGui.EndTabBar();}
        TabBarAfter(id,drag,dragged);
        return true;
    }

    /// <summary>X-605: after a reorderable bar (the families' header, the detail tabs): the order shown kept and saved when a drag ends.</summary>
    private void TabBarAfter(string id,TenshoAssistant.UI.TabBarDrag drag,bool dragged)
    {
        if(drag.After(dragged,configuration.TabOrders.GetValueOrDefault(id)) is not { } keep)return;
        KeepOrder(id,keep);
    }

    /// <summary>X-605: an order the player arranged (a tab bar, a group of the side bar, 0.1702), kept by id and saved.</summary>
    private void KeepOrder(string id,List<string> keep)
    {
        configuration.TabOrders=new Dictionary<string,List<string>>(configuration.TabOrders){[id]=keep};
        TrySave("TabOrders");
    }
}
