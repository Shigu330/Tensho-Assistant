using System.Text.Json;

// 0.1357 (X-376, X-356 review, D-160): the English and French texts say the same thing. The labels a help text cites are the
// real labels of the other keys, the French « Disponible : » prefix that English never had is gone, and the public documents
// carry both languages (SECURITY's French network table, ROADMAP's English decisions, CHANGELOG's English header).
internal static class EnFrMeaning1357Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string,string> Flat(string code)
    {
        var values=new Dictionary<string,string>();
        void Walk(JsonElement node,string prefix)
        {
            foreach(var property in node.EnumerateObject())
            {
                if(property.Value.ValueKind==JsonValueKind.Object)Walk(property.Value,prefix+property.Name+".");
                else if(property.Value.ValueKind==JsonValueKind.String)values[prefix+property.Name]=property.Value.GetString()!;
            }
        }
        using var document=JsonDocument.Parse(File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{code}.json")));
        Walk(document.RootElement,"");
        return values;
    }

    private static void Quotes(Dictionary<string,string> texts,string key,string labelKey)
    {
        var label=texts[labelKey];
        Check(texts[key].Contains($"« {label} »"),$"{key} cites « {label} » ({labelKey})");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("En/fr meaning 1357: help texts cite the real labels, in both languages",() =>
        {
            foreach(var code in new[]{"en","fr"})
            {
                var texts=Flat(code);
                Quotes(texts,"SettingTips.CacheQuests","QuestsBrowser.Refresh");
                Quotes(texts,"ListPanels.OriginTip","ListPanels.FinishedOnly");
                Quotes(texts,"Reminders.DiscordRowTip","Discord.Reminders");
                Quotes(texts,"Reminders.DiscordRowTip","Discord.Voyages");
                Quotes(texts,"Help.AnswerDesktopInstall","Advanced.DesktopAutoUpdate");
            }
        }),
        ("En/fr meaning 1357: no French « Disponible : » prefix the English never had",() =>
        {
            var fr=Flat("fr");
            var found=fr.Where(pair => pair.Value.StartsWith("Disponible : ")).Select(pair => pair.Key).ToList();
            Check(found.Count==0,"« Disponible : » in "+string.Join(", ",found.Take(5)));
        }),
        ("En/fr meaning 1357: SECURITY's French table, ROADMAP's English decisions and CHANGELOG's header speak their language",() =>
        {
            string Read(string path) => File.ReadAllText(Path.GetFullPath(path));
            var security=Read("SECURITY.md");
            var french=security[security.IndexOf("<!-- docgen:network-fr -->",StringComparison.Ordinal)..];
            Check(french.Contains("**Ce dépôt** :") && french.Contains("**Webhook Discord** :") && !french.Contains("**This repository**"),"French network table names its rows in French");
            Check(security.Contains("« Report a problem or help »") && security.Contains("« Signaler un problème ou aider »"),"SECURITY cites the real Help label");
            var roadmap=Read("docs/project/ROADMAP.md");
            var englishDecisions=roadmap[roadmap.IndexOf("<!-- gen:decisions:en -->",StringComparison.Ordinal)..roadmap.IndexOf("<!-- /gen:decisions:en -->",StringComparison.Ordinal)];
            Check(!englishDecisions.Contains("relais") && !englishDecisions.Contains("supposée"),"ROADMAP's English decisions are in English");
            Check(Read("CHANGELOG.md").Contains("Summary per version, in French then `### English`"),"CHANGELOG's header has its English half");
        }),
    ];
}
