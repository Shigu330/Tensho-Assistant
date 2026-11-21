using Dalamud.Bindings.ImGui;
using TenshoAssistant.UI;

namespace TenshoAssistant;

internal sealed partial class MainWindow
{
    /// <summary>X-684 (0.1696, M3 of X-700): whether a part of Tensho's own tooltips is shown (Settings › General › Other tooltips; all by default).</summary>
    private bool TipPart(string part)=>TooltipParts.Shown(configuration.TooltipPartsHidden,part);
    // The list is replaced whole at each change: the tooltips' caches compare it by reference and are built again.
    private void SetTipPart(string part,bool shown)=>configuration.TooltipPartsHidden=TooltipParts.With(configuration.TooltipPartsHidden,part,shown);

    /// <summary>X-684 (0.1696): each part of Tensho's own tooltips by kind (the item tooltip keeps its own category), one setting per part of
    /// UI/TooltipParts.All, written out so that the settings' search finds each one.</summary>
    private void DrawTooltipPartSettings()
    {
        ImGui.TextDisabled(localization.Get("Settings.TipPartsHelp"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindPlace"));
        BoolSetting("Settings.TipPlaceRegion","Settings.TipPartTip",()=>TipPart("Place.Region"),value=>SetTipPart("Place.Region",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Place.Region"));
        BoolSetting("Settings.TipPlaceCoordinates","Settings.TipPartTip",()=>TipPart("Place.Coordinates"),value=>SetTipPart("Place.Coordinates",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Place.Coordinates"));
        BoolSetting("Settings.TipPlaceAetheryte","Settings.TipPartTip",()=>TipPart("Place.Aetheryte"),value=>SetTipPart("Place.Aetheryte",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Place.Aetheryte"));
        BoolSetting("Settings.TipPlaceWeather","Settings.TipPartTip",()=>TipPart("Place.Weather"),value=>SetTipPart("Place.Weather",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Place.Weather"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindQuest"));
        BoolSetting("Settings.TipQuestCategory","Settings.TipPartTip",()=>TipPart("Quest.Category"),value=>SetTipPart("Quest.Category",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Quest.Category"));
        BoolSetting("Settings.TipQuestLevel","Settings.TipPartTip",()=>TipPart("Quest.Level"),value=>SetTipPart("Quest.Level",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Quest.Level"));
        BoolSetting("Settings.TipQuestZone","Settings.TipPartTip",()=>TipPart("Quest.Zone"),value=>SetTipPart("Quest.Zone",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Quest.Zone"));
        BoolSetting("Settings.TipQuestExpansion","Settings.TipPartTip",()=>TipPart("Quest.Expansion"),value=>SetTipPart("Quest.Expansion",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Quest.Expansion"));
        BoolSetting("Settings.TipQuestProgress","Settings.TipPartTip",()=>TipPart("Quest.Progress"),value=>SetTipPart("Quest.Progress",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Quest.Progress"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindMonster"));
        BoolSetting("Settings.TipMonsterLevel","Settings.TipPartTip",()=>TipPart("Monster.Level"),value=>SetTipPart("Monster.Level",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Monster.Level"));
        BoolSetting("Settings.TipMonsterZone","Settings.TipPartTip",()=>TipPart("Monster.Zone"),value=>SetTipPart("Monster.Zone",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Monster.Zone"));
        BoolSetting("Settings.TipMonsterNotes","Settings.TipPartTip",()=>TipPart("Monster.Notes"),value=>SetTipPart("Monster.Notes",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Monster.Notes"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindAction"));
        BoolSetting("Settings.TipActionDetail","Settings.TipPartTip",()=>TipPart("Action.Detail"),value=>SetTipPart("Action.Detail",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Action.Detail"));
        BoolSetting("Settings.TipActionDescription","Settings.TipPartTip",()=>TipPart("Action.Description"),value=>SetTipPart("Action.Description",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Action.Description"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindFate"));
        BoolSetting("Settings.TipFateKind","Settings.TipPartTip",()=>TipPart("Fate.Kind"),value=>SetTipPart("Fate.Kind",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Fate.Kind"));
        BoolSetting("Settings.TipFatePlace","Settings.TipPartTip",()=>TipPart("Fate.Place"),value=>SetTipPart("Fate.Place",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Fate.Place"));
        BoolSetting("Settings.TipFateNow","Settings.TipPartTip",()=>TipPart("Fate.Now"),value=>SetTipPart("Fate.Now",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Fate.Now"));
        BoolSetting("Settings.TipFateDescription","Settings.TipPartTip",()=>TipPart("Fate.Description"),value=>SetTipPart("Fate.Description",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Fate.Description"));
        BoolSetting("Settings.TipFateParticipants","Settings.TipPartTip",()=>TipPart("Fate.Participants"),value=>SetTipPart("Fate.Participants",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Fate.Participants"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindNpc"));
        BoolSetting("Settings.TipNpcPlaces","Settings.TipPartTip",()=>TipPart("Npc.Places"),value=>SetTipPart("Npc.Places",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Npc.Places"));
        BoolSetting("Settings.TipNpcShops","Settings.TipPartTip",()=>TipPart("Npc.Shops"),value=>SetTipPart("Npc.Shops",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Npc.Shops"));
        BoolSetting("Settings.TipNpcNotes","Settings.TipPartTip",()=>TipPart("Npc.Notes"),value=>SetTipPart("Npc.Notes",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Npc.Notes"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindDuty"));
        BoolSetting("Settings.TipDutyKind","Settings.TipPartTip",()=>TipPart("Duty.Kind"),value=>SetTipPart("Duty.Kind",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Duty.Kind"));
        BoolSetting("Settings.TipDutyZone","Settings.TipPartTip",()=>TipPart("Duty.Zone"),value=>SetTipPart("Duty.Zone",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Duty.Zone"));
        BoolSetting("Settings.TipDutyStatus","Settings.TipPartTip",()=>TipPart("Duty.Status"),value=>SetTipPart("Duty.Status",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Duty.Status"));
        BoolSetting("Settings.TipDutyUnlock","Settings.TipPartTip",()=>TipPart("Duty.Unlock"),value=>SetTipPart("Duty.Unlock",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Duty.Unlock"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindRecipe"));
        BoolSetting("Settings.TipRecipeStats","Settings.TipPartTip",()=>TipPart("Recipe.Stats"),value=>SetTipPart("Recipe.Stats",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Recipe.Stats"));
        BoolSetting("Settings.TipRecipeMinimum","Settings.TipPartTip",()=>TipPart("Recipe.Minimum"),value=>SetTipPart("Recipe.Minimum",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Recipe.Minimum"));
        BoolSetting("Settings.TipRecipeFacts","Settings.TipPartTip",()=>TipPart("Recipe.Facts"),value=>SetTipPart("Recipe.Facts",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Recipe.Facts"));
        BoolSetting("Settings.TipRecipeCrafted","Settings.TipPartTip",()=>TipPart("Recipe.Crafted"),value=>SetTipPart("Recipe.Crafted",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Recipe.Crafted"));
        BoolSetting("Settings.TipRecipeIngredients","Settings.TipPartTip",()=>TipPart("Recipe.Ingredients"),value=>SetTipPart("Recipe.Ingredients",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Recipe.Ingredients"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindPlayer"));
        BoolSetting("Settings.TipPlayerJob","Settings.TipPartTip",()=>TipPart("Player.Job"),value=>SetTipPart("Player.Job",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Player.Job"));
        UiComponents.SubTitle(localization.Get("Settings.TipKindGathering"));
        BoolSetting("Settings.TipGatheringDetail","Settings.TipPartTip",()=>TipPart("Gathering.Detail"),value=>SetTipPart("Gathering.Detail",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Gathering.Detail"));
        BoolSetting("Settings.TipGatheringPlace","Settings.TipPartTip",()=>TipPart("Gathering.Place"),value=>SetTipPart("Gathering.Place",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Gathering.Place"));
        BoolSetting("Settings.TipGatheringOpening","Settings.TipPartTip",()=>TipPart("Gathering.Opening"),value=>SetTipPart("Gathering.Opening",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Gathering.Opening"));
        BoolSetting("Settings.TipGatheringItems","Settings.TipPartTip",()=>TipPart("Gathering.Items"),value=>SetTipPart("Gathering.Items",value),c=>TooltipParts.Shown(c.TooltipPartsHidden,"Gathering.Items"));
    }
}
