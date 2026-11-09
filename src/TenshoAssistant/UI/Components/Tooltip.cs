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

    // Tooltip of the last drawn item (also when disabled).
    private void Tip(string key)
    {
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(localization.Get(key));
    }

    // Tooltip of the last drawn item from a ready text (also when disabled).
    private static void TipText(string text)
    {
        if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))WrappedTooltip(text);
    }

    // A muted « info » icon on the line, its help text on hover.
    private void HelpMarker(string key)
    {
        DrawHelpIcon();
        Tip(key);
    }

    // The same marker for a help text already built (formatted or made of several parts).
    private static void HelpMarkerText(string text)
    {
        DrawHelpIcon();
        TipText(text);
    }

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

    /// <summary>True when the mouse is over the box; the next tooltip is then placed under it (server bar entries).</summary>
    public static bool UnderBox((Vector2 Min,Vector2 Max)? bounds)
    {
        if(bounds is not { } box)return false;
        var mouse=ImGui.GetIO().MousePos;
        if(mouse.X<box.Min.X || mouse.X>box.Max.X || mouse.Y<box.Min.Y || mouse.Y>box.Max.Y)return false;
        ImGui.SetNextWindowPos(new Vector2(box.Min.X,box.Max.Y+4));
        return true;
    }

    public void Dispose()
    {
        if(!begun)return;
        try{ImGui.PopTextWrapPos();}
        finally{ImGui.EndTooltip();}
    }
}
