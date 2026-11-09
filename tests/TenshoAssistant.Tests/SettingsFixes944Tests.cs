// X-76 step 3 (0.944): the nine setting defects confirmed by the inventory (docs/project/SETTINGS.md) stay fixed.
internal static class SettingsFixes944Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings fixes 944: regen reader gate, buff wait reset, crystal tip, one quest area control, inventory written",() =>
        {
            string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
            Check(Read("Plugin.cs").Contains("()=>configuration.GpTickBar || configuration.GatheringPanel,log)"),"regen reader follows the gathering help");
            Check(Read("MainWindow.Settings.cs").Contains("value=>search.BuffExtraWait=value==search.ExtraWait ? null:value,0,3,search.ExtraWait);"),"buff wait reset");
            Check(Read("MainWindow.ListPanels.cs").Contains("BoolSetting(\"ListPanels.Crystal\",\"ListPanels.CrystalTip\","),"crystal tip");
            Check(!Read("MainWindow.MapView.cs").Contains("SettingBox(\"MapView.ShowQuestAreas\""),"one control");
            Check(File.ReadAllText(Path.GetFullPath("docs/project/SETTINGS.md")).Contains("| `FishingPanel` |"),"inventory");
        }),
    ];
}
