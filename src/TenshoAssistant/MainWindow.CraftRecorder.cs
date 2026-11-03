using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// A-14 (0.436): « Record a rotation » in the simulator — start, craft by hand in the game, stop; the actions the game accepted become
// the simulator's sequence on « Use as the sequence » (then simulated, exported or saved as usual).
internal sealed partial class MainWindow
{
    internal CraftRecorder? CraftRecorder { get; set; }
    private string? craftRecorderMessage;

    private void DrawCraftRecorder(CraftActionCatalog catalog,uint job)
    {
        if(CraftRecorder is not { } recorder || !TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftRecorder.Title")+"###CraftRecorder"))return;
        ImGui.TextWrapped(localization.Get("CraftRecorder.Help"));
        var recording=recorder.Recording.Recording;
        if(!recording && ImGui.SmallButton(localization.Get("CraftRecorder.Start")))
            craftRecorderMessage=recorder.Start() ? null:localization.Get("CraftRecorder.Unavailable");
        if(recording && ImGui.SmallButton(localization.Get("CraftRecorder.Stop")))recorder.Stop();
        var (actions,ignored)=recorder.Recording.Snapshot();
        if(recording){ImGui.SameLine();ImGui.TextColored(Accent,localization.Format("CraftRecorder.Recording",actions.Count));}
        if(craftRecorderMessage is { } message)ImGui.TextWrapped(message);
        if(actions.Count==0)return;
        ImGui.TextDisabled(string.Join(" › ",actions.Select(kind=>catalog.Find(job,kind)?.Name ?? kind.ToString())));
        if(ignored>0)ImGui.TextDisabled(localization.Format("CraftRecorder.Ignored",ignored));
        if(ImGui.SmallButton(localization.Get("CraftRecorder.Use"))){simulationActions.Clear();simulationActions.AddRange(actions);simulationResult=null;simulationError=null;}
    }
}
