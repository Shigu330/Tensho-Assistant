using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant.UI;

internal static class UiComponents
{
    // 0.293 (R-06 step 6, game resources first): page titles use the game's own title font (TrumpGothic, as its window titles),
    // provided by Dalamud from the game files; null or not built yet = the normal font.
    public static Dalamud.Interface.ManagedFontAtlas.IFontHandle? TitleFont { get; set; }
    // 0.294: the same game font, smaller, for section headers and sidebar categories.
    public static Dalamud.Interface.ManagedFontAtlas.IFontHandle? SectionFont { get; set; }
    public static IDisposable? PushSectionFont()=>SectionFont is { Available: true } font ? font.Push():null;
    // 0.394 (user request): the text font of the Tensho window (null = Dalamud's own font), pushed by MainWindow.PreDraw.
    public static Dalamud.Interface.ManagedFontAtlas.IFontHandle? BodyFont { get; set; }

    /// <summary>A collapsing section header in the game's title font (every section header goes through here).</summary>
    public static bool Header(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        using (PushSectionFont()) return ImGui.CollapsingHeader(label, flags);
    }

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
        using (TitleFont is { Available: true } font ? font.Push() : null)
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
