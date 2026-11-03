using Dalamud.Bindings.ImGui;

namespace TenshoAssistant;

// Craft messages: the craft domain (Craft/*.cs) explains rejections and issues in French sentences; each known sentence is mapped to a
// localization key (CraftMessages.*), the raw text is shown only in developer mode. No simulation rule lives here.
internal sealed partial class MainWindow
{
    // Presentation adapter for legacy domain explanations; simulation data and rules remain unchanged.
    private string? CraftMessageKey(string message)=>message switch
    {
        "Simulation parameters missing for this recipe." => "CraftMessages.NoParameters",
        "Expert, adjusted, specialist or fixed-quality recipe outside this simulation's scope." => "CraftMessages.SpecialRecipe",
        "Collectable maximum quality missing or incompatible." => "CraftMessages.CollectableQuality",
        "Native parameters incomplete or incompatible with the model." => "CraftMessages.NativeParameters",
        "Craftsmanship or control below the recipe's minimum." => "CraftMessages.MinimumStats",
        "100 actions maximum." => "CraftMessages.ActionLimit",
        "Favourable condition or bonus not supported." => "CraftMessages.Condition",
        "Action non prise en charge." => "CraftMessages.UnsupportedAction",
        "Level too low for this action." => "CraftMessages.Level",
        "Use of Manipulation not confirmed in the scenario." => "CraftMessages.Manipulation",
        "Action reserved for the first step." => "CraftMessages.FirstTurn",
        "Quick Innovation needs ten levels more than the recipe." => "CraftMessages.TrainedEye",
        "Action incompatible with Waste Not." => "CraftMessages.WasteNot",
        "Ten stacks of Inner Quiet required." => "CraftMessages.QuietTen",
        "Trained Perfection can be used only once per synthesis." => "CraftMessages.TrainedOnce",
        "Inner Quiet required for Byregot." => "CraftMessages.Byregot",
        "This recipe has no quality target." => "CraftMessages.NoQuality",
        "Not enough CP for this action." => "CraftMessages.Cp",
        "Crafting log selection too old or invalid." => "CraftMessages.LogOld",
        "Recipe not linked to the current catalogue; no matching by name." => "CraftMessages.LogRecipe",
        "Crafting log starting quality missing or maximum quality incompatible; calculation suspended." => "CraftMessages.LogQuality",
        "Crafting log starting quality outside the recipe's limits." => "CraftMessages.QualityBounds",
        "Waiting for the character's recent attributes." => "CraftMessages.StatsWaiting",
        "Crafting attributes incomplete or incompatible." => "CraftMessages.StatsIncomplete",
        "No finished synthesis found within the search limits." => "CraftMessages.NoCompletion",
        "The sequence ends below the target quality." => "CraftMessages.BelowTarget",
        "Observation missing or too old." => "CraftMessages.ObservationOld",
        "Action or animation in progress." => "CraftMessages.Animating",
        "Recipe identity not established." => "CraftMessages.RecipeIdentity",
        "Typed native values missing, inconsistent, or condition outside the four ordinary states." => "CraftMessages.TypedValues",
        "Favourable tool bonus unknown: the affected quality gains stay unknown, the other resources are still projected." => "CraftMessages.ToolBonus",
        "Displayed resources, native values and recipe do not agree, or synthesis finished." => "CraftMessages.Inconsistent",
        "Native action availability not established for this job." => "CraftMessages.Availability",
        "Full status reading not established; absence of an effect unknown." => "CraftMessages.Statuses",
        "Effect metadata incomplete." => "CraftMessages.EffectMetadata",
        "Two Waste Not effects at once: inconsistent state." => "CraftMessages.WasteNotConflict",
        "Craftsmanship or Control below the recipe's minimum." => "CraftMessages.MinimumStats",
        _=>TenshoAssistant.UI.UiMessages.Key(message),
    };
    private string CraftMessage(string message)=>CraftMessageKey(message) is { } key ? localization.Get(key):message;

    private void DrawCraftIssue(string message)
    {
        var key=CraftMessageKey(message);
        if(key is not null){ImGui.TextWrapped(localization.Get(key));return;}
        const string transitionSuffix=": available client-side, no compatible transition in the model.";
        if(message.EndsWith(transitionSuffix,StringComparison.Ordinal))
            ImGui.TextWrapped(localization.Format("CraftMessages.NoTransition",message[..^transitionSuffix.Length]));
        else
            ImGui.TextWrapped(localization.Get(
                // 0.406: « Status counter » first, it also starts with « Status ».
                message.StartsWith("Status counter ",StringComparison.Ordinal) ? "CraftMessages.UnknownCounter":
                message.StartsWith("Status ",StringComparison.Ordinal) ? "CraftMessages.DuplicateEffect":
                message.StartsWith("Effect ",StringComparison.Ordinal) ? "CraftMessages.UnsupportedEffect":"CraftMessages.Uninterpreted"));
        if(developerMode)ImGui.TextWrapped(RawMessage(message));
    }
}
