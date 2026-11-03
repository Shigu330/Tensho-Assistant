using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

// Shared error and message presentation: a localized line for data and service errors (raw text only in developer mode), and service or
// sort messages mapped to localization keys (UI/UiMessages.cs).
internal sealed partial class MainWindow
{
    // 0.406 (user rule): a raw message (developer mode, details) in the chosen language when a table knows it, else as written (English,
    // the original). The log keeps English.
    private string RawMessage(string message)=>TenshoAssistant.UI.UiMessages.Key(message) is { } key ? localization.Get(key):message;
    // 0.324: a source turned off in Settings › Advanced is not a failure (Community/SourceDisabledException.cs, MarketService.SourceOff).
    private static bool SourceOff(string? message)=>message is not null && message.EndsWith(Community.SourceDisabledException.Suffix,StringComparison.Ordinal);

    private void DrawDataError(string message,string key)
    {
        if(SourceOff(message)){ImGui.TextWrapped(localization.Get("Advanced.SourceOffTip"));if(developerMode)ImGui.TextWrapped(RawMessage(message));return;}
        ImGui.PushTextWrapPos();
        try{ImGui.TextColored(FfxivTheme.Error,localization.Get(key));}
        finally{ImGui.PopTextWrapPos();}
        if(developerMode)ImGui.TextWrapped(RawMessage(message));
    }

    private void DrawServiceMessage(string message,string fallbackKey)
    {
        ImGui.TextWrapped(UiMessages.Get(localization,message,fallbackKey));
        if(developerMode)ImGui.TextWrapped(RawMessage(message));
    }

    private void DrawSortMessage(string message)
    {
        var key=message.StartsWith("SortUi.",StringComparison.Ordinal) ? message:UiMessages.Key(message);
        if(key is not null)ImGui.TextWrapped(localization.Get(key));
        else DrawDataError(message,"ServiceErrors.Sort");
    }
}
