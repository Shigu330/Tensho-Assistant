using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    private CraftSimulationResult? conditionEnvelopeSource;
    private CraftConditionEnvelopeResult? conditionEnvelope;
    private string? conditionEnvelopeError;
    private int conditionEnvelopeTool;

    private void DrawCraftConditionEnvelope(CraftSimulationResult simulation)
    {
        if(!ImGui.CollapsingHeader(localization.Get("CraftConditions.Title")+"###CraftConditions"))return;
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
            if(developerMode)ImGui.TextWrapped(errorText);
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
