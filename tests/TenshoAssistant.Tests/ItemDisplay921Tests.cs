using TenshoAssistant.UI;
// X-76 2b H8 (0.921): one game icon helper (no ItemDefinition with id 0, no raw icon drawn as a widget in the pages), one fallback key for an
// unnamed item, the tool button in the components.
internal static class ItemDisplay921Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static string Read(string name)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+name)).Replace("\r\n","\n");
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Item display 921: DrawGameIcon everywhere, BankUi.Item only, ToolButton in the components",() =>
        {
            var found=new List<string>();
            foreach(var path in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories))
            {
                if(path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))continue;
                var text=File.ReadAllText(path);var name=Path.GetFileName(path);
                if(System.Text.RegularExpressions.Regex.IsMatch(text,@"ItemDefinition\(0,"))found.Add(name+" icon");
                foreach(var key in new[]{"MonsterUi.Item\"","HoverUi.Item\"","CraftPresetMessages.Item\"","CraftLogUi.Item\""})if(text.Contains(key))found.Add(name+" "+key);
            }
            Check(found.Count==0,"left: "+string.Join(", ",found.Take(8)));
            var cell=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/UI/Components/ItemCell.cs"));
            Check(cell.Contains("private bool DrawGameIcon(uint iconId,Vector2 size,bool keepSpace=true)"),"helper");
            Check(File.Exists(Path.GetFullPath("src/TenshoAssistant/UI/Components/ToolButton.cs")) && !File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs")).Contains("private bool ToolButton("),"tool button moved");
        }),
        ("0.562: an item's English name is shown only on the setting, and only when it differs",() =>
        {
            Check(!new TenshoAssistant.Configuration().ShowEnglishItemName,"off by default");
            Check(Read("MainWindow.MarketHover.cs").Contains("configuration.ShowEnglishItemName && item is {EnglishName.Length:>0} && !string.Equals(item.EnglishName,item.Name"),"rule");
            Check(Read("MainWindow.ItemSheet.cs").Contains("DrawEnglishItemName(item);"),"sheet");
        }),
    ];
}
