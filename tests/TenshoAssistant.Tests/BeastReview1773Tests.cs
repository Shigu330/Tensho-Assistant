using TenshoAssistant.Beastmaster;

// X-771 (0.1773, review of block 0.1769 to 0.1772, minor findings): M1 the result window's rewards read only in the one layout seen (one
// reward); M2 the satiety the bestiary's page shows, kept with the page and shown before XBMPet column 3, which stays « to confirm »;
// M3 the feeds named by family and the shop and treasure spaces' advice from the game's guide (DescriptionString 1736, 1737, XBMItem 57);
// M4 the saved plans written and read back with the rewards and the pages' satiety, ranks kept.
internal static class BeastReview1773Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).ReplaceLineEndings("\n");

    private static uint Icon(uint item) => item == 51734 ? 20217u : item == 9 ? 60009u : 0u;

    // The reward numbers of the board of 12/11 (docs/evidence/0.1771-beast-result-rewards.md).
    private static Dictionary<int, long> Rewards(params (int Index, long Value)[] changes)
    {
        var values = new Dictionary<int, long> { [47] = 1, [48] = 51734, [53] = 20217, [63] = 4 };
        foreach (var (index, value) in changes) values[index] = value;
        return values;
    }

    private static readonly BeastStats[] Ranks = Enumerable.Range(1, 8).Select(rank => new BeastStats(127 + rank, 78, 77, 73, 103)).ToArray();
    private static Beast Make(uint id, string name, int family, int satiety) => new(id, name, 0, "", "", "", "", Ranks) { FamilyId = family, Satiety = satiety };

    // The bestiary's page of Cu sith as the probe read it on 18/11 (docs/evidence/0.1548-beast-book.md), texts only.
    private const string CuSithPage = "0/3;Satiété;83;Rés. magique;95;Intelligence;125;Vitalité;88;Rés. physique;154;Force;608/608;PV;12/100;Exp;(8);5;;Rang;"
        + "Zone d'apparition;Aire d'effet;Attaque physique tranchante;Griffure;Thériens;Classe;Cu sith;N°1";

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("Beast review 1773: behaviour: M1 the rewards are read only as seen (one); two to five, a layout never seen, are refused whole", () =>
        {
            Check(BeastResults.Rewards(Rewards(), Icon) is [{ Item: 51734, Icon: 20217, Count: 4 }], "one reward, as on the boards of 12/11 and 15/11");
            foreach (var count in new[] { 2, 3, 5 })
                Check(BeastResults.Rewards(Rewards((47, count), (49, 9), (54, 60009), (64, 3), (50, 9), (55, 60009), (65, 1)), Icon) is null, $"{count} rewards: not read");
            Check(Read("docs/evidence/0.1771-beast-result-rewards.md").Contains("X-771", StringComparison.Ordinal), "the evidence says why");
        }),
        ("Beast review 1773: behaviour: M2 the bestiary's satiety is read with the page and shown first; the column stays « to confirm »", () =>
        {
            var cuSith = Make(1, "Cu sith", 1, 3);
            var page = BeastTeams.ParseBook(CuSithPage.Split(';'), [cuSith]);
            Check(page is { Beast: 1, Rank: 8, Satiety: 3 }, $"the page's « 0/3 »: satiety 3, {page}");
            Check(BeastTeams.ParseBook(CuSithPage.Replace("0/3;", "1/4;", StringComparison.Ordinal).Split(';'), [cuSith]) is { Satiety: 4 }, "the page's second number, whatever was eaten");
            Check(BeastSpaces.SatietyOf(cuSith, null) == (3, false), "no page read: the column, to confirm");
            Check(BeastSpaces.SatietyOf(cuSith, new Dictionary<uint, BeastTeams.BookRead> { [1] = page! with { Satiety = 4 } }) == (4, true), "the page's number over the column when they differ");
            var old = page! with { Satiety = 0 };
            Check(BeastSpaces.SatietyOf(cuSith, new Dictionary<uint, BeastTeams.BookRead> { [1] = old }) == (3, false), "a page kept before 0.1773 (no satiety): the column, to confirm");
            Check(BeastSpaces.SatietyOf(Make(2, "Unread", 1, 0), null) == (0, false), "neither: not read");
            // A page of the same rank read again now carries the satiety: it replaces the one kept without it (saved once).
            var kept = new BeastTeams.SavedPlans([], [], new() { [1] = 8 }, null, null, new() { [1] = old });
            Check(BeastTeams.WithBook(kept, page!) is { Book: { } book } && book[1].Satiety == 3, "the old page replaced by the same one with its satiety");
            Check(BeastTeams.WithBook(kept with { Book = new() { [1] = page! } }, page!) is null, "the same page again: nothing to save");
        }),
        ("Beast review 1773: behaviour: M3 the feeds a family can eat, by name, in the catalog's order; none for an unread family", () =>
        {
            BeastItem Item(uint row, int kind, string name, params int[] families) => new(row, kind, name, 0, "", families);
            BeastItem[] items = [Item(1, BeastItems.Feed, "Carrot", 1, 2), Item(2, BeastItems.Gear, "Gear", 1), Item(3, BeastItems.Feed, "Fish", 2), Item(4, BeastItems.Feed, "Berry", 1), Item(5, BeastItems.Feed, "", 1)];
            Check(BeastSpaces.Feeds(items, 1).SequenceEqual(["Carrot", "Berry"]), "family 1: its two feeds, no gear, no nameless row: " + string.Join(",", BeastSpaces.Feeds(items, 1)));
            Check(BeastSpaces.Feeds(items, 2).SequenceEqual(["Carrot", "Fish"]), "family 2");
            Check(BeastSpaces.Feeds(items, 0).Count == 0 && BeastSpaces.Feeds(items, 9).Count == 0, "a family not read, or with no feed: none");
        }),
        ("Beast review 1773: behaviour: M4 the saved plans go and come back whole with the rewards and the pages' satiety; an older file reads", () =>
        {
            var result = new BoardResult(DateTimeOffset.Parse("2026-11-22T03:00:00+00:00"), 7550, [new BeastGain(1, 4, 0, 5, 20)], 1000, 1, 8550,
                new BoardParts(5000, 750, 800, 1000, 0, 361), [1000], [new BoardReward(51734, 20217, 4)]);
            var page = new BeastTeams.BookRead(1, 8, 5, new BeastStats(154, 95, 88, 83, 125), 608, 12, 3);
            var saved = new BeastTeams.SavedPlans([], [], new() { [1] = 8, [3] = 2 }, result, [3], new() { [1] = page });
            var back = BeastTeams.Read(BeastTeams.Write(saved));
            Check(back is not null, "read back (a JsonException would reject the whole file, ranks included)");
            Check(back!.Result?.Rewards is [{ Item: 51734, Icon: 20217, Count: 4 }], "the rewards come back");
            Check(back.Ranks is { Count: 2 } ranks && ranks[1] == 8 && ranks[3] == 2 && back.Floors!.SetEquals([3u]), "the ranks and floors come back");
            Check(back.Book?[1] == page, "the bestiary's page comes back with its satiety");
            Check(back.Result!.Score == 7550 && back.Result.Parts == result.Parts && back.Result.BonusPoints!.SequenceEqual([1000]), "the rest of the result");
            var older = BeastTeams.Read("{\"Plans\":{},\"Keys\":{},\"Ranks\":{\"1\":8},\"Result\":{\"At\":\"2026-11-16T10:35:54+00:00\",\"Score\":8000,\"Beasts\":[]},"
                + "\"Book\":{\"1\":{\"Beast\":1,\"Rank\":8,\"Synced\":5,\"Stats\":{\"Strength\":154,\"Intelligence\":95,\"PhysicalResistance\":88,\"MagicalResistance\":83,\"Vitality\":125},\"Hp\":608,\"Exp\":12}}}");
            Check(older is { Result.Rewards: null } && older.Book?[1].Satiety == 0 && older.Ranks?[1] == 8, "a file of before: no rewards, no satiety, the ranks kept");
        }),
        ("Beast review 1773: wiring: the panel's shop and treasure advice, the satiety and feeds line; both languages", () =>
        {
            var page = Read("src/TenshoAssistant/MainWindow.Beastmaster.cs");
            Check(page.Contains("if (space.Kind is BeastSpaces.Shop or BeastSpaces.Treasure) { DrawBeastSpaceAdvice(space.Kind, catalog); continue; }", StringComparison.Ordinal), "the advice under shops and coffers");
            Check(page.Contains("BeastSpaces.SatietyOf(beast, beastPlans?.Book)", StringComparison.Ordinal) && page.Contains("\"BeastUi.FeedLineToConfirm\"", StringComparison.Ordinal), "the satiety: the bestiary's, else to confirm");
            Check(page.Contains("BeastSpaces.Feeds(catalog.Items, beast.FamilyId)", StringComparison.Ordinal), "the feeds by name");
            Check(page.Contains("rule.Key == (kind == BeastSpaces.Shop ? \"Shop\" : \"Treasure\")", StringComparison.Ordinal), "the game's guide text on hover");
            foreach (var language in new[] { "en", "fr" })
            {
                var words = Read($"src/TenshoAssistant/Localization/Languages/{language}.json");
                Check(new[] { "FeedLine", "FeedLineToConfirm", "FeedLineUnread", "FeedNone", "ShopAdvice", "ThiefKnife", "TreasureAdvice" }.All(key => words.Contains($"    \"{key}\": ", StringComparison.Ordinal)), "the words, " + language);
            }
        }),
    ];
}
