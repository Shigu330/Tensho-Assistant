using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

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
        // 0.457 (user request, in-game campaign): the recorded actions by their icons, in order; number and name on hover.
        for(var index=0;index<actions.Count;index++)
        {
            if(index>0)
            {
                ImGui.SameLine();
                if(ImGui.GetContentRegionAvail().X<24)ImGui.NewLine();
            }
            var metadata=catalog.Find(job,actions[index]);
            DrawGameIcon(metadata?.Icon ?? 0,new System.Numerics.Vector2(24));
            if(ImGui.IsItemHovered())WrappedTooltip($"{index+1}. {metadata?.Name ?? actions[index].ToString()}");
        }
        if(ignored>0)ImGui.TextDisabled(localization.Format("CraftRecorder.Ignored",ignored));
        if(ImGui.SmallButton(localization.Get("CraftRecorder.Use"))){simulationActions.Clear();simulationActions.AddRange(actions);simulationResult=null;simulationError=null;}
    }
}
