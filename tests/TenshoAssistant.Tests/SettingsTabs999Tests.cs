using System.Text.RegularExpressions;

// X-100 (0.999, DEC-048): the Settings tabs chosen by the user, and a search whose index stays in step with the code (the same scan as its generator).
internal static class SettingsTabs999Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string,string> Scan()
    {
        var bodies=new Dictionary<string,string>();
        foreach(var file in Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"),"MainWindow*.cs"))
        {
            var text=File.ReadAllText(file).Replace("\r\n","\n");
            foreach(Match m in Regex.Matches(text,@"\n    (?:private|internal|public)[^\n(]*?\b(\w+)\(([^)\n]*)\)\s*\n?\s*\{"))
            {
                var start=m.Index+m.Length;var next=Regex.Match(text[start..],@"\n    (?:private|internal|public|//|///)");
                var body=text.Substring(start,next.Success ? next.Index:text.Length-start);
                bodies[m.Groups[1].Value]=bodies.GetValueOrDefault(m.Groups[1].Value,"")+body;
            }
        }
        var seen=new Dictionary<string,string>();
        foreach(var (tab,names) in new[]{("General",new[]{"DrawGeneralSettings","DrawInterfaceSettings"}),("Panels",["DrawPanelSettings"]),("Map",["DrawMapSettingsBody"]),
            ("Tracking",["DrawTrackingSettings"]),("Data",["DrawDataSettings"]),("Advanced",["DrawAdvancedSettings"])})
        {
            var todo=new List<string>(names);var visited=new HashSet<string>();
            while(todo.Count>0)
            {
                var name=todo[0];todo.RemoveAt(0);
                if(!visited.Add(name) || !bodies.TryGetValue(name,out var body))continue;
                foreach(Match key in Regex.Matches(body,@"\b(?:BoolSetting|IntSetting|SliderSetting|ComboSetting|SettingBox)\(\s*""([\w.]+)"""))seen.TryAdd(key.Groups[1].Value,tab);
                todo.AddRange(Regex.Matches(body,@"\b(Draw\w+)\(").Select(call=>call.Groups[1].Value));
            }
        }
        return seen;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings tabs 999: the seven tabs in order",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Settings.cs"));
            var tabs=Regex.Matches(page,@"SettingsTab\(""(\w+)""").Select(match=>match.Groups[1].Value).ToArray();
            Check(tabs.SequenceEqual(["General","Panels","Map","Tracking","Data","Advanced","Debug"]),"tabs: "+string.Join(",",tabs));
        }),
        ("Settings tabs 999: the search index is the code's own list, every label translated",() =>
        {
            var scanned=Scan();
            var index=TenshoAssistant.SettingsIndex.Entries.ToDictionary(entry=>entry.Key,entry=>entry.Tab);
            Check(scanned.Count>100 && scanned.Count==index.Count && scanned.All(pair=>index.GetValueOrDefault(pair.Key)==pair.Value),
                $"index out of step: {scanned.Count} scanned, {index.Count} listed; missing {string.Join(",",scanned.Keys.Except(index.Keys).Take(5))}; extra {string.Join(",",index.Keys.Except(scanned.Keys).Take(5))}");
            var en=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json"));
            Check(index.Keys.All(key=>en.Contains($"\"{key.Split('.')[^1]}\"")),"labels");
        }),
    ];
}
