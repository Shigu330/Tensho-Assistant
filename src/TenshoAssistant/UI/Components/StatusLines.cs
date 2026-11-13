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
        ImGui.PushFont(UiBuilder.IconFont);
        try{ImGui.TextColored(colour,icon.ToIconString());}
        finally{ImGui.PopFont();}
        ImGui.SameLine();
        ImGui.TextColored(colour,localization.Get(UiStates.Key(state)));
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
