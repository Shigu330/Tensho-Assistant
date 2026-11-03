using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

// Craft simulator › conditions: conservative quality range of the fixed sequence over the model's condition changes, starting Normal
// (Good and Poor return to Normal, Excellent forces Poor), for a Good quality bonus of ×1.50, ×1.75 or both; no probability and no HQ
// guarantee. Evaluated in Draw only when the simulation result or the scenario changes (Craft/CraftConditionEnvelope.cs).
internal sealed partial class MainWindow
{
    private CraftSimulationResult? conditionEnvelopeSource;
    private CraftConditionEnvelopeResult? conditionEnvelope;
    private string? conditionEnvelopeError;
    private int conditionEnvelopeTool;

    private void DrawCraftConditionEnvelope(CraftSimulationResult simulation)
    {
        if(!TenshoAssistant.UI.UiComponents.Header(localization.Get("CraftConditions.Title")+"###CraftConditions"))return;
        ImGui.TextWrapped(localization.Get("CraftConditions.Help"));
        ImGui.SetNextItemWidth(330);
        var scenario = localization.Get(conditionEnvelopeTool switch {1=>"CraftConditions.Standard",2=>"CraftConditions.Enhanced",_=>"CraftConditions.Both"});
        if(ImGui.BeginCombo(localization.Get("CraftConditions.Scenario")+"###ConditionScenario",scenario))
        {
            try
            {
                for(var index=0;index<3;index++)
                    if(ImGui.Selectable(localization.Get(index switch {1=>"CraftConditions.Standard",2=>"CraftConditions.Enhanced",_=>"CraftConditions.Both"}),conditionEnvelopeTool==index))
                    {conditionEnvelopeTool=index;conditionEnvelopeSource=null;}
            }
            finally {ImGui.EndCombo();}
        }
        Tip("CraftConditions.ScenarioTip");
        if(!ReferenceEquals(conditionEnvelopeSource,simulation))
        {
            conditionEnvelopeSource=simulation;conditionEnvelope=null;conditionEnvelopeError=null;
            try
            {
                conditionEnvelope=CraftConditionEnvelope.Evaluate(simulationRecipe!,simulation.Stats,simulation.StartingQuality,simulationActions,
                    conditionEnvelopeTool==0 ? null:conditionEnvelopeTool==1 ? 150:175);
            }
            catch(ArgumentException error){conditionEnvelopeError=error.Message;}
            catch(InvalidOperationException error){conditionEnvelopeError=error.Message;}
        }
        if(conditionEnvelopeError is { } errorText)
        {
            ImGui.TextWrapped(localization.Get("CraftConditions.Error"));
            if(developerMode)ImGui.TextWrapped(RawMessage(errorText));
        }
        if(conditionEnvelope is not { } result)return;
        ImGui.TextColored(Accent,localization.Format("CraftConditions.Result",result.Quality,simulation.Final.Quality));
        ImGui.TextWrapped(localization.Get("CraftConditions.BoundsHelp"));
        if(simulation.Final.Finish!=CraftFinish.Completed || simulation.Error is not null)ImGui.TextWrapped(localization.Get("CraftConditions.Incomplete"));
        if(ImGui.TreeNode(localization.Get("CraftConditions.Steps")+"###ConditionSteps"))
        {
            try {foreach(var band in result.Steps)ImGui.TextUnformatted(localization.Format("CraftConditions.Step",band.Step,band.Quality));}
            finally {ImGui.TreePop();}
        }
    }
}
