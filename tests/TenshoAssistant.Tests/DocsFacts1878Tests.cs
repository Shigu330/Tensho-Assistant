using System.Text.RegularExpressions;

// X-568 (audit of 17/11, part B): public documents that said things no longer true. The in-game tests are done in small batches, not in
// one campaign at the end; D-88 is closed; DEC-032 and DEC-047 were replaced (DEC-046, the addition of 2026-11-11 to DEC-038) without saying
// so; PUBLISHING named one NuGet package of three; the discoveries probe read Discoveries.json, gone since the base moved to SQLite.
internal static class DocsFacts1878Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Flat(string path)=>Regex.Replace(Read(path),@"\s+"," ");
    private static (string En,string Fr) Halves(string path)
    {
        var text=Read(path);
        var half=text.IndexOf("\n---\n",StringComparison.Ordinal);
        Check(half>0,path+" has two halves");
        return (text[..half],text[half..]);
    }
    private static string Dec(string half,string id,bool french)
    {
        var start=half.IndexOf((french ? "DEC-ID: ":"ID: ")+id+"\n",StringComparison.Ordinal);
        Check(start>=0,id+" is in the half");
        var end=half.IndexOf("\n\n",start,StringComparison.Ordinal);
        return end<0 ? half[start..]:half[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("X-568: no public document plans the in-game tests as one campaign at the end",() =>
        {
            foreach(var path in new[]{"docs/project/GLOBAL-PLAN.md","docs/project/PUBLISHING.md","docs/project/ROADMAP.md"})
            {
                var text=Flat(path);
                foreach(var words in new[]{"one campaign, as late","in one campaign, as late","une seule campagne","guided final campaign","campagne guidée de fin","guided in-game campaign","campagne guidée en jeu"})
                    Check(!text.Contains(words,StringComparison.OrdinalIgnoreCase),$"{path}: « {words} »");
            }
            Check(Flat("docs/project/GLOBAL-PLAN.md").Contains("small batches of tests at the end of each block of work"),"the phase 6 line says small batches");
        }),
        ("X-568: ROADMAP no longer sends to D-88, closed",() =>
            Check(!Read("docs/project/ROADMAP.md").Contains("D-88"),"ROADMAP: D-88")),
        ("X-568: DEC-032 and DEC-047 say they were replaced, in both halves",() =>
        {
            var (en,fr)=Halves("docs/project/DECISIONS.md");
            foreach(var id in new[]{"DEC-032","DEC-047"})
            {
                Check(Regex.IsMatch(Dec(en,id,false),@"\nReferences: .*replaced by DEC-0"),id+": « replaced by » in English");
                Check(Regex.IsMatch(Dec(fr,id,true),@"\nReferences: .*remplac[ée]e? par DEC-0"),id+": « remplacé(e) par » in French");
            }
        }),
        ("X-568: PUBLISHING names the three NuGet packages of the plugin, in both halves",() =>
        {
            var packages=Regex.Matches(Read("src/TenshoAssistant/TenshoAssistant.csproj"),@"<PackageReference Include=""([^""]+)"" Version=""([^""]+)""").Select(m=>(m.Groups[1].Value,m.Groups[2].Value)).ToArray();
            Check(packages.Length==3,"three packages in the csproj");
            var (en,fr)=Halves("docs/project/PUBLISHING.md");
            foreach(var (name,version) in packages)
                foreach(var half in new[]{en,fr})Check(half.Contains($"`{name}` {version}"),$"PUBLISHING: {name} {version}");
            Check(!Read("docs/project/PUBLISHING.md").Contains("one NuGet package") && !Read("docs/project/PUBLISHING.md").Contains("un paquet NuGet"),"no « one package »");
        }),
        ("X-568: DiscoveryCheckProbe reads Discoveries.db read-only, and the tools' README says so in both halves",() =>
        {
            var probe=Read("tools/DiscoveryCheckProbe/Program.cs");
            Check(probe.Contains("\"Discoveries.db\"") && probe.Contains("SqliteOpenMode.ReadOnly") && !probe.Contains("\"Discoveries.json\""),"the probe opens the base read-only");
            var (en,fr)=Halves("tools/README.md");
            Check(en.Contains("`Discoveries.db`, opened read-only") && fr.Contains("`Discoveries.db`, ouvert en lecture seule"),"README in both halves");
            Check(!Read("tools/README.md").Contains("(`Discoveries.json`)"),"README: no Discoveries.json");
        }),
    ];
}
