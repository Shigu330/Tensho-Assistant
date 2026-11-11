// X-122, D-112 (0.1043): every deletion of saved data takes two clicks, through MainWindow.DeleteButton (UI.DeleteConfirm).
internal static class DeleteConfirm1043Tests
{
    // label key, file: the deletions of saved data, each drawn by DeleteButton.
    private static readonly (string Key,string File)[] Deletions =
    [
        ("Alarms.Delete","Alarms"),("CraftLists.Delete","CraftLists"),("CraftLists.Remove","CraftLists"),("CraftPresets.Remove","CraftPresets"),
        ("Garden.Remove","Garden"),("Gear.ClearPlan","Gear"),("HuntTrain.Clear","HuntTrain"),("MapsUi.Remove","MapBookmarks"),
        ("MapsUi.Remove","MapLocalLists"),("PersonalTasks.Remove","PersonalTasks"),("Places.Remove","PlaceDirectory"),
        ("SortUi.DeleteTemplate","SortTemplates"),("SortUi.DeleteZone","SortRules"),("TriadDecks.Delete","TriadDecks"),
        ("Wishlist.RemoveShort","Wishlist"),("WorldClocks.Remove","WorldClocks"),("Discord.Clear","Discord"),("CraftPresets.Remove","Quests"),
        ("TriadUi.Remove","TripleTriad"),("MacroUi.Remove","MacroChain"),("Gear.PlanDelete","GearPlanSets"),
    ];

    internal static readonly (string Name, Action Run)[] Cases =
    [
        ("Delete confirm 1043: first click arms, second confirms, other button or late click re-arms",() =>
        {
            var confirm=new TenshoAssistant.UI.DeleteConfirm();
            Check(!confirm.Click(7,1000),"first click only arms");
            Check(confirm.IsArmed(7,1000) && !confirm.IsArmed(8,1000),"only the clicked button is armed");
            Check(confirm.Click(7,2000),"second click within the window deletes");
            Check(!confirm.IsArmed(7,2000),"disarmed after the deletion");
            Check(!confirm.Click(7,3000) && !confirm.Click(8,3100),"another button takes the arming");
            Check(confirm.Click(8,3200),"that button confirms");
            Check(!confirm.Click(9,10_000) && !confirm.Click(9,10_000+TenshoAssistant.UI.DeleteConfirm.WindowMs+1),"a late second click arms again");
            Check(confirm.Click(9,10_000+TenshoAssistant.UI.DeleteConfirm.WindowMs+2),"then confirms");
            Check(!new TenshoAssistant.UI.DeleteConfirm().Click(0,0),"id 0 never confirms on its first click");
        }),
        ("Delete confirm 1043: every deletion of saved data goes through DeleteButton, no Ctrl+click left",() =>
        {
            foreach(var (key,file) in Deletions)
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/MainWindow.{file}.cs"));
                Check(text.Contains($"DeleteButton(\"{key}\""),$"{file}: {key} drawn by DeleteButton");
                Check(!text.Contains($"Button(localization.Get(\"{key}\")"),$"{file}: {key} not drawn as a plain button");
                Check(!text.Contains($"localization.Get(\"{key}\")) && ImGui.GetIO().KeyCtrl"),$"{file}: no Ctrl+click deletion");
            }
            var component=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/DeleteButton.cs"));
            Check(component.Contains("return clicked && deleteConfirm.Click(id,now);"),"the component confirms through DeleteConfirm");
            foreach(var language in new[]{"en","fr"})
            {
                var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json"));
                Check(text.Contains("\"DeleteConfirm\":"),$"{language}: armed label present");
                Check(!text.Contains("without confirmation") && !text.Contains("sans confirmation"),$"{language}: no tip still says « without confirmation »");
                Check(!text.Contains("Ctrl + click: delete") && !text.Contains("Ctrl + click: clear") && !text.Contains("Ctrl + click: remove")
                    && !text.Contains("Ctrl + clic : supprime") && !text.Contains("Ctrl + clic : efface") && !text.Contains("Ctrl + clic : retire"),$"{language}: no Ctrl+click deletion tip");
            }
        }),
    ];

    private static void Check(bool condition,string message)
    {
        if(!condition)throw new Exception(message);
    }
}
