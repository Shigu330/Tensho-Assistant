using System.Text.Json;
using System.Text.RegularExpressions;

// X-568 (audit of 17/11, B1): ARCHITECTURE.md had fallen behind the code: no Chat/, Link/ nor desktop application, the page groups of before
// X-601, the Settings tabs of before, the discoveries in Discoveries.json, the Beastmaster « without a panel », « Reset everything to
// default ». Read again against the code on 2026-11-27; these cases keep the parts that the code itself can check.
internal static class ArchitectureMap1879Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static (string En,string Fr) Halves()
    {
        var text=Read("docs/project/ARCHITECTURE.md");
        var half=text.IndexOf("\n---\n",StringComparison.Ordinal);
        Check(half>0,"ARCHITECTURE has two halves");
        return (text[..half],text[half..]);
    }
    private static string Label(string language,string section,string key)
    {
        using var json=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        return json.RootElement.GetProperty(section).GetProperty(key).GetString()!;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("X-568 B1: every code folder of the plugin is a row of the module map, in both halves",() =>
        {
            var (en,fr)=Halves();
            var folders=Directory.GetDirectories(Path.GetFullPath("src/TenshoAssistant")).Select(Path.GetFileName).Where(name=>name is not ("bin" or "obj" or "images")).ToArray();
            Check(folders.Contains("Chat") && folders.Contains("Link"),"the folders are read");
            foreach(var folder in folders)
                foreach(var (half,name) in new[]{(en,"English"),(fr,"French")})
                    Check(Regex.IsMatch(half,$@"^\|[^|\n]*`{folder}/`",RegexOptions.Multiline),$"ARCHITECTURE ({name}): the folder {folder}/ has no row in the module map");
        }),
        ("X-568 B1: the desktop application, the SQLite files and the chat command are described, in both halves",() =>
        {
            var (en,fr)=Halves();
            foreach(var half in new[]{en,fr})
            {
                Check(half.Contains("src/TenshoAssistant.Desktop") && half.Contains("TenshoData.db") && half.Contains("Discoveries/Discoveries.db") && half.Contains("Settings.json"),"desktop and files");
                Check(half.Contains("`chat hide`"),"the chat commands");
                Check(!half.Contains("`Discoveries.json`, `Shared/`") && !half.Contains("FileInventoryBankStore` : un fichier") && !half.Contains("FileInventoryBankStore`: one file"),"no files of before as the live ones");
                Check(!half.Contains("(DEC-032) |"),"no Beastmaster without a panel");
            }
            Check(!en.Contains("Reset everything to default") && en.Contains("« "+Label("en","Settings","ResetAll")+" »"),"the button's English name");
            Check(fr.Contains("« "+Label("fr","Settings","ResetAll")+" »"),"the button's French name");
        }),
        ("X-568 B1: the Settings tabs of the code, with their labels, are those of ARCHITECTURE, in both halves",() =>
        {
            var (en,fr)=Halves();
            var tabs=Regex.Matches(Read("src/TenshoAssistant/MainWindow.Settings.cs"),@"SettingsTab\(""(\w+)""").Select(m=>m.Groups[1].Value).Distinct().ToArray();
            Check(tabs.Length>=6 && tabs.Contains("Panels"),"the tabs are read");
            foreach(var tab in tabs)
            {
                Check(en.Contains(Label("en","SettingsSections",tab)),$"ARCHITECTURE (English): the Settings tab {tab}");
                Check(fr.Contains(Label("fr","SettingsSections",tab)),$"ARCHITECTURE (French): the Settings tab {tab}");
            }
            Check(en.Contains("checked against the code on 2026-11-27") && fr.Contains("relue contre le code le 2026-11-27"),"the date of the reading");
        }),
    ];
}
