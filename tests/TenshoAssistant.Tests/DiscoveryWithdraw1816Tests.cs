using System.Text.Json;

// 0.1816 (X-803, user decision of 22/11 after D-220): a player withdrawn keeps their facts in the common set, without any name (an anonymous
// player file, no issue nor login). The behaviour (a player alone on a fact withdraws: the fact stays, counted as before, in a file without
// login nor issue; rebuilt again, nothing changes) is in the merge tool's selftest (tools/discoveries-merge.py --selftest, run before each
// version); here, the wiring and the texts in both languages.
internal static class DiscoveryWithdraw1816Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");
    private static string Key(string language,string section,string key)
    {
        using var document=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")));
        var root=document.RootElement;
        return Find(section.Length==0 ? root : root.GetProperty(section),key) ?? "";
    }
    // The first value of that key, at any depth (DiscoveriesExchange lives in a section of a section).
    private static string? Find(JsonElement node,string key)
    {
        if(node.ValueKind!=JsonValueKind.Object)return null;
        foreach(var part in node.EnumerateObject())
        {
            if(part.Name==key && part.Value.ValueKind==JsonValueKind.String)return part.Value.GetString();
            if(Find(part.Value,key) is { } found)return found;
        }
        return null;
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Discovery withdraw 1816: the tool keeps a withdrawn player's facts in an anonymous file, written after the common set",() =>
        {
            var tool=Read("tools/discoveries-merge.py");
            Check(tool.Contains("fresh_id=new_file_id({v[\"File\"] for v in links.values()});new=fresh_id+PLAYER_SUFFIX",StringComparison.Ordinal),"a random name, never the issue (X-830: a new one)");
            Check(tool.Contains("pending={new:(fresh_id,facts)}",StringComparison.Ordinal) && tool.Contains("if drop:del links[drop];write_links(links,links_path)",StringComparison.Ordinal),"no login, written by rebuild after the common set; the private link forgotten (X-830)");
            foreach(var name in new[]{"X-803: withdrawn: the numbered file removed, every fact kept","X-803: the anonymous file holds the facts, without login nor issue","X-803: rebuilt again"})
                Check(tool.Contains(name,StringComparison.Ordinal),"the tool's behaviour case: "+name);
            Check(!tool.Contains("the facts no other player has gone",StringComparison.Ordinal),"the old case, its facts gone, is no more");
        }),
        ("Discovery withdraw 1816: said before the first share, in the help, in each issue and the README, in English and French",() =>
        {
            // X-820 M2 (review of 0.1819): « without your name » went further than what happens (the maintainer's withdraw, the issue's title, git history).
            const string en="What you share stays in the common base even if you stop sharing. On request (or once your issue is closed), the maintainer removes the link between your GitHub name and your discoveries; your issue's title and the repository's history still show that name.";
            const string fr="Ce que vous partagez reste dans la base commune même si vous arrêtez de partager. Sur demande (ou une fois votre ticket fermé), le mainteneur retire le lien entre votre nom GitHub et vos découvertes ; le titre de votre ticket et l'historique du dépôt montrent encore ce nom.";
            foreach(var key in new[]{"PublicWarning","ConnectTip","DisconnectTip","ExchangeHelp","DiscoveriesExchange"})
            {
                Check(Key("en","",key).EndsWith(en,StringComparison.Ordinal),"en "+key);
                Check(Key("fr","",key).EndsWith(fr,StringComparison.Ordinal),"fr "+key);
            }
            var exchange=Read("src/TenshoAssistant/Discoveries/GitHubExchange.cs");
            Check(exchange.Contains("What you share stays in the common base even if you stop sharing. Close this issue (or ask in it) and the maintainer removes your player file",StringComparison.Ordinal)
                && exchange.Contains("Français : ce que vous partagez reste dans la base commune même si vous arrêtez de partager. Fermez ce ticket",StringComparison.Ordinal),"the issue, both languages");
            var readme=Read("community-discoveries/README.md");
            Check(readme.Contains("**Withdrawing.** What you share stays in the common base even if you stop sharing.",StringComparison.Ordinal)
                && readme.Contains("**Se retirer.** Ce que vous partagez reste dans la base commune même si vous arrêtez de partager.",StringComparison.Ordinal)
                && readme.Contains("« Discoveries: <login> »",StringComparison.Ordinal),"the README, both halves, the issue's title said");
            var decisions=Read("docs/project/DECISIONS.md");
            Check(decisions.Contains("Completed in 0.1816 (X-803",StringComparison.Ordinal) && decisions.Contains("Complétée en 0.1816 (X-803",StringComparison.Ordinal),"DEC-052 completed in both halves");
        }),
    ];
}
