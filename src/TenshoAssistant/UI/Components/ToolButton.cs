using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace TenshoAssistant;

// X-76 step 2b (harmonisation H8, 0.920): the square icon button of a tool bar (map), with its tooltip; greyed when not enabled, the theme's
// Selected colour when active. Square on the frame height, so it follows the font size.
internal sealed partial class MainWindow
{
    private bool ToolButton(FontAwesomeIcon icon,string tooltipKey,bool enabled,bool active=false)
    {
        ImGui.BeginDisabled(!enabled);
        if(active)ImGui.PushStyleColor(ImGuiCol.Button,UI.FfxivTheme.Selected);
        ImGui.PushFont(UiBuilder.IconFont);
        bool clicked;
        try{clicked=ImGui.Button($"{icon.ToIconString()}##{tooltipKey}",new Vector2(ImGui.GetFrameHeight()));}
        finally{ImGui.PopFont();if(active)ImGui.PopStyleColor();ImGui.EndDisabled();}
        Tip(tooltipKey);
        return clicked && enabled;
    }
}
