using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// System plan 4.19 (plan F3): the FAQ is one list (Help/Faq.cs) shown and searched on the Help page and written to docs/FAQ.md by tools/faq.py.
// A question without its English and French texts, a page that does not exist, or a docs/FAQ.md out of date does not pass.
internal static class FaqTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static Dictionary<string,string> HelpTexts(string language)
    {
        using var document=JsonDocument.Parse(Read($"src/TenshoAssistant/Localization/Languages/{language}.json").TrimStart('﻿'));
        return document.RootElement.GetProperty("Help").EnumerateObject().ToDictionary(property=>property.Name,property=>property.Value.GetString() ?? "");
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("FAQ: every question has its question and answer in English and French, once, and its page exists",() =>
        {
            Check(Faq.Entries.Count>=15,"the list holds the questions");
            Check(Faq.Entries.Select(entry=>entry.Key).Distinct().Count()==Faq.Entries.Count,"a key twice");
            var pages=Regex.Match(Read("src/TenshoAssistant/MainWindow.cs"),@"enum Page \{([^}]*)\}").Groups[1].Value
                .Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
            foreach(var language in new[]{"en","fr"})
            {
                var help=HelpTexts(language);
                foreach(var entry in Faq.Entries)
                {
                    Check(help.GetValueOrDefault("Question"+entry.Key) is { Length:>0 },$"{language}: Help.Question{entry.Key}");
                    Check(help.GetValueOrDefault("Answer"+entry.Key) is { Length:>0 },$"{language}: Help.Answer{entry.Key}");
                }
                foreach(var key in help.Keys.Where(key=>key.StartsWith("Question",StringComparison.Ordinal) || key.StartsWith("Answer",StringComparison.Ordinal)))
                    Check(Faq.Entries.Any(entry=>key=="Question"+entry.Key || key=="Answer"+entry.Key),$"{language}: Help.{key} is not in the list (Help/Faq.cs)");
            }
            foreach(var entry in Faq.Entries.Where(entry=>entry.Page is not null))Check(pages.Contains(entry.Page),$"{entry.Key}: no page {entry.Page}");
            Check(Faq.PageOf("Language")=="Settings" && Faq.PageOf("Marks") is null,"the page of an answer");
        }),
        ("FAQ: the Help page shows the list and searches it like the pages",() =>
        {
            var source=Read("src/TenshoAssistant/MainWindow.Help.cs");
            Check(source.Contains("Faq.Entries.Select(") && source.Contains("HelpIndex.Search(") && !source.Contains("new[] { \"EmptyPage\""),"the list, not questions written in the page");
            var en=HelpTexts("en");
            var topics=Faq.Entries.Select(entry=>new HelpTopic(entry.Key,en["Question"+entry.Key],en["Answer"+entry.Key],"","")).ToArray();
            Check(HelpIndex.Search(topics,"installer").Any(topic=>topic.Key=="Install"),"a word of an answer finds its question");
            Check(HelpIndex.Search(topics,"").Count==Faq.Entries.Count,"no query: every question");
        }),
        ("FAQ: docs/FAQ.md is written from the list, English then French, and is up to date",() =>
        {
            var page=Read("docs/FAQ.md");
            var marker=page.IndexOf("\nFrançais :\n",StringComparison.Ordinal);
            Check(page.StartsWith("<!-- Written by tools/faq.py",StringComparison.Ordinal) && marker>0,"written by tools/faq.py, with its French part");
            foreach(var (language,part) in new[]{("en",page[..marker]),("fr",page[marker..])})
            {
                var help=HelpTexts(language);
                var asked=Regex.Matches(part,@"^## (.+)$",RegexOptions.Multiline).Select(match=>match.Groups[1].Value).ToArray();
                Check(asked.SequenceEqual(Faq.Entries.Select(entry=>help["Question"+entry.Key])),$"{language}: the questions of docs/FAQ.md are those of the list, in its order (python -I -X utf8 tools/faq.py write)");
                foreach(var entry in Faq.Entries)Check(part.Contains("\n"+help["Answer"+entry.Key]+"\n"),$"{language}: the answer {entry.Key} of docs/FAQ.md is out of date (python -I -X utf8 tools/faq.py write)");
            }
            Check(Read("README.md").Contains("docs/FAQ.md") && Read(".github/ISSUE_TEMPLATE/config.yml").Contains("docs/FAQ.md")
                && Read(".github/ISSUE_TEMPLATE/config.yml").Contains("discussions/categories/q-a"),"the README and the ticket form send to the FAQ and to Q&A");
        }),
    ];
}
