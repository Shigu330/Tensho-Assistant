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

    private static float Scale=>Dalamud.Interface.Utility.ImGuiHelpers.GlobalScale;

    /// <summary>
    /// X-154 (0.1252): one selectable row for every list where a row is chosen. Selected: the theme's selection background and a bar on the
    /// left edge in the accent colour (SelectionMark); the label keeps the body colour, never the accent on the selection background.
    /// </summary>
    public static bool SelectableRow(string label,bool selected,float height=0)
    {
        var clicked=ImGui.Selectable(label,selected,ImGuiSelectableFlags.None,new Vector2(0,height));
        if(selected)
        {
            var (min,max)=SelectionMark.SideMark(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),Scale);
            ImGui.GetWindowDrawList().AddRectFilled(min,max,ImGui.GetColorU32(FfxivTheme.Gold));
        }
        return clicked;
    }

    public static bool NavigationItem(string label, bool selected)
        => SelectableRow(label, selected, 26*Scale);

    public static bool CategoryItem(string label,bool selected)
    {
        if(selected)ImGui.PushStyleColor(ImGuiCol.Button,FfxivTheme.Selected);
        bool clicked;
        try{clicked=ImGui.SmallButton(label);}
        finally{if(selected)ImGui.PopStyleColor();}
        if(selected)
        {
            // X-154: the same mark as a selected row, under the button (the buttons sit side by side).
            var (min,max)=SelectionMark.UnderMark(ImGui.GetItemRectMin(),ImGui.GetItemRectMax(),Scale);
            ImGui.GetWindowDrawList().AddRectFilled(min,max,ImGui.GetColorU32(FfxivTheme.Gold));
        }
        return clicked;
    }

    /// <summary>X-154 (0.1252): a title inside a page (a block of the page, the page named beside the folded navigation): the section font, accent colour.</summary>
    public static void SubTitle(string text)
    {
        using (PushSectionFont()) ImGui.TextColored(FfxivTheme.Gold,text);
    }

    public static void SectionHeader(string title, string? description = null)
    {
        using (TitleFont is { Available: true } font ? font.Push() : null)
            ImGui.TextColored(FfxivTheme.Gold, title);
        if (!string.IsNullOrWhiteSpace(description)) ImGui.TextWrapped(description);
        // X-154 (0.1254): space under a page title instead of one more line (the panes and the sections already draw theirs).
        ImGui.Dummy(new Vector2(0, ImGui.GetStyle().ItemSpacing.Y*2));
    }
}
