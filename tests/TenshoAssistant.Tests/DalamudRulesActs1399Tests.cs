using System.Text.RegularExpressions;

// X-421 (0.1399, review of 0.1390 to 0.1397, I2): every function by which Tensho acts on the game stays in the files that rule 1 of the Dalamud
// table names (and rule 12 for the macros), in both halves of PUBLISHING.md: a new call elsewhere fails here until it is checked against the
// rules and its row changes. Completes DalamudRules1392Tests, whose list knew only the alarms, the hunt call, the teleport and the title.
internal static class DalamudRulesActs1399Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    // function → (files that call it, rules whose rows must name those files)
    private static readonly (string Token, string[] Files, int[] Rules)[] Acts =
    [
        ("EquipGearset(", ["Gear/GameGearsetReader.Dalamud.cs"], [1]),
        ("AgentTryon.TryOn(", ["Items/TryOn.Dalamud.cs"], [1]),
        ("ReplaceMacroLines", ["Macros/MacroWriter.Dalamud.cs"], [1, 12]),
        ("ExecuteMacro", ["Macros/MacroChainBridge.Dalamud.cs"], [1, 12]),
        ("ItemOrderModuleSorterItemEntry", ["Inventory/InventoryVisualSortBridge.Dalamud.cs"], [1]),
        ("ProcessChatBoxEntry", ["Alarms/AlarmService.Dalamud.cs", "Maps/HuntAnnouncer.Dalamud.cs"], [1]),
        ("SendTitleIdUpdate", ["Collections/TitleChanger.Dalamud.cs"], [1]),
        // X-425 M1 (0.1408): the teleport to an aetheryte, named by rule 1's text since 0.1399, its file now in its list too.
        ("telepo->Teleport(", ["Maps/MapLiveReader.Dalamud.cs"], [1]),
    ];

    private static IEnumerable<(string Name, string Text)> Sources()
    {
        var root = Path.GetFullPath("src/TenshoAssistant");
        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)));
    }

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Dalamud rules acts 1399: each function that acts on the game is only in the files named", () =>
        {
            var sources = Sources().ToList();
            foreach (var (token, files, _) in Acts)
            {
                var found = sources.Where(source => source.Text.Contains(token, StringComparison.Ordinal)).Select(source => source.Name).Order().ToArray();
                Check(found.SequenceEqual(files.Order()), token + ": in " + string.Join(", ", found) + " (check the rules, then PUBLISHING.md and this list)");
            }
        }),
        ("Dalamud rules acts 1399: rules 1 and 12 name these files in both halves", () =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/PUBLISHING.md")).Replace("\r\n", "\n");
            foreach (var heading in new[] { "## Dalamud rules, one by one", "## Règles de Dalamud, une par une" })
            {
                var rows = DalamudRules1392Tests.Rows(text, heading);
                foreach (var (token, files, rules) in Acts)
                    foreach (var rule in rules)
                        foreach (var file in files)
                            Check(rows.TryGetValue(rule, out var cells) && cells[2].Contains("`src/TenshoAssistant/" + file + "`", StringComparison.Ordinal),
                                heading + " rule " + rule + ": names " + file + " (" + token + ")");
            }
        }),
        ("Dalamud rules acts 1399: rule 5 says the seller's retainer id is read and not kept, as the market reader does", () =>
        {
            var text = File.ReadAllText(Path.GetFullPath("docs/project/PUBLISHING.md")).Replace("\r\n", "\n");
            var en = DalamudRules1392Tests.Rows(text, "## Dalamud rules, one by one")[5][1];
            var fr = DalamudRules1392Tests.Rows(text, "## Règles de Dalamud, une par une")[5][1];
            var reader = File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Market/SeenPriceReader.Dalamud.cs"));
            Check(reader.Contains("listing.RetainerId", StringComparison.Ordinal), "the reader still compares the seller's retainer id");
            Check(en.Contains("never kept", StringComparison.Ordinal) && !en.Contains("without retainer", StringComparison.Ordinal), "rule 5 (en): " + en);
            Check(fr.Contains("jamais gardé", StringComparison.Ordinal) && !fr.Contains("sans identifiant de servant", StringComparison.Ordinal), "rule 5 (fr): " + fr);
        }),
    ];
}
