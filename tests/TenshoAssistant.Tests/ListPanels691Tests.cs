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
        ("List panels: hidden in combat, and unread stocks are not shown as zero",() =>
        {
            var page=Read("MainWindow.ListPanels.cs");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"InCombat:true").Count==2,"combat");
            Check(page.Contains("if(!stock.HasObservation)ImGui.TextDisabled("),"unknown is not zero");
            Check(System.Text.RegularExpressions.Regex.Matches(page,@"RefreshRecipeHoldingsCache\(\);").Count==2,"stocks refreshed");
        }),
    ];
}
