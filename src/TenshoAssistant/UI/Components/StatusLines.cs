using Dalamud.Bindings.ImGui;

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
