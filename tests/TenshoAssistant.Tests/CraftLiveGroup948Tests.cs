// X-76 step 3 (0.948, user report): the craft window and its two parts form one block, the parts greyed while the window is off, on both pages.
internal static class CraftLiveGroup948Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Craft live group 948: one block, parts under the window, greyed when off, suggestion not worked out without its window",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            var live=Read("MainWindow.CraftLive.cs");var settings=Read("MainWindow.Settings.cs");
            Check(live.Contains("private void DrawCraftLiveSettings()") && live.Contains("var off=!configuration.CraftLivePanel;") && settings.Contains("DrawCraftLiveSettings();"),"one block");
            Check(System.Text.RegularExpressions.Regex.Matches(live+settings,"BoolSetting\\(\"CraftLiveUi.Suggest\"").Count==1,"drawn once in code");
            Check(Read("MainWindow.CraftProjection.cs").Contains("if(!configuration.CraftLivePanel || !configuration.CraftLiveSuggest"),"no work without the window");
        }),
    ];
}
