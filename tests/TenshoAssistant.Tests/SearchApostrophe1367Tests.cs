using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Help;
using TenshoAssistant.Items;
using TenshoAssistant.Lexicon;

// X-389 (0.1367): « d'Ehcatl » typed with any apostrophe finds the same names (real names of the Item sheet, French), in the shared
// search helper, the item list, the quick search and the lexicon; and no search site compares a typed search without it any more.
internal static class SearchApostrophe1367Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    // Item#8090 and Item#8568, French (docs/evidence/0.1367-search-apostrophes.md).
    private const string Feathers = "poignée de plumes d'Ehcatl", Gloves = "paire de gants d'Ehcatl";
    private static readonly string[] Typed = ["d'Ehcatl", "d\u2019Ehcatl", "d\u2018ehcatl", "D\u02BCEHCATL", "d\u00B4Ehcatl", "d`ehcatl", "d\u2032Ehcatl", "d\uFF07Ehcatl"];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Search apostrophe 1367: every apostrophe form finds the real names", () =>
        {
            foreach (var typed in Typed)
            {
                Check(SearchText.Contains(Feathers, typed) && SearchText.Contains(Gloves, typed), "SearchText: " + typed);
                Check(SearchText.Contains(Feathers.Replace('\'', '\u2019'), typed), "a curly name, typed: " + typed);
            }
            Check(!SearchText.Contains(Feathers, "d'Eh catl") && !SearchText.Contains(null, "d'") && SearchText.Fold("aucune") == "aucune", "nothing else changes");
            Check(LexiconBoard.Fold("Plumes d\u2019Ehcatl") == LexiconBoard.Fold("plumes d'ehcatl"), "lexicon");
            Check(QuickSearch.Normalize("d\u2019Ehcatl") == QuickSearch.Normalize("d'Ehcatl"), "quick search");
        }),
        ("Search apostrophe 1367: the item list finds both items with any apostrophe", () =>
        {
            var source = new[] { new ItemDefinition(8090, Feathers, false), new ItemDefinition(8568, Gloves, false), new ItemDefinition(5, "plume de chocobo", false) };
            var catalog = ItemCatalog.Build(source, new ItemCatalogMetadata("fixture", "French", "v", 1));
            foreach (var typed in Typed)
            {
                var found = catalog.Search(typed).Select(item => item.Id).Order().ToArray();
                Check(found.SequenceEqual(new uint[] { 8090, 8568 }), "item list: " + typed + " found " + string.Join(",", found));
            }
        }),
        ("Search apostrophe 1367: no search site compares a typed search without the shared helper", () =>
        {
            // X-408 (0.1375): the 0.1367 scan saw only needles named search, query, filter, wanted or term; the stricter scan sees any.
            var left = SearchSites1375Tests.Left();
            Check(left.Count == 0, "search sites without SearchText: " + string.Join("; ", left.Take(5)));
        }),
    ];
}
