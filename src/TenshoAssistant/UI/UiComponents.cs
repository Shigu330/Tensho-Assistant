using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant.UI;

internal static class UiComponents
{
    public static bool NavigationItem(string label, bool selected)
        => ImGui.Selectable(label, selected, ImGuiSelectableFlags.None, new Vector2(0, 26));

    public static bool CategoryItem(string label,bool selected)
    {
        if(selected)ImGui.PushStyleColor(ImGuiCol.Button,FfxivTheme.Selected);
        try{return ImGui.SmallButton(label);}
        finally{if(selected)ImGui.PopStyleColor();}
    }

    public static void SectionHeader(string title, string? description = null)
    {
        ImGui.TextColored(FfxivTheme.Gold, title);
        if (!string.IsNullOrWhiteSpace(description)) ImGui.TextWrapped(description);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }

    public static void StatusBadge(string label, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted($"◆ {label}");
        ImGui.PopStyleColor();
    }
}
