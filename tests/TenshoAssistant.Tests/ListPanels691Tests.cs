// W-05 (0.691): the on-screen lists of a workshop project and of a preparation.
internal static class ListPanels691Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("List panels: both are off by default, created with the others, and their settings are on their pages",() =>
        {
            var configuration=new TenshoAssistant.Configuration();
            Check(!configuration.WorkshopPanel && !configuration.PreparationPanel,"off by default");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("new(this,WorkshopListPanel),new(this,PreparationListPanel)"),"created");
            Check(Read("MainWindow.WorkshopProjects.cs").Contains("BoolSetting(\"FreeCompany.WorkshopPanel\""),"workshop setting");
            Check(Read("MainWindow.CraftPlan.cs").Contains("BoolSetting(\"PlanUi.Panel\""),"preparation setting");
        }),
        ("List panels: the crafting list has its own, with its setting on its tab, and the island's board does not hide a panel",() =>
        {
            Check(Read("MainWindow.CraftLists.cs").Contains("BoolSetting(\"CraftLists.Panel\""),"setting");
            Check(Read("MainWindow.TrackerWindow.cs").Contains("new(this,CraftListListPanel)"),"created");
            Check(!new TenshoAssistant.Configuration().CraftListPanel,"off by default");
            Check(TenshoAssistant.UI.GameWindows.Ignored("MJIHud"),"the board");
            Check(TenshoAssistant.UI.GameWindows.Ignored("JobHudRRP0") && TenshoAssistant.UI.GameWindows.Ignored("JobHudRRP1"),"the job gauges, read in the log");
            Check(!TenshoAssistant.UI.GameWindows.Ignored("MJIAnimalManagement"),"the pasture window stays a window");
        }),
        ("List panels: shown means shown unless the player chooses, and unread stocks are not shown as zero",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"ListPanelsHiddenInCombat").Count==4,"combat, by choice");
            var settings=new TenshoAssistant.Configuration();
            Check(!settings.ListPanelsHideInCombat && !settings.ListPanelsHideBehindGame,"never hidden by default");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"\(\)=>configuration\.ListPanelsHideBehindGame\);").Count==3,"behind the game, by choice");
            Check(Read("PanelWindow.cs").Contains("panel.StepsAside?.Invoke() != false && lastPosition"),"the window honours it");
            Check(page.Contains("if(!stock.HasObservation)ImGui.TextDisabled("),"unknown is not zero");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"RefreshRecipeHoldingsCache\(\);").Count==3,"stocks refreshed");
        }),
    ];
}
