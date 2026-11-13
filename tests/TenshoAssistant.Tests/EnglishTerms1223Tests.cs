// 0.1223 (X-260, first-player review of 12/11, G9, G10, D7, D8): the English interface reads like the game's English client.
// Official terms from the English sheets (Addon 6888 « Submersible », 646 « Free Company », 10890 « Data Center », 2770 « Grand Company Rank »,
// HowTo 152 « Allied Society Quests », Completion 4041 « Island Sanctuary »), no wording calqued on French, British spelling outside game
// names, and setting tooltips « On: … / Off: … ». A ratchet on the values of en.json: the replaced forms never come back.
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class EnglishTerms1223Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    // Every string value of en.json with its dotted key; URLs, paths, file names and {tokens} removed (they keep their own spelling).
    private static List<(string Key,string Value)> Values()
    {
        var found = new List<(string,string)>();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Localization/Languages/en.json")));
        void Walk(JsonElement element,string path)
        {
            if (element.ValueKind == JsonValueKind.Object) foreach (var property in element.EnumerateObject()) Walk(property.Value,path.Length == 0 ? property.Name : path + "." + property.Name);
            else if (element.ValueKind == JsonValueKind.String)
                found.Add((path,Regex.Replace(element.GetString()!,@"https?://\S+|(?:[\w.<>-]+/)+[\w.<>-]+|[\w-]+\.(?:json|py|cs|md|csv|zip|txt|dll|ps1|sh)\b|\{[^}]*\}"," ")));
        }
        Walk(document.RootElement,"");
        return found;
    }

    private static void None(List<(string Key,string Value)> values,string pattern,string what,RegexOptions options = RegexOptions.None)
    {
        var hit = values.FirstOrDefault(entry => Regex.IsMatch(entry.Value,pattern,options));
        Check(hit.Key is null,what + " came back: " + hit.Key);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("English terms 1223: the game's official words, never the old ones",() =>
        {
            var values = Values();
            None(values,@"\b[Ss]ubmarines?\b","« submarine » (the game says submersible, Addon 6888)");
            None(values,@"\b[Ff]ree company\b","lowercase « free company » (Addon 646 « Free Company »)");
            None(values,@"\b[Dd]ata cent(?:re|er)","« data centre » or « data center » (Addon 10890 « Data Center »)");
            None(values,@"\btribal quests?\b","« tribal quests » (HowTo 152 « Allied Society Quests »)",RegexOptions.IgnoreCase);
            None(values,@"\b[Ii]sland sanctuary\b","lowercase « island sanctuary » (Completion 4041 « Island Sanctuary »)");
            Check(values.Where(entry => entry.Key.StartsWith("GrandCompany.",StringComparison.Ordinal)).All(entry => !Regex.IsMatch(entry.Value,@"\bgrades?\b",RegexOptions.IgnoreCase)),
                "a Grand Company « grade » (Addon 2770 « Grand Company Rank »)");
            None(values,@"Your grade and seals","the Grand Companies page purpose with « grade »");
            Check(values.Any(entry => entry.Value.Contains("Submersible",StringComparison.Ordinal)) && values.Any(entry => entry.Value.Contains("Free Company",StringComparison.Ordinal))
                  && values.Any(entry => entry.Value.Contains("Data Center",StringComparison.Ordinal)) && values.Any(entry => entry.Value.Contains("allied society quests",StringComparison.Ordinal))
                  && values.Any(entry => entry.Value.Contains("Island Sanctuary",StringComparison.Ordinal)),"the official forms are used");
            Check(values.Any(entry => entry.Value == "Favor") && values.Any(entry => entry.Value == "Defense") && values.Any(entry => entry.Value.Contains("Materia grade",StringComparison.Ordinal)),
                "game names keep the game's spelling (Addon 6572 « Favor », 257 « Defense ») and materia keeps its grades");
        }),
        ("English terms 1223: no wording calqued on French",() =>
        {
            var values = Values();
            foreach (var (pattern,what) in new[]
            {
                (@"\bthe job worn\b","« the job worn »"),(@"\bon your click\b","« on your click »"),(@"\bunder way\b","« under way »"),
                (@"\bRead again after\b","« Read again after »"),(@"^Read again$","the « Read again » button"),(@"\bread again in a moment\b","« read again in a moment »"),
                (@"\bretain(?:ed|s)\b","« retained »"),(@"\bpropos(?:e|es|ed|al)\b","« propose » meaning suggest"),
                (@"\b(?:its|this|full|item) sheet\b","« sheet » for an item's page"),(@"Find a word of the game","« Find a word of the game »"),
                (@"To get ready outside","« To get ready outside »"),(@"asks the server for nothing","« asks the server for nothing »"),
                (@"By relic version","« By relic version »"),(@"\bby themselves\b","« by themselves »"),
            })
                None(values,pattern,what);
            Check(values.Single(entry => entry.Key == "Relics.Stage").Value == "Stage","the relic « Version » is a stage, as the page says elsewhere");
        }),
        ("English terms 1223: British spelling outside game names",() =>
        {
            var values = Values();
            None(values,@"\bcatalogs?\b","« catalog » (catalogue)",RegexOptions.IgnoreCase);
            foreach (var word in new[] { "color","colors","favorite","favorites","favorable","armor","behavior","analyze","analyzed","customize","minimize","optimization","optimizes",
                                         "organization","prioritizes","unrecognized","authorization","serializes","modeled","unmodeled","localization" })
                None(values,@"\b" + word + @"\b","American « " + word + " »",RegexOptions.IgnoreCase);
        }),
        ("English terms 1223: setting tooltips say On / Off, not Ticked / Unticked",() =>
        {
            var values = Values();
            None(values,@"\b(?:Unt|T)icked: ","« Ticked: » or « Unticked: » (« Nothing ticked: » of a currency list is a list, not a setting)");
            None(values,@"Default: (?:un)?ticked","« Default: ticked »");
            None(values,@"» is (?:un)?ticked\b|\b(?:option|category)\b[^.]* is (?:un)?ticked\b|\bIt is ticked when\b","a setting « is ticked » or « is unticked » (a box of a list stays ticked)");
            Check(values.Count(entry => Regex.IsMatch(entry.Value,@"(?:^|\. )On: ") ) >= 15,"the On: tooltips are there");
        }),
    ];
}
