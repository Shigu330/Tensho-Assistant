// 0.1265 (X-156, Crafting): the crafting pages say waiting and old data as everywhere, and tell what is read in game (measured) from
// what Tensho computes (estimate): the live read, the projection of the next actions, a simulated sequence with actions left to chance.
internal static class CraftStates1265Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft states 1265: the live read is measured, or old data after two seconds; the projection is an estimate",() =>
        {
            var live=Read("MainWindow.CraftLive.cs");
            Check(live.Contains("OriginLine(fresh ? UI.UiOrigin.Measured:UI.UiOrigin.Old,observedLine);"),"live read: measured or old, with its time");
            Check(live.Contains("TimeSpan.FromSeconds(2)"),"the two seconds of the read are kept");
            var projection=Read("MainWindow.CraftProjection.cs");
            Check(projection.Contains("OriginLine(UI.UiOrigin.Estimated,localization.Get(\"CraftProjectionUi.SortHelp\"))"),"projection table: an estimate");
            Check(projection.Contains("StateLine(UI.UiState.Stale,localization.Get(\"CraftProjectionUi.Old\"))"),"projection on an old read: old data, and nothing projected");
            Check(projection.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"CraftProjectionUi.SuggestLoading\"))"),"suggestion: waiting");
            var simulation=Read("MainWindow.CraftSimulation.cs");
            Check(simulation.Contains("OriginLine(UI.UiOrigin.Estimated,localization.Get(\"CraftSimulation.RandomAssumed\"))"),"a sequence with actions left to chance: an estimate");
            Check(simulation.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"SimulationHelp.Loading\"))"),"simulation: waiting for the actions");
        }),
        ("Craft states 1265: chain, crafts from stock, solver and log say waiting and old data as everywhere",() =>
        {
            var chain=Read("MainWindow.CraftChain.cs");
            Check(chain.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"ChainUi.Loading\"))"),"chain: waiting");
            Check(chain.Contains("StateLine(UI.UiState.Stale,localization.Get(\"ChainUi.Stale\"))") && chain.Contains("StateLine(UI.UiState.Stale,localization.Get(\"ChainUi.ChoicesChanged\"))"),"chain: stocks or choices changed since: old data");
            var stock=Read("MainWindow.CraftFromStock.cs");
            Check(stock.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"RecipeUi.Loading\"))") && stock.Contains("StateLine(UI.UiState.Stale,localization.Get(\"CraftStock.Stale\"))"),"from stock: waiting, old data");
            Check(Read("MainWindow.CraftSolver.cs").Contains("StateLine(UI.UiState.Waiting,localization.Get(\"CraftSearch.Loading\"))"),"solver: waiting");
            var log=Read("MainWindow.CraftLog.cs");
            Check(log.Contains("StateLine(UI.UiState.Waiting,localization.Get(\"CraftLogUi.ActionsLoading\"))") && log.Contains("UiComponents.SubTitle(title);"),"log: waiting; its sections are sub-titles");
        }),
        ("Craft states 1265: the crafting tables are the common table",() =>
        {
            foreach(var (file,id) in new[]{("MainWindow.CraftLists.cs","CraftListIngredients"),("MainWindow.CraftPlan.cs","CraftPlanMaterials"),("MainWindow.CraftProjection.cs","LiveCraftProjection"),("MainWindow.CraftSimulation.cs","CraftSequence")})
            {
                var text=Read(file);
                Check(text.Contains("BeginDataTable(\""+id+"\"") && !text.Contains("ImGui.BeginTable(\""+id+"\""),file+": "+id);
            }
        }),
    ];
}
