using System.Globalization;
using System.Text.RegularExpressions;
using TenshoAssistant;
using TenshoAssistant.Craft;
using TenshoAssistant.Gathering;
using TenshoAssistant.Gear;
using TenshoAssistant.Help;
using TenshoAssistant.Items;

// X-408 (0.1375, review of 0.1365 to 0.1374, I1): every search typed by the player goes through SearchText, whatever its needle is
// called (word, token, text…) and the CompareInfo.IndexOf ones too. The scan sees every case-insensitive Contains / IndexOf /
// StartsWith of the plugin's sources and every CompareInfo.IndexOf; a needle written as a string literal is never typed; the few
// other comparisons that are not a typed search are listed below, each with its reason, and the list has no stale line.
internal static class SearchSites1375Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static readonly Regex Plain = new(@"(?<!SearchText)\.(Contains|IndexOf|StartsWith)\(((?:[^,()]|\((?:[^()]|\([^()]*\))*\))+),\s*StringComparison\.\w*IgnoreCase\s*\)");
    private static readonly Regex Compare = new(@"CompareInfo\s*\.\s*IndexOf\(");

    /// <summary>Comparisons that are not a search typed by the player: file, needle without spaces, reason.</summary>
    private static readonly (string File, string Needle, string Why)[] NotTyped =
    [
        ("MainWindow.MapView.cs", "quest.Name", "the game's quest name against the game's marker tooltip"),
        ("DeepDungeonBoard.cs", "dungeon", "a deep dungeon's name from the game against the game's duty names"),
        ("LoadMemory.cs", "plugin", "the plugin's own name among the load contexts"),
        ("WikiDutyGuide.cs", "word", "Tensho's fixed list of wiki headings to skip"),
        ("Collectables.cs", "cut", "a name cut by the game's window against the game's names"),
        ("ProblemReport.cs", "prefix", "Tensho's own report prefix"),
        ("LocalLink.cs", "root+Path.DirectorySeparatorChar", "a folder path"),
        ("MacroChainService.cs", "Marker+\"\"", "Tensho's fixed macro marker"),
        ("BeastWikiIndex.cs", "name", "Tensho's fixed wiki column headers"),
        ("BlueSpellWikiIndex.cs", "name", "Tensho's fixed wiki column headers"),
        ("NpcVendorCatalog.cs", "key", "a layer key of the game's quest scripts"),
    ];

    private static IEnumerable<string> Sources()
    {
        foreach (var file in Directory.EnumerateFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories))
        {
            var sep = Path.DirectorySeparatorChar;
            if (file.Contains(sep + "obj" + sep) || file.Contains(sep + "bin" + sep)) continue;
            yield return file;
        }
    }

    private static List<(string File, string Needle)> Found()
    {
        var found = new List<(string, string)>();
        foreach (var file in Sources())
        {
            var name = Path.GetFileName(file);
            if (name == "SearchText.cs") continue;
            var text = File.ReadAllText(file);
            foreach (Match match in Plain.Matches(text))
            {
                var needle = Regex.Replace(match.Groups[2].Value, @"\s+", "");
                if (needle.StartsWith('"')) continue;
                found.Add((name, needle));
            }
            foreach (Match match in Compare.Matches(text)) found.Add((name, "CompareInfo.IndexOf"));
        }
        return found;
    }

    /// <summary>Sites comparing a typed search without SearchText (empty when all go through it).</summary>
    public static List<string> Left() =>
        Found().Where(site => !NotTyped.Any(not => not.File == site.File && not.Needle == site.Needle))
            .Select(site => site.File + ": " + site.Needle).ToList();

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Search sites 1375: every comparison of the sources is a SearchText one or a listed non-search", () =>
        {
            var left = Left();
            Check(left.Count == 0, "search sites without SearchText: " + string.Join("; ", left));
        }),
        ("Search sites 1375: the scan sees word, token, text and CompareInfo needles", () =>
        {
            var sample = "a.Contains(word,StringComparison.CurrentCultureIgnoreCase) b.Contains(token, StringComparison.OrdinalIgnoreCase) "
                + "c.Contains(text.Trim(),StringComparison.InvariantCultureIgnoreCase) SearchText.Contains(d,e,StringComparison.OrdinalIgnoreCase)";
            var needles = Plain.Matches(sample).Select(match => Regex.Replace(match.Groups[2].Value, @"\s+", "")).ToArray();
            Check(needles.SequenceEqual(["word", "token", "text.Trim()"]), "needles: " + string.Join(",", needles));
            Check(Compare.IsMatch("CultureInfo.CurrentCulture.CompareInfo.IndexOf(a,b,CompareOptions.IgnoreCase)"), "CompareInfo");
        }),
        ("Search sites 1375: the list of non-searches has no stale line", () =>
        {
            var found = Found();
            var stale = NotTyped.Where(not => !found.Contains((not.File, not.Needle))).Select(not => not.File + ": " + not.Needle).ToArray();
            Check(stale.Length == 0, "stale: " + string.Join("; ", stale));
        }),
        ("Search sites 1375: the fixed searches find a name typed with a curly apostrophe", () =>
        {
            const string typed = "d\u2019ehcatl", name = "plume d'Ehcatl";
            var notes = new Dictionary<uint, ItemNote> { [1] = new ItemNote("à vendre", false, true) };
            Check(ItemNotes.Annotated(notes, AnnotatedFilter.All, typed, _ => name).Length == 1, "annotated items");
            Check(Collectables.Filter([new CollectableRule(1, name, 1, 1, 0, 0, 0, 0, 0, 0, false)], typed).Length == 1, "collectables");
            Check(ChocoboBarding.Filter([new BardingSet(1, "Barde", 0, 0, 0, 0, 1, 7)], typed, false, null, _ => name).Count == 1, "bardings");
            Check(HelpIndex.Search([new HelpTopic("k", "Page", "Une " + name, "", "")], typed).Count == 1, "help");
            Check(CraftPresets.View([new CraftPreset(Guid.NewGuid(), "Suite", 1, 1, 1, [], DateTimeOffset.UnixEpoch)], typed, CraftPresetSort.Saved, _ => name, _ => "").Length == 1, "craft presets");
            Check(SearchText.Contains("Égide d'Ehcatl", "egide d\u2019EHCATL", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, CultureInfo.InvariantCulture), "compare options");
            Check(!SearchText.Contains(null, "d'", CompareOptions.IgnoreCase), "null text");
        }),
    ];
}
