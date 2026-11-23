using System.Text.RegularExpressions;
using TenshoAssistant.Help;

// X-850 (review 0.1852, I1): X-813 left links to a tab of another page outside the table: Triple Triad's « Npcs » and « Deck » in plain text
// (OpenTriadTab), its notebook (focusTriadNotebook, dropped after one frame), the crafting page's Simulation and Preparation (focusCraftPage).
// They are rows of Help/NavTargets.cs now; NavTargets1809Tests reads every row against the bars. Here: no page's tab is chosen by a flag of
// its own any more, except the collections' kind (an enum chosen at run time, kept until open), and every key passed to OpenLink is a row.
internal static class NavTargets1855Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static IEnumerable<(string File, string Code)> Sources() =>
        Directory.GetFiles(Path.GetFullPath("src/TenshoAssistant"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => (Path.GetFileName(path), File.ReadAllText(path).Replace("\r\n", "\n")));

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Nav targets 1855: Triple Triad's and the crafting page's tabs are rows of the table, followed by OpenLink", () =>
        {
            Check(NavTargets.Of("Triad.Npcs") == new NavTarget("TripleTriad", "Npcs") && NavTargets.Of("Triad.Deck") == new NavTarget("TripleTriad", "Deck")
                && NavTargets.Of("Triad.Notebook") == new NavTarget("TripleTriad", "Notebook"), "Triple Triad's rows");
            Check(NavTargets.Of("Craft.Simulation") == new NavTarget("CraftSimulation", "Simulation") && NavTargets.Of("Craft.Preparation") == new NavTarget("CraftSimulation", "Preparation"), "the crafting page's rows");
            Check(Read("src/TenshoAssistant/MainWindow.MapLocalLists.cs").Contains("()=>OpenLink(\"Triad.Npcs\")", StringComparison.Ordinal), "an NPC's Triple Triad tool");
            Check(Read("src/TenshoAssistant/MainWindow.Settings.cs").Contains("OpenLink(\"Triad.Deck\");", StringComparison.Ordinal), "the settings' Deck button");
            Check(Read("src/TenshoAssistant/MainWindow.TripleTriad.cs").Contains("OpenLink(\"Triad.Notebook\");selectedTriad=cardId;", StringComparison.Ordinal), "Show in Triple Triad");
            Check(Read("src/TenshoAssistant/MainWindow.CraftSimulation.cs").Contains("SelectItem(recipe.ResultItemId); OpenLink(\"Craft.Simulation\");", StringComparison.Ordinal)
                && Read("src/TenshoAssistant/MainWindow.CraftPlan.cs").Contains("OpenCraftSimulation(recipe);\n        OpenLink(\"Craft.Preparation\");", StringComparison.Ordinal), "the crafting page's links");
        }),
        ("Nav targets 1855: no tab chosen by a flag or a tab name in plain text; the collections' kind is the one jump left outside, kept until open", () =>
        {
            var flags = new Regex(@"\b(?:focusTriadNotebook|triadFocusTab|focusCraftPage|CraftPageTab|OpenTriadTab)\b");
            // A field focus…Tab, focus…Page or focus…Notebook set from a link is a tab chosen outside the table.
            var other = new Regex(@"\bfocus\w*(?:Tab|Page|Notebook)\s*=\s*(?!null|false)[\w.]+;", RegexOptions.IgnoreCase);
            foreach (var (file, code) in Sources())
            {
                Check(!flags.IsMatch(code), file + ": a tab chosen outside the table: " + flags.Match(code).Value);
                Check(!other.IsMatch(code), file + ": a focus flag set: " + other.Match(code).Value);
            }
            var collections = Read("src/TenshoAssistant/MainWindow.Collections.cs");
            Check(collections.Contains("if (focusCollectionKind == kind) focusCollectionKind = null; }, focusCollectionKind == kind);", StringComparison.Ordinal), "the collections' kind is kept until its tab is open");
            Check(Read("src/TenshoAssistant/Help/NavTargets.cs").Contains("One jump stays outside, on purpose", StringComparison.Ordinal), "the exception is written down");
        }),
    ];
}
