using TenshoAssistant.Beastmaster;

// 0.1574 (X-551, the user's request of 17/11 at the Crucible: « the panel should look more like the game's »): the icon the game draws before
// a damage type or a weakness comes from the XBMElement sheet's own text (« <icon(56)>Feu »), never from a table of ours. Real data: the
// sheet's nine rows in French, read with their macro codes (tools/XbmProbe, PROBE_MACRO=1, game 2026.11.03).
internal static class BeastElementIcons1574Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static readonly (uint Row, string Macro, string Name, uint Icon)[] Sheet =
    [
        (1, "<icon(56)>Feu", "Feu", 56), (2, "<icon(58)>Vent", "Vent", 58), (3, "<icon(59)>Terre", "Terre", 59), (4, "<icon(60)>Foudre", "Foudre", 60),
        (5, "<icon(57)>Glace", "Glace", 57), (6, "<icon(61)>Eau", "Eau", 61), (7, "<icon(185)>Contondant", "Contondant", 185),
        (8, "<icon(186)>Perforant", "Perforant", 186), (9, "<icon(187)>Tranchant", "Tranchant", 187),
    ];

    private static object?[] Blank(int count, params (int Column, object Value)[] set)
    {
        var cells = new object?[count];
        foreach (var (column, value) in set) cells[column] = value;
        return cells;
    }

    // The least the catalog needs: one beast (a piercing attack), one board with one enemy weak to fire, and the nine elements.
    private static Dictionary<string, IReadOnlyList<XbmRow>> Sheets() => new()
    {
        ["XBMPet"] = [new(0, 0, Blank(27)), new(1, 0, Blank(27, (0, 47), (1, (byte)1), (2, (byte)42), (4, 242001u), (5, (ushort)45187), (9, ""), (10, "")))],
        ["XBMPetParamGrow"] = [new(42, 0, [(ushort)10, (ushort)11, (ushort)12, (ushort)13, (ushort)14])],
        ["XBMBattleDetail"] = [new(1, 0, Blank(11, (0, 14531u), (8, (byte)1), (10, 242501u)))],
        ["XBMBattleDetailAction"] = [new(1, 0, [46866u, (byte)1, (byte)6, 0u])],
        ["XBMContent"] = [new(0, 0, Blank(37)), new(1, 0, Blank(37, (0, 1088u), (1, (byte)10), (3, (byte)5)))],
        ["XBMContentBattle"] = [new(1, 0, [(ushort)1])],
        ["XBMElement"] = [new(0, 0, [""]), .. Sheet.Select(row => new XbmRow(row.Row, 0, [row.Name]))],
        ["XBMScoreBonus"] = [new(0, 0, [false, "Noble I", "Three down"])],
        ["XBMActionEffectType"] = [new(0, 0, ["???"])],
        ["XBMActionTarget"] = [new(0, 0, [""])],
    };

    private static BeastCatalog Build(Func<uint, string>? macro) => BeastCatalog.Build(Sheets(), _ => "cu sith", _ => "Therians", _ => 2, _ => "enemy", _ => "action", _ => ("Board", 30, 25), elementMacro: macro);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast element icons 1574: the font icon a sheet's text starts with; none when the text starts with none", () =>
        {
            foreach (var row in Sheet) Check(BeastCatalog.IconOfMacro(row.Macro) == row.Icon, row.Macro);
            Check(BeastCatalog.IconOfMacro("Neutre") == 0 && BeastCatalog.IconOfMacro("") == 0 && BeastCatalog.IconOfMacro(null) == 0, "a word alone has no icon");
            Check(BeastCatalog.IconOfMacro("Feu<icon(56)>") == 0, "only the icon before the word is the element's");
            Check(BeastCatalog.IconOfMacro("<icon(x)>Feu") == 0 && BeastCatalog.IconOfMacro("<icon(56") == 0 && BeastCatalog.IconOfMacro("<icon()>") == 0 && BeastCatalog.IconOfMacro("<icon(-1)>") == 0, "a broken macro gives no icon, never another one");
        }),
        ("Beast element icons 1574: the catalog keeps each element's icon by the name the beasts and enemies carry; nothing without the sheet's raw text", () =>
        {
            var macros = Sheet.ToDictionary(row => row.Row, row => row.Macro);
            var catalog = Build(row => macros.GetValueOrDefault(row, ""));
            Check(catalog.ElementIcons.Count == 9, "nine elements: " + catalog.ElementIcons.Count);
            foreach (var row in Sheet) Check(catalog.ElementIcons.GetValueOrDefault(row.Name) == row.Icon, row.Name);
            var beast = catalog.Beasts.Single();
            Check(beast.AutoAttack == "Perforant" && catalog.ElementIcons[beast.AutoAttack] == 186, "a beast's type of damage finds its icon: " + beast.AutoAttack);
            var enemy = catalog.Boards.Single().Battles.Single().Enemies.Single();
            Check(enemy.Weakness == "Feu" && catalog.ElementIcons[enemy.Weakness] == 56, "an enemy's weakness finds its icon: " + enemy.Weakness);
            Check(Build(null).ElementIcons.Count == 0, "no raw text (tests, the desktop application): no icon, the words stay");
            // A row whose text lost its icon after a game update: that element alone has none.
            var partial = Build(row => row == 1 ? "Feu" : macros.GetValueOrDefault(row, ""));
            Check(!partial.ElementIcons.ContainsKey("Feu") && partial.ElementIcons.Count == 8, "an element without an icon in the sheet has none");
        }),
        ("Beast element icons 1574: wiring: the loader gives the sheet's macro text, the panel draws the game's icon before the word and falls back to the word alone", () =>
        {
            var loader = Read("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("if (row.ReadColumn(0) is ReadOnlySeString name) elementMacros[row.RowId] = name.ToMacroString();") && loader.Contains("elementMacro: row => elementMacros.GetValueOrDefault(row, \"\"));"), "the loader reads XBMElement with its macro codes");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("if (inDesktopApp || beastFontIconFailed || element.Length == 0 || !catalog.ElementIcons.TryGetValue(element, out var icon)) return;"), "no icon: nothing drawn, the word stays");
            Check(page.Contains("catch (Exception error)\n        {\n            beastFontIconFailed = true;"), "a renderer failure is caught once and the words stay");
            Check(page.Contains("TargetDrawList = ImGui.GetWindowDrawList(), ScreenOffset = ImGui.GetCursorScreenPos(), Font = ImGui.GetFont(), FontSize = ImGui.GetFontSize()"), "Dalamud's rule for a given draw list: list, place, font and size given");
            var fight = page[page.IndexOf("private void DrawBeastFightNow(", StringComparison.Ordinal)..page.IndexOf("private string BeastCallText(", StringComparison.Ordinal)];
            Check(fight.Contains("DrawBeastElementIcon(catalog, enemy.Weakness);") && fight.Contains("DrawBeastElementIcon(catalog, action.Damage);") && fight.Contains("DrawBeastElementIcon(catalog, beast.AutoAttack);"),
                "the fight shown: weakness, each action's type of damage and each beast's attack carry the game's icon");
            Check(fight.Contains("if (number > 0) ImGui.Separator();"), "a line between enemies, as in the game's board window");
            var plan = page[page.IndexOf("private void DrawBeastPlanLines(", StringComparison.Ordinal)..];
            Check(plan.Contains("ImGui.TableNextColumn(); DrawBeastElementIcon(catalog, beast.AutoAttack);"), "the team's rows: the type of damage after its icon");
            Check(!page.Contains("SendAction") && !page.Contains("UseAction"), "nothing is sent to the game (R03.3)");
        }),
    ];
}
