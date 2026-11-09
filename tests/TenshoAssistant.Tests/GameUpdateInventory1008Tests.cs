using System.Text.RegularExpressions;

// 0.1008: docs/evidence/0.1008-game-update-inventory.md lists what a game update can break; every native function, native singleton and game window
// read by name in the plugin's code must be in it, so that the list stays the one to check after an update.
internal static class GameUpdateInventory1008Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game update inventory 1008: every native function, singleton and window named in the code is listed",() =>
        {
            var doc=File.ReadAllText(Path.GetFullPath("docs/evidence/0.1008-game-update-inventory.md"));
            var code=string.Join("\n",Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"),"*.cs",SearchOption.AllDirectories)
                .Where(path=>!path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                .Select(File.ReadAllText));
            var names=new SortedSet<string>();
            foreach(Match match in Regex.Matches(code,@"\b([A-Za-z]+)\.MemberFunctionPointers\.([A-Za-z]+)"))names.Add(match.Groups[1].Value+"."+match.Groups[2].Value);
            foreach(Match match in Regex.Matches(code,@"\b([A-Z][A-Za-z]+)\.Instance\(\)"))names.Add(match.Groups[1].Value);
            foreach(Match match in Regex.Matches(code,@"GetAddonByName\(""([A-Za-z0-9_]+)"""))names.Add(match.Groups[1].Value);
            Check(names.Count>80,"names found: "+names.Count);
            var missing=names.Where(name=>!doc.Contains("`"+name+"`")).ToArray();
            Check(missing.Length==0,"not in the inventory: "+string.Join(", ",missing));
            Check(Regex.Matches(code,@"HookFromAddress<").Count==2 && doc.Contains("Two hooks only"),"hooks");
        }),
    ];
}
