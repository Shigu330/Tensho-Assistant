using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// X-76 step 2b (harmonisation H7, 0.920): one look for a page's empty or loading line (muted, wrapped) and one copy button (small, with a
// « Copied » mark for two seconds). Data and service errors go through DrawDataError (MainWindow.ErrorPresentation.cs).
internal sealed partial class MainWindow
{
    private string? copiedId;private long copiedAt=-100_000;

    /// <summary>A page's « nothing here » or « loading » line: muted and wrapped, the same everywhere.</summary>
    private void EmptyState(string key)
    {
        ImGui.PushStyleColor(ImGuiCol.Text,Muted);
        try{ImGui.TextWrapped(localization.Get(key));}
        finally{ImGui.PopStyleColor();}
    }

    /// <summary>
    /// X-154 (0.1253): one of the four states of a page (waiting, source turned off, old data, failed), the same everywhere: its glyph and
    /// its short word in the state's colour, then the page's own sentence (muted, wrapped). The glyph and the word tell the state, never
    /// the colour alone (UI/UiStates.cs).
    /// </summary>
    private void StateLine(UiState state,string? detail=null)
    {
        var colour=UiStates.Colour(state) switch{UiColour.Warning=>Warning,UiColour.Error=>Error,_=>Muted};
        var icon=state switch{UiState.Waiting=>FontAwesomeIcon.Hourglass,UiState.SourceOff=>FontAwesomeIcon.PowerOff,UiState.Stale=>FontAwesomeIcon.History,_=>FontAwesomeIcon.ExclamationTriangle};
        GlyphLine(icon,colour,localization.Get(UiStates.Key(state)),detail);
    }

    /// <summary>
    /// X-156 (0.1264): where the values under this line come from, said the same way everywhere: read (« measured »), computed by Tensho
    /// from a rule (« estimate »), or read too long ago (the « old data » state itself). The glyph and the word tell it, not the colour.
    /// </summary>
    private void OriginLine(UiOrigin origin,string? detail=null)
    {
        if(origin==UiOrigin.Old){StateLine(UiState.Stale,detail);return;}
        var icon=origin switch{UiOrigin.Measured=>FontAwesomeIcon.Eye,_=>FontAwesomeIcon.Calculator};
        GlyphLine(icon,Muted,localization.Get(UiOrigins.Key(origin)),detail);
    }

    /// <summary>
    /// X-156 (0.1264): the same origin beside one value (a table cell, a line): the glyph alone, its word and the reason on hover.
    /// A measured value carries no mark: only what is not a plain read is flagged.
    /// </summary>
    private void OriginMark(UiOrigin origin,string? detail=null)
    {
        if(origin==UiOrigin.Measured)return;
        var icon=origin switch{UiOrigin.Estimated=>FontAwesomeIcon.Calculator,_=>FontAwesomeIcon.History};
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(origin==UiOrigin.Old ? Warning:Muted,icon.ToIconString());}
        finally{ImGui.PopFont();}
        var word=localization.Get(UiOrigins.Key(origin));
        TipText(string.IsNullOrWhiteSpace(detail) ? word:word+" · "+detail);
    }

    // One drawing for the state and origin lines: the glyph and the short word in the colour, then the page's sentence (muted, wrapped).
    private void GlyphLine(FontAwesomeIcon icon,System.Numerics.Vector4 colour,string word,string? detail)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(colour,icon.ToIconString());}
        finally{ImGui.PopFont();}
        ImGui.SameLine();
        ImGui.TextColored(colour,word);
        if(string.IsNullOrWhiteSpace(detail))return;
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Text,Muted);
        try{ImGui.TextWrapped(detail);}
        finally{ImGui.PopStyleColor();}
    }

    /// <summary>A small button that copies <paramref name="text"/> (built only on click) and shows « Copied » next to it for two seconds.</summary>
    private bool CopyButton(string label,Func<string> text)
    {
        var clicked=ImGui.SmallButton(label);
        if(clicked)
        {
            try{ImGui.SetClipboardText(text());copiedId=label;copiedAt=Environment.TickCount64;}
            catch(Exception error){log.Warning(error,"Copy to the clipboard failed.");}
        }
        if(copiedId==label && Environment.TickCount64-copiedAt<2000){ImGui.SameLine();ImGui.TextColored(Success,localization.Get("Common.Copied"));}
        return clicked;
    }
}
