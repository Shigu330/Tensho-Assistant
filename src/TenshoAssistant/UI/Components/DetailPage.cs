using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// R-06 step 3 (0.272): the detail sheet template — a header drawn by the caller, then one tab bar where only the open tab is
// computed. A requested tab (from a menu or another tool) is selected once through « focus ».
internal sealed partial class MainWindow
{
    private void DetailTabs(string id,IReadOnlyList<(string Key,string Label,Action Draw)> tabs,ref string? focus)
    {
        if(!ImGui.BeginTabBar(id))return;
        try
        {
            var requested=focus;focus=null;
            foreach(var (key,label,draw) in tabs)
            {
                if(!ImGui.BeginTabItem(label+"###"+id+key,key==requested ? ImGuiTabItemFlags.SetSelected:ImGuiTabItemFlags.None))continue;
                try
                {
                    if(ImGui.BeginChild(id+key+"Body",System.Numerics.Vector2.Zero,false))draw();
                    ImGui.EndChild();
                }
                finally{ImGui.EndTabItem();}
            }
        }
        finally{ImGui.EndTabBar();}
    }
}
