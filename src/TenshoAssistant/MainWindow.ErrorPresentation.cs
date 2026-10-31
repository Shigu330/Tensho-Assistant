using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawDataError(string message,string key)
    {
        ImGui.PushTextWrapPos();
        try{ImGui.TextColored(FfxivTheme.Error,localization.Get(key));}
        finally{ImGui.PopTextWrapPos();}
        if(developerMode)ImGui.TextWrapped(message);
    }

    private void DrawServiceMessage(string message,string fallbackKey)
    {
        ImGui.TextWrapped(UiMessages.Get(localization,message,fallbackKey));
        if(developerMode)ImGui.TextWrapped(message);
    }

    private void DrawSortMessage(string message)
    {
        var key=message.StartsWith("SortUi.",StringComparison.Ordinal) ? message:UiMessages.Key(message);
        if(key is not null)ImGui.TextWrapped(localization.Get(key));
        else DrawDataError(message,"ServiceErrors.Sort");
    }
}
