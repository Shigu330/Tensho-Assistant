using TenshoAssistant;

// X-76 E3 (0.937): every docs/evidence path the code cites exists (the agent's instructions rule 7), through renames and moves. E4: OnceSet keeps noting new events.
internal static class Evidence937Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Evidence 937: every cited evidence file exists",() =>
        {
            var cited=new Dictionary<string,string>();
            foreach(var folder in new[]{"src","tools"})
                foreach(var path in Directory.EnumerateFiles(Path.GetFullPath(folder),"*.cs",SearchOption.AllDirectories))
                {
                    var separator=Path.DirectorySeparatorChar;
                    if(path.Contains($"{separator}obj{separator}") || path.Contains($"{separator}bin{separator}"))continue;
                    foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path),@"docs/evidence/[A-Za-z0-9._\-/]+"))
                        cited.TryAdd(match.Value.TrimEnd('.',',',')',';',':'),Path.GetFileName(path));
                }
            Check(cited.Count>=150,"paths found: "+cited.Count);
            var missing=cited.Where(pair=>!File.Exists(Path.GetFullPath(pair.Key)) && !Directory.Exists(Path.GetFullPath(pair.Key))).Select(pair=>$"{pair.Key} ({pair.Value})").ToArray();
            Check(missing.Length==0,"missing evidence: "+string.Join(", ",missing.Take(8)));
        }),
        ("Once set 937: first time true, again false, oldest forgotten past the bound, clear",() =>
        {
            var set=new OnceSet<int>(3);
            Check(set.Add(1) && set.Add(2) && !set.Add(1) && set.Add(3) && set.Add(4),"adds");
            Check(set.Count==3 && set.Add(1) && !set.Add(4),"1 forgotten, 4 kept");
            set.Clear();Check(set.Count==0 && set.Add(4),"cleared");
            Check(!File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Discoveries/ItemDiscoveryWatcher.Dalamud.cs")).Contains("lootSeen.Count>=256"),"no silent stop");
        }),
    ];
}
