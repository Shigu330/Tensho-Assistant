using System.Text.Json;

// 0.1012 (PLAN-SYSTEME 4.2): the shared discoveries published by the repository carry the day only; CC0-1.0 stated where players agree to it.
internal static class DiscoveriesPrivacy1012Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Source(string path)=>File.ReadAllText(Path.GetFullPath(path));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discoveries privacy 1012: every published fact has First and Last at 00:00 UTC",() =>
        {
            var files=Directory.GetFiles(Path.GetFullPath("community-discoveries/players"),"*.json").ToList();
            var facts=0;
            // X-357 (0.1349): the common facts are the lines of the v3 set (the v2 discoveries.json is gone).
            foreach(var (file,line) in RepositoryCommon.FactLines())
            {
                using var document=JsonDocument.Parse(line);
                facts++;
                foreach(var name in new[]{"First","Last"})
                {
                    var text=document.RootElement.GetProperty(name).GetString()!;
                    Check(text.EndsWith("T00:00:00+00:00",StringComparison.Ordinal),$"v3/{file} {name} {text[..10]} keeps a time");
                }
            }
            foreach(var file in files)
            {
                using var document=JsonDocument.Parse(File.ReadAllText(file));
                foreach(var fact in document.RootElement.GetProperty("Facts").EnumerateArray())
                {
                    facts++;
                    foreach(var name in new[]{"First","Last"})
                    {
                        var text=fact.GetProperty(name).GetString()!;
                        Check(text.EndsWith("T00:00:00+00:00",StringComparison.Ordinal),$"{Path.GetFileName(file)} {name} {text[..10]} keeps a time");
                    }
                }
            }
            Check(facts>0,"facts read");
        }),
        ("Discoveries privacy 1012: the merge tool checks the time as sent, then keeps the day, in sync and in rebuild",() =>
        {
            var tool=Source("tools/discoveries-merge.py");
            Check(tool.Contains("if not isinstance(f,dict) or not valid(f) or not valid(daily(dict(f))) or player and not sighted(f):refused+=1;continue") && tool.Contains("g=capped(daily(dict(f)));g[\"Seen\"]=0"),"sync");
            Check(tool.Contains("kept=[daily(f) for f in kept]"),"rebuild");
            Check(tool.Contains("QUEST_ASKED_FIXED_DAY=datetime(2026,11,9,tzinfo=timezone.utc)"),"quest rule by day");
        }),
        ("Discoveries privacy 1012: CC0-1.0 in the folder's README (both languages), the consent text (en, fr) and the player's issue",() =>
        {
            var readme=Source("community-discoveries/README.md");
            Check(readme.Contains("**Licence.** The shared discoveries") && readme.Contains("**Licence.** Les découvertes partagées") && readme.Contains("CC0-1.0"),"README");
            foreach(var lang in new[]{"en","fr"})
                Check(Source($"src/TenshoAssistant/Localization/Languages/{lang}.json").Contains("Settings › Applications. ") && Source($"src/TenshoAssistant/Localization/Languages/{lang}.json").Contains("CC0-1.0 ("),lang);
            Check(Source("src/TenshoAssistant/Discoveries/GitHubExchange.cs").Contains("- [x] I share these discoveries under CC0-1.0"),"issue");
            Check(Source("SECURITY.md").Contains("CC0-1.0, which the warning") && Source("SECURITY.md").Contains("jour, sous CC0-1.0"),"SECURITY");
        }),
    ];
}
