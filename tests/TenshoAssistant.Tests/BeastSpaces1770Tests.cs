using TenshoAssistant.Beastmaster;

// X-744 (0.1770, the user's idea I-026, second part): the board's spaces that are not fights (XBMContentStageEvent), the campsites' places
// (XBMContentCamp), the random spaces' options (XBMContentRandomStageEvent, XBMRandomStageEvent), the advice of which familiars to rest from
// the HP the team window shows (the game's guide, DescriptionString 1735), and the satiety (XBMPet column 3, the bestiary's « Satiété 0/3 »).
// Real rows of the game files (2026.11.03), boards 1 and 2. docs/evidence/0.1770-crucible-spaces.md.
internal static class BeastSpaces1770Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static XbmRow Row(uint row, ushort sub, params object?[] cells) => new(row, sub, cells);

    // XBMContentStageEvent boards 1 and 2 (step, kind, index, node), as the probe read them.
    private static readonly int[][] Board1 = [[0, 1, -1, -1], [1, 2, 1, 3], [2, 2, 2, 4], [2, 2, 3, 5], [3, 6, 0, 6], [3, 7, 0, 7], [4, 2, 4, 8], [5, 7, 1, 9], [6, 3, 5, 10], [7, 7, 2, 11], [7, 6, 1, 12], [8, 5, 0, 13], [9, 4, 0, 14]];
    private static readonly int[][] Board2 = [[0, 1, -1, -1], [1, 2, 1, 8], [2, 5, 0, 9], [3, 3, 2, 10], [3, 2, 3, 11], [4, 7, 0, 12], [4, 6, 0, 13], [5, 2, 4, 14], [6, 7, 1, 15], [7, 3, 5, 16], [8, 8, 0, 17], [8, 7, 2, 18], [9, 5, 1, 19], [10, 4, 0, 20]];

    private static IEnumerable<XbmRow> Stages() =>
        new[] { (Board: 1u, Rows: Board1), (Board: 2u, Rows: Board2) }.SelectMany(board => board.Rows.Select((cells, sub) => Row(board.Board, (ushort)sub, (sbyte)cells[0], (byte)cells[1], (sbyte)cells[2], (sbyte)cells[3])))
            .Prepend(Row(0, 0, (sbyte)-1, (byte)0, (sbyte)-1, (sbyte)-1));

    private static readonly XbmRow[] Camps = [Row(0, 0, (byte)0), Row(1, 0, (byte)2), Row(1, 1, (byte)2), Row(2, 0, (byte)2), Row(2, 1, (byte)3)];
    private static readonly XbmRow[] RandomOfBoard = [Row(0, 0, (ushort)0), Row(2, 0, (ushort)1)];
    private static readonly XbmRow[] Randoms = [Row(0, 0, (byte)0, (sbyte)-1), Row(1, 0, (byte)2, (sbyte)6), Row(1, 1, (byte)6, (sbyte)1)];

    private static BeastTeams.TeamMember Member(uint beast, int hp, int max) => new(beast, 5, hp, max);

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast spaces 1770: behaviour: the real boards 1 and 2: shops, campsites with their places, treasures, the random space's options, by step", () =>
        {
            var spaces = BeastSpaces.Read(Stages(), Camps, RandomOfBoard, Randoms);
            Check(spaces.Count == 2 && !spaces.ContainsKey(0), "two boards, row 0 left out");
            var one = spaces[1];
            Check(one.Select(space => (space.Step, space.Kind)).SequenceEqual([(3, 6), (3, 7), (5, 7), (7, 7), (7, 6), (8, 5)]), "board 1: no fight, no start, by step: " + string.Join(" ", one.Select(space => $"{space.Step}/{space.Kind}")));
            Check(one.Where(space => space.Kind == BeastSpaces.Campsite).Select(space => space.Places).SequenceEqual([2, 2]), "board 1's two campsites: 2 and 2 familiars (XBMContentCamp 1)");
            var two = spaces[2];
            var random = two.Single(space => space.Kind == BeastSpaces.Random);
            Check(random.Step == 8 && random.Options.Select(option => (option.Kind, option.Index, option.Places)).SequenceEqual([(BeastSpaces.Enemy, 6, 0), (BeastSpaces.Campsite, 1, 3)]), "board 2's random space: an enemy (battle 6) or the campsite 1 of 3 places");
            Check(two.Single(space => space.Kind == BeastSpaces.Campsite).Places == 2 && BeastSpaces.CampsitePlaces(random) == 3 && BeastSpaces.CampsitePlaces(two[0]) == 0, "board 2: its campsite 2, the random's campsite 3, a shop none");
            Check(two.Count(space => space.Kind == BeastSpaces.Shop) == 2 && two.Count(space => space.Kind == BeastSpaces.Treasure) == 3, "board 2: two shops, three treasures");
        }),
        ("Beast spaces 1770: behaviour: places or options not in the sheets stay unread (0, none), never guessed", () =>
        {
            var spaces = BeastSpaces.Read(Stages(), [Row(1, 0, (byte)2)], [], Randoms);
            Check(spaces[1].Where(space => space.Kind == BeastSpaces.Campsite).Select(space => space.Places).SequenceEqual([2, 0]), "the second campsite of board 1 has no row: 0 (not read)");
            Check(spaces[2].Single(space => space.Kind == BeastSpaces.Random).Options.Count == 0 && spaces[2].Single(space => space.Kind == BeastSpaces.Campsite).Places == 0, "board 2: the random space without its row: no option; its campsite without a row: 0");
            Check(BeastSpaces.Columns.Count == 4 && BeastSpaces.Columns["XBMContentStageEvent"] == 4 && BeastSpaces.Columns["XBMRandomStageEvent"] == 2, "the column counts checked before reading");
        }),
        ("Beast spaces 1770: behaviour: the campsite's advice: the hurt ones, lowest HP share first, at most its places; full, incapacitated and unread apart", () =>
        {
            var team = new[] { Member(1, 300, 600), Member(3, 100, 500), Member(4, 0, 400), Member(5, 450, 450), Member(6, -1, 0), Member(7, 560, 600), Member(1, 1, 600) };
            var two = BeastSpaces.Rest(team, 2);
            Check(two.Places == 2 && two.Rest.SequenceEqual([3u, 1u]), "two places: the beast at 20 % then the one at 50 %, not the one at 93 %: " + string.Join(",", two.Rest));
            Check(two.Full.SequenceEqual([5u]) && two.Down.SequenceEqual([4u]) && two.Unread.SequenceEqual([6u]), "full, incapacitated, not read: each apart, each beast once (the second line of beast 1 left out)");
            Check(BeastSpaces.Rest(team, 4).Rest.SequenceEqual([3u, 1u, 7u]), "four places, three hurt: those three only (resting fewer restores more HP)");
            Check(BeastSpaces.Rest(team, 0).Rest.Count == 0 && BeastSpaces.Rest([Member(5, 450, 450)], 3).Rest.Count == 0, "no place, or nobody hurt: nobody to rest");
            Check(BeastSpaces.Rest([Member(8, 50, 100), Member(9, 50, 100)], 1).Rest.SequenceEqual([8u]), "equal shares: the team window's order");
        }),
        ("Beast spaces 1770: behaviour: satiety from XBMPet column 3 and from the bestiary's page (« 0/3 »), as the user's capture of Cu sith", () =>
        {
            var cuSith = Row(1, 0, 47, (byte)1, (byte)42, (byte)3, 3u);
            Check(BeastSpaces.Satiety(cuSith) == 3 && BeastSpaces.Satiety(Row(2, 0, 48, (byte)1, (byte)35, (byte)0, 3u)) == 0 && BeastSpaces.Satiety(Row(3, 0, 1, (byte)1)) == 0, "Cu sith 3; 0 or a short row: not read");
            Check(BeastSpaces.SatietyShown("0/3") == 3 && BeastSpaces.SatietyShown("2/5") == 5 && BeastSpaces.SatietyShown("4/3") == 0 && BeastSpaces.SatietyShown("PV") == 0, "the page's « eaten/most »");
            var page = "0/3; Satiété; 83; Rés. magique; 95; Intelligence; 125; Vitalité; 88; Rés. physique; 154; Force; 608/608; PV; 12/100; Exp; (8); 5; ; Rang; Cu sith; N°1".Split("; ");
            Check(BeastSpaces.SatietyOfBook(page) == 3, "the bestiary page read in game on 18/11 (docs/evidence/0.1548): 3");
            Check(BeastSpaces.SatietyOfBook(page.Skip(1).ToArray()) == 0 && BeastSpaces.SatietyOfBook(["Exp", "Rang", "Pugil"]) == 0, "another shape, or a beast not held: 0");
        }),
        ("Beast spaces 1770: wiring: read apart in the loader; the panel's section; the bestiary's satiety checked in the log; the self-check; both languages; the evidence", () =>
        {
            var loader = Read("src/TenshoAssistant/Beastmaster/BeastCatalogLoader.Dalamud.cs");
            Check(loader.Contains("var spaces = BeastSpaces.Read(rows[\"XBMContentStageEvent\"], rows[\"XBMContentCamp\"], rows[\"XBMContentRandomStageEvent\"], rows[\"XBMRandomStageEvent\"]);", StringComparison.Ordinal)
                && loader.Contains("Columns.Count != columns) { rows.Clear(); break; }", StringComparison.Ordinal), "read apart, column counts checked");
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("DrawBeastSpaces(board, plan, team, catalog);", StringComparison.Ordinal) && page.Contains("var advice = BeastSpaces.Rest(team, places);", StringComparison.Ordinal), "the panel's section and its advice");
            var reader = Read("src/TenshoAssistant/Beastmaster/BeastBoardReader.Dalamud.cs");
            Check(reader.Contains("BeastSpaces.SatietyOfBook(texts) is var shown and > 0", StringComparison.Ordinal) && reader.Contains("Beastmaster satiety differs", StringComparison.Ordinal), "the satiety checked against each bestiary page");
            Check(Read("src/TenshoAssistant/MainWindow.SelfCheckPages.cs").Contains("beasts with a satiety", StringComparison.Ordinal), "the self-check counts them");
            foreach (var language in new[] { "en", "fr" })
            {
                var text = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "Spaces", "SpacesNote", "SpacePlaces", "SpaceRandom", "SpaceStep", "RestAdvice", "RestDown", "RestFull", "RestNoTeam", "RestNone", "FeedTeam", "FeedTeamTip", "FeedLine", "SpaceKind6", "SpaceKind8" }
                    .All(key => text.Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
            var evidence = Read("docs/evidence/0.1770-crucible-spaces.md");
            Check(evidence.Contains("XBMContentCamp", StringComparison.Ordinal) && evidence.Contains("Satiété 0/3", StringComparison.Ordinal) && evidence.Contains("\n---\n", StringComparison.Ordinal), "the evidence, both halves");
        }),
    ];
}
