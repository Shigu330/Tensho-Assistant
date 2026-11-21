using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// R-06 step 3 (0.272): the detail sheet template — a header drawn by the caller, then one tab bar where only the open tab is
// computed. A requested tab (from a menu or another tool) is selected once through « focus ».
internal sealed partial class MainWindow
{
    // X-605 (0.1698): each detail bar's memory between frames (where its tabs were drawn, its number, the tab shown), by bar id.
    private readonly Dictionary<string,TenshoAssistant.UI.TabBarDrag> tabBarDrags=[];

    private void DetailTabs(string id,IReadOnlyList<(string Key,string Label,Action Draw)> tabs,ref string? focus)
    {
        // X-605 (0.1698, the user's request of 18/11): the tabs dragged with the mouse (Reorderable), drawn in the order kept (Configuration.
        // TabOrders, by bar id; Settings › Appearance puts them back); each tab's id stays its key, not its place.
        if(!tabBarDrags.TryGetValue(id,out var drag))tabBarDrags[id]=drag=new();
        if(!ImGui.BeginTabBar(id+"/"+drag.Generation,ImGuiTabBarFlags.Reorderable))return;
        var dragged=false;var fresh=drag.TakeFresh();
        drag.Places.Clear();
        try
        {
            var requested=focus;focus=null;
            foreach(var (key,label,draw) in TenshoAssistant.UI.TabOrder.Arrange(tabs,configuration.TabOrders.GetValueOrDefault(id),tab=>tab.Key))
            {
                var select=key==requested || fresh && requested is null && key==drag.Shown;
                var open=ImGui.BeginTabItem(label+"###"+id+key,select ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None);
                drag.Places.Add((key,ImGui.GetItemRectMin().X));
                if(ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))dragged=true;
                if(!open)continue;
                drag.Shown=key;
                try
                {
                    if(ImGui.BeginChild(id+key+"Body",System.Numerics.Vector2.Zero,false))draw();
                    ImGui.EndChild();
                }
                finally{ImGui.EndTabItem();}
            }
        }
        finally{ImGui.EndTabBar();}
        if(drag.After(dragged,configuration.TabOrders.GetValueOrDefault(id)) is { } keep)
        {
            configuration.TabOrders=new Dictionary<string,List<string>>(configuration.TabOrders){[id]=keep};
            TrySave("TabOrders");
        }
    }
}
