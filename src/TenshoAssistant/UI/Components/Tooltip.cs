using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace TenshoAssistant;

// X-76 step 2b (harmonisation H1): one tooltip helper everywhere. Tip(key) for a translated help text, TipText(text) for a ready
// text, both also when the item is greyed; HelpMarker for a « help » icon; TooltipScope for the tooltips drawn by hand, so they all
// wrap at the same width.
internal sealed partial class MainWindow
{
    // Tooltip text wrapped to a readable width (0.207: long help texts went past the screen edge).
    private static void WrappedTooltip(string text)
    {
        using(TooltipScope.Begin())ImGui.TextUnformatted(text);
    }

    // X-684 (0.1676): the title of a tooltip that names a thing of the game (place, monster, action, FATE): its name, the same everywhere.
    private void TooltipTitle(string text)=>ImGui.TextColored(Accent,text);

    // Tooltip of the last drawn item (also when disabled).
    private void Tip(string key)
    {
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(key));
    }

    // Tooltip of the last drawn item from a ready text (also when disabled).
    // X-878 (0.1889): no closure. A text that is computed (formatted, joined, made of several parts) is built only on hover, by
    // « if(TipHovered())TipText(…) »: nothing is allocated per row and per frame while the mouse is elsewhere (UiRatchets1885Tests).
    private static void TipText(string text)
    {
        if(TipHovered())WrappedTooltip(text);
    }

    // True when the last drawn item is hovered, also when disabled: the guard of a tooltip whose text is computed.
    private static bool TipHovered()=>ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

    // X-607 (0.1956): the guard of a rich tooltip (item, action, place, quest, duty, FATE, NPC, recipe, gathering…): the last drawn item
    // hovered, not when greyed. One form everywhere, « if(RichTipHovered())DrawXTooltip(…) », in place of the hover test on each page
    // (UiRichTips1956Tests). Also with another condition or braces: « if(x && RichTipHovered()){…DrawXTooltip(…)…} » (X-913, 0.1957,
    // UiRichTips1957Tests).
    private static bool RichTipHovered()=>ImGui.IsItemHovered();

    // X-607 (0.1943): the item's tooltip (name, kind, level, price, sources) of the last drawn item, on hover, the same guard as before
    // (not when greyed). One form everywhere in place of the hover test and the item tooltip written by hand on each page (UiItemTips1943Tests).
    private void ItemTip(uint id,bool glamourOnly=false,bool? hq=null)
    {
        if(RichTipHovered())DrawItemInfoTooltip(id,glamourOnly,hq);
    }

    // A muted « info » icon on the line, its help text on hover.
    private void HelpMarker(string key)
    {
        DrawHelpIcon();
        Tip(key);
    }

    // The bare « info » icon. X-880 (0.1891): a help text built of several parts follows it behind its guard,
    // « DrawHelpIcon();if(TipHovered())TipText(…) » (HelpMarkerText took that text built, at each frame).
    private static void DrawHelpIcon()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());}
        finally{ImGui.PopFont();}
    }
}

/// <summary>A tooltip drawn by hand, with the same wrap width as every other tooltip: begun by <see cref="Begin"/>, ended on Dispose.</summary>
internal readonly struct TooltipScope : IDisposable
{
    private readonly bool begun;
    private TooltipScope(bool begun)=>this.begun=begun;

    /// <summary>Wrap width of every tooltip, from the font size (32 letters' height, as since 0.207).</summary>
    public static float WrapWidth=>ImGui.GetFontSize()*32;

    public static TooltipScope Begin()
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(WrapWidth);
        return new TooltipScope(true);
    }

    /// <summary>True when the mouse is over the box; the next tooltip is then placed under it, or above it (server bar entries; X-162:
    /// BarTooltipPlace, never off the screen).</summary>
    public static bool UnderBox((Vector2 Min,Vector2 Max)? bounds,bool above=false)
    {
        if(bounds is not { } box)return false;
        var mouse=ImGui.GetIO().MousePos;
        if(mouse.X<box.Min.X || mouse.X>box.Max.X || mouse.Y<box.Min.Y || mouse.Y>box.Max.Y)return false;
        var (position,pivot)=TenshoAssistant.UI.BarTooltipPlace.For(box.Min,box.Max,ImGui.GetIO().DisplaySize,above);
        ImGui.SetNextWindowPos(position,ImGuiCond.Always,pivot);
        return true;
    }

    public void Dispose()
    {
        if(!begun)return;
        try{ImGui.PopTextWrapPos();}
        finally{ImGui.EndTooltip();}
    }
}
