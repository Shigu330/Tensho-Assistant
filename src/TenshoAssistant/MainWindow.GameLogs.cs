using Dalamud.Bindings.ImGui;
using TenshoAssistant.Craft;
using TenshoAssistant.Items;

namespace TenshoAssistant;

// 0.405 (user request): open the game's Crafting log on a recipe or its Gathering log on an item, from Tensho (item menu, Gathering page,
// Crafting page), on click only (Items/GameLogOpener).
internal sealed partial class MainWindow
{
    private void OpenGameRecipe(RecipeDefinition recipe)
    {
        // 0.430 (user remark): no level check — the game's log shows recipes above the job's level too (level brackets); it decides.
        gameLogRequest=(true,recipe.RecipeId,DateTimeOffset.UtcNow.AddSeconds(2));ProcessGameLogRequest();
    }
    // 0.429 (user remark): an item made by several jobs opened the first recipe, maybe of a job below its level. The job worn first, then
    // a job at the recipe's level (levels read with the gear sets), then the lowest recipe level.
    private RecipeDefinition PreferredRecipe(IReadOnlyList<RecipeDefinition> recipes)
    {
        GearsetSource?.Want();
        var levels=GearsetSource?.State?.JobLevels;var worn=characterStats.State.JobId;
        return recipes.OrderBy(recipe=>recipe.CraftTypeId+8==worn ? 0:1)
            .ThenBy(recipe=>levels is not null && levels.TryGetValue(recipe.CraftTypeId+8,out var level) && level>=recipe.Level ? 0:1)
            .ThenBy(recipe=>recipe.Level).ThenBy(recipe=>recipe.RecipeId).First();
    }
    private void OpenGameGatherable(uint itemId){gameLogRequest=(false,itemId,DateTimeOffset.UtcNow.AddSeconds(2));ProcessGameLogRequest();}
    // 0.429 (user report): an open log is closed first, then opened on the entry on a later frame, 2 s at most.
    private (bool Recipe,uint Id,DateTimeOffset Until)? gameLogRequest;
    /// <summary>Called every frame (also with the window closed): one step of the pending game log request.</summary>
    internal void ProcessGameLogRequest()
    {
        if(gameLogRequest is not { } request)return;
        if(DateTimeOffset.UtcNow>request.Until){gameLogRequest=null;saveError=localization.Get("GameLogs.Unavailable");return;}
        var result=request.Recipe ? GameLogOpener.Recipe(request.Id,error=>log.Warning(error,"The game's crafting log could not be opened."))
            :GameLogOpener.Gatherable(request.Id,error=>log.Warning(error,"The game's gathering log could not be opened."));
        if(result==GameLogOpen.Closing)return;
        gameLogRequest=null;
        if(result==GameLogOpen.Opened)CloseForGameWindow();
        else saveError=localization.Get("GameLogs.Unavailable");
    }
    // 0.426 (user request): Tensho covers the screen, so it closes once a game window it opened is shown (setting CloseOnGameWindow, on
    // by default); a game window already open is opened on the entry, not toggled shut (the game's « open on » functions).
    private void CloseForGameWindow(){if(configuration.CloseOnGameWindow)IsOpen=false;}

    // Item menu entries: one per recipe (several crafters can make the item) and the gathering log when it is gathered (not fished).
    private void DrawGameLogEntries(ItemDefinition item)
    {
        foreach(var recipe in recipeCatalog.State.Catalog?.ForResult(item.Id) ?? [])
            if(ImGui.Selectable(localization.Format("GameLogs.OpenRecipe",recipe.CraftType)+$"##GameRecipe{recipe.RecipeId}"))OpenGameRecipe(recipe);
        if(gatheringCatalog.State.Catalog?.ForItem(item.Id).Any(source=>!source.IsFishing)==true
           && ImGui.Selectable(localization.Get("GameLogs.OpenGathering")+$"##GameGathering{item.Id}"))OpenGameGatherable(item.Id);
    }
}
