using TenshoAssistant.UI;
// X-76 P5 (0.928): targeted ventures from the game's sheets on the item hover; the community line only when the sheets give none.
internal static class Ventures928Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Ventures 928: RetainerTask (not random) to RetainerTaskNormal.Item, community fallback for exploration only",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Plugin.cs").Contains("if(task.IsRandom || task.Task.RowId==0 || normal.GetRowOrDefault(task.Task.RowId) is not { } row || row.Item.RowId==0)continue;"),"sheets");
            Check(Read("MainWindow.MarketHover.cs").Contains("if(ventureTasks.Count==0 && supplement.FromVenture(id))"),"fallback");
        }),
        ("0.570: retainer tasks one by one, with a bell, and one for the last back",() =>
        {
            var view=Read("MainWindow.Reminders.cs");
            Check(view.Contains("AlarmButton(\"Venture\"+name") && view.Contains("AlarmButton(\"VenturesAll\""),"two bells");
        }),
    ];
}
