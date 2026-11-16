using System.Text.Json;

// X-519 (review of 0.1497, I1): texts shown to players send them to a place of the Settings page (« Settings › Panels beside the game ›
// Crafting »). Since the tabs were cut into categories (X-449), such a path must name a tab, then (when it goes on) a category of that tab
// or a quoted label that exists. Here every « Settings › … » of en.json and « Paramètres › … » of fr.json is read against the page.
internal static class SettingsPaths1498Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static Dictionary<string,string> Flat(string language)
    {
        var text=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{language}.json")).TrimStart('﻿');
        using var document=JsonDocument.Parse(text);
        var flat=new Dictionary<string,string>();
        void walk(JsonElement element,string path)
        {
            if(element.ValueKind==JsonValueKind.Object)foreach(var property in element.EnumerateObject())walk(property.Value,path.Length==0 ? property.Name:path+"."+property.Name);
            else if(element.ValueKind==JsonValueKind.String)flat[path]=element.GetString() ?? "";
        }
        walk(document.RootElement,"");
        return flat;
    }

    /// <summary>The wrong paths of one language: (key, what is wrong), with the number of paths read.</summary>
    internal static (List<string> Wrong,int Read) WrongPaths(string language,IReadOnlyDictionary<string,string> texts)
    {
        var flat=Flat(language);
        var word=language=="fr" ? "Paramètres › ":"Settings › ";
        var categories=SettingsTabs999Tests.CategoryList();
        var tabs=categories.Select(category=>category.Tab).Distinct()
            .Select(tab=>(Tab:tab,Label:flat["SettingsSections."+tab])).OrderByDescending(tab=>tab.Label.Length).ToArray();
        var values=flat.Values.ToHashSet();
        var wrong=new List<string>();
        var read=0;
        foreach(var (key,text) in texts)
        {
            for(var at=text.IndexOf(word,StringComparison.Ordinal);at>=0;at=text.IndexOf(word,at+1,StringComparison.Ordinal))
            {
                var before=text[..at];
                if(before.EndsWith("Dalamud › ",StringComparison.Ordinal) || before.EndsWith("github.com › ",StringComparison.Ordinal))continue;
                read++;
                var rest=text[(at+word.Length)..];
                var tab=tabs.FirstOrDefault(tab=>rest.StartsWith(tab.Label,StringComparison.Ordinal));
                if(tab.Tab is null){wrong.Add($"{key}: no tab « {rest[..Math.Min(40,rest.Length)]} »");continue;}
                rest=rest[tab.Label.Length..];
                if(!rest.StartsWith(" › ",StringComparison.Ordinal))continue;
                rest=rest[3..];
                var labels=categories.Where(category=>category.Tab==tab.Tab).Select(category=>flat["SettingsCategories."+category.Id]).OrderByDescending(label=>label.Length);
                if(labels.Any(label=>rest.StartsWith(label,StringComparison.Ordinal) && (rest.Length==label.Length || !char.IsLetter(rest[label.Length]))))continue;
                var end=rest.IndexOf(" »",StringComparison.Ordinal);
                if(rest.StartsWith("« ",StringComparison.Ordinal) && end>2 && values.Contains(rest[2..end]))continue;
                wrong.Add($"{key}: « {rest[..Math.Min(40,rest.Length)]} » is not a category of {tab.Tab} nor an existing label");
            }
        }
        return (wrong,read);
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Settings paths 1498: every « Settings › tab › category » of the texts names a tab and one of its categories, in English and French",() =>
        {
            foreach(var language in new[]{"en","fr"})
            {
                var (wrong,read)=WrongPaths(language,Flat(language));
                Check(read>=40,$"{language}: only {read} paths read: the reading broke");
                Check(wrong.Count==0,$"{language}: {wrong.Count} wrong path(s): "+string.Join(" ; ",wrong));
            }
        }),
        ("Settings paths 1498: the check refuses the paths X-519 found (old category, removed title, French name of a category cut short)",() =>
        {
            var en=SettingsPaths1498Tests.WrongPaths("en",new Dictionary<string,string>
            {
                ["a"]="(Settings › Panels beside the game › Crafting).",
                ["b"]="Enable it in Settings › Panels beside the game › « Gathering & fishing · observation ».",
                ["c"]="Settings › Panels beside the game › Crafting macros.",
                ["d"]="Settings › Data and sources.",
            });
            Check(en.Read==4 && en.Wrong.Count==2 && en.Wrong[0].StartsWith("a:") && en.Wrong[1].StartsWith("b:"),"en: "+string.Join(" ; ",en.Wrong));
            var fr=SettingsPaths1498Tests.WrongPaths("fr",new Dictionary<string,string>{["a"]="Paramètres › Panneaux à côté du jeu › Artisanat.",["b"]="Paramètres › Panneaux à côté du jeu › Aide à l'artisanat."});
            Check(fr.Wrong.Count==1 && fr.Wrong[0].StartsWith("a:"),"fr: "+string.Join(" ; ",fr.Wrong));
        }),
    ];
}
