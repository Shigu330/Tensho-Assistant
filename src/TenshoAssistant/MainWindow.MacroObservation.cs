using System.Text;
using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private void DrawMacroObservation()
    {
        if (!ImGui.CollapsingHeader(localization.Get("MacroObserverUi.Title"))) return;
        var state = macroObservations.State;
        DrawToolSettingsLink("MacroObservation");
        ImGui.TextDisabled(state.Enabled ? localization.Get("MacroObserverUi.Enabled"):localization.Get("MacroObserverUi.Disabled"));
        ImGui.TextWrapped(localization.Get("MacroObserverUi.Help"));
        if (state.Error is { } error) DrawDataError(error,"ServiceErrors.Observation");
        if (state.Current is not { } current) return;
        ImGui.TextDisabled(current.Available ? localization.Format("MacroObserverUi.Current",current.Line,current.Locked,current.WaitMs) : localization.Get("MacroObserverUi.Unavailable"));
        foreach (var row in state.Trace.TakeLast(10))
            ImGui.TextUnformatted(localization.Format("MacroObserverUi.Trace",row.ObservedAt.ToLocalTime().ToString("HH:mm:ss.fff"),row.Line,row.Locked,row.WaitMs,row.Name));
        if (ImGui.Button(localization.Get("MacroObserverUi.Copy")))
        {
            var text = new StringBuilder(localization.Format("MacroObserverUi.CopyHeader",BuildVersion));
            foreach (var row in state.Trace)
                text.AppendLine(localization.Format("MacroObserverUi.CopyRow",row.ObservedAt.ToLocalTime().ToString("O"),row.Available,row.Line,row.Locked,row.WaitMs,row.Name));
            ImGui.SetClipboardText(text.ToString());
        }
    }
}
