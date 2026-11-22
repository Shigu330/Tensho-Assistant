namespace TenshoAssistant.Beastmaster;

/// <summary>X-744 (0.1770): one thing a random space may hold (XBMRandomStageEvent: its kind and its index; a campsite's places).</summary>
public sealed record SpaceOption(int Kind, int Index, int Places);

/// <summary>
/// X-744 (0.1770, the user's idea I-026, second part): one space of a board that is not a fight (XBMContentStageEvent, by board): its step
/// (column 0, the game's « Étape N » of the board's layout), its kind (column 1, XBMStageEventType: 5 shop, 6 campsite, 7 treasure, 8 random),
/// its index among the spaces of that kind (column 2); a campsite's places (XBMContentCamp: the familiars it can house); a random space's
/// options (XBMContentRandomStageEvent then XBMRandomStageEvent). docs/evidence/0.1770-crucible-spaces.md.
/// </summary>
public sealed record BoardSpace(int Step, int Kind, int Index, int Places, IReadOnlyList<SpaceOption> Options);

/// <summary>
/// X-744 (0.1770): the advice for one campsite from the team the game's team window shows (HP read after each fight): the beasts to rest
/// (alive, hurt, the lowest HP share first, at most the campsite's places), those at full HP (resting would only cost them their feed's
/// effects), those incapacitated (the game's guide does not say a campsite revives them: a Crucible item does) and those whose HP was not read.
/// </summary>
public sealed record RestAdvice(int Places, IReadOnlyList<uint> Rest, IReadOnlyList<uint> Full, IReadOnlyList<uint> Down, IReadOnlyList<uint> Unread);

public static class BeastSpaces
{
    /// <summary>XBMStageEventType rows, named by the game's Addon 17550 + kind (17551 « Affrontement » … 17557 « Carte chance »; 1 the start).</summary>
    public const int Start = 1, Enemy = 2, Elite = 3, Guardian = 4, Shop = 5, Campsite = 6, Treasure = 7, Random = 8;

    /// <summary>The game's name of a space's kind: Addon 17549 + kind (2 → 17551 « Affrontement », 8 → 17557 « Carte chance »).</summary>
    public const uint NameAddonBase = 17549;

    /// <summary>The game's « Étape &lt;num&gt; » of the board's layout (Addon 17771) and « Satiété » of the bestiary (Addon 17728).</summary>
    public const uint StepAddon = 17771, SatietyAddon = 17728;

    /// <summary>The Crucible item that revives an incapacitated familiar (XBMItem 96: « Ranime une bête hors de combat. »).</summary>
    public const uint ReviveItem = 96;
    /// <summary>X-771 M3 (0.1773): XBMItem 57 « Dague de voleur » / « Thief's Knife »: two items from a treasure coffer instead of one (the game's guide, DescriptionString 1737; Addon 17623).</summary>
    public const uint ThiefKnife = 57;

    /// <summary>The column count of each sheet read; another count means the game changed it: no space is read then (R08).</summary>
    public static readonly IReadOnlyDictionary<string, int> Columns = new Dictionary<string, int>
    {
        ["XBMContentStageEvent"] = 4, ["XBMContentCamp"] = 1, ["XBMContentRandomStageEvent"] = 1, ["XBMRandomStageEvent"] = 2,
    };

    /// <summary>
    /// The spaces of each board that are not fights, in the board's order (by step, then the sheet's order). A campsite whose places are not in
    /// XBMContentCamp, or a random space whose options are not found, keeps 0 places or no option (shown as not read), never a guess.
    /// </summary>
    public static IReadOnlyDictionary<uint, IReadOnlyList<BoardSpace>> Read(IEnumerable<XbmRow> stages, IEnumerable<XbmRow> camps, IEnumerable<XbmRow> randomOfBoard, IEnumerable<XbmRow> randoms)
    {
        var places = camps.Where(row => row.Row != 0).GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.ToDictionary(row => (int)row.Sub, row => row.Int(0)));
        var randomRow = randomOfBoard.Where(row => row.Row != 0).GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.ToDictionary(row => (int)row.Sub, row => row.UInt(0)));
        var options = randoms.Where(row => row.Row != 0).GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.OrderBy(row => row.Sub).Select(row => (Kind: row.Int(0), Index: row.Int(1))).ToArray());
        int Places(uint board, int index) => places.TryGetValue(board, out var camp) && camp.TryGetValue(index, out var count) && count > 0 ? count : 0;
        var result = new Dictionary<uint, IReadOnlyList<BoardSpace>>();
        foreach (var board in stages.Where(row => row.Row != 0).GroupBy(row => row.Row))
        {
            var spaces = new List<BoardSpace>();
            foreach (var row in board.OrderBy(row => row.Int(0)).ThenBy(row => row.Sub))
            {
                var (step, kind, index) = (row.Int(0), row.Int(1), row.Int(2));
                if (kind is not (Shop or Campsite or Treasure or Random) || index < 0) continue;
                IReadOnlyList<SpaceOption> held = [];
                if (kind == Random && randomRow.TryGetValue(board.Key, out var map) && map.TryGetValue(index, out var random) && options.TryGetValue(random, out var found))
                    held = found.Where(option => option.Kind is >= Enemy and <= Treasure && option.Index >= 0)
                        .Select(option => new SpaceOption(option.Kind, option.Index, option.Kind == Campsite ? Places(board.Key, option.Index) : 0)).ToArray();
                spaces.Add(new BoardSpace(step, kind, index, kind == Campsite ? Places(board.Key, index) : 0, held));
            }
            result[board.Key] = spaces;
        }
        return result;
    }

    /// <summary>The campsite places a space may offer: its own, or a random space's campsite option (0: none, or not read).</summary>
    public static int CampsitePlaces(BoardSpace space) => space.Kind == Campsite ? space.Places : space.Options.FirstOrDefault(option => option.Kind == Campsite)?.Places ?? 0;

    /// <summary>
    /// The beasts to rest at a campsite of this many places (DescriptionString 1735: « resting fewer familiars than the maximum allowance will
    /// restore more HP to those who do rest », « familiars will lose all effects of feed upon resting »): the hurt ones, the lowest HP share
    /// first, at most <paramref name="places"/>; fewer when fewer are hurt. Each beast once, in the team's order for equal shares.
    /// </summary>
    public static RestAdvice Rest(IReadOnlyList<BeastTeams.TeamMember> team, int places)
    {
        var known = team.GroupBy(member => member.Beast).Select(group => group.First()).ToArray();
        var hurt = known.Where(member => member.MaxHp > 0 && member.Hp > 0 && member.Hp < member.MaxHp)
            .Select((member, order) => (member, order)).OrderBy(pair => (double)pair.member.Hp / pair.member.MaxHp).ThenBy(pair => pair.order)
            .Take(Math.Max(0, places)).Select(pair => pair.member.Beast).ToArray();
        return new RestAdvice(places, hurt,
            known.Where(member => member.MaxHp > 0 && member.Hp >= member.MaxHp).Select(member => member.Beast).ToArray(),
            known.Where(member => member.Down).Select(member => member.Beast).ToArray(),
            known.Where(member => member.MaxHp <= 0).Select(member => member.Beast).ToArray());
    }

    /// <summary>
    /// X-744 (0.1770): a beast's satiety (XBMPet column 3, 1 to 5): the bestiary's « Satiété 0/3 » for Cu sith (row 1, column 3 = 3,
    /// docs/evidence/0.1548-beast-book.md); 0 when not read. The game's guide (DescriptionString 1742): satiety limits how much feed a familiar
    /// eats, never the same feed twice.
    /// </summary>
    public static int Satiety(XbmRow pet) => pet.Cells.Length > 3 && pet.Int(3) is var value and >= 1 and <= 9 ? value : 0;

    /// <summary>
    /// The satiety the bestiary's beast page shows (XBMMonsterBookDetail, the same places as <see cref="BeastTeams.ParseBook"/>: the value 16
    /// texts before the only « (n) » text, « 0/3; Satiété; … »); 0 when the page is not of that shape. The reader compares it with
    /// <see cref="Beast.Satiety"/> and says in the log whether they agree, so each page read in game checks the column (R06).
    /// </summary>
    public static int SatietyOfBook(IReadOnlyList<string> texts)
    {
        var own = Enumerable.Range(0, texts.Count).Where(index => texts[index] is { Length: > 2 } text && text[0] == '(' && text[^1] == ')'
            && int.TryParse(text[1..^1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var rank) && rank > 0).ToArray();
        return own is [var at] && at >= 16 ? SatietyShown(texts[at - 16]) : 0;
    }

    /// <summary>
    /// X-771 M2 (0.1773): the satiety the panel shows: the one the bestiary's page showed in game when it was read (the game's own number,
    /// kept with the page), else XBMPet column 3, taken as satiety on one reading only and shown « to confirm » (R06); 0 when neither.
    /// </summary>
    public static (int Value, bool FromBook) SatietyOf(Beast beast, IReadOnlyDictionary<uint, BeastTeams.BookRead>? book)
        => book is not null && book.TryGetValue(beast.Id, out var page) && page.Satiety > 0 ? (page.Satiety, true) : (beast.Satiety, false);

    /// <summary>
    /// X-771 M3 (0.1773): the names of the feeds a familiar's family can eat (those whose description in the game names the family), in the
    /// catalog's order, so the panel says which ones to look for at a shop rather than only how many there are.
    /// </summary>
    public static IReadOnlyList<string> Feeds(IEnumerable<BeastItem> items, int family)
        => family > 0 ? items.Where(item => BeastItems.UsableBy(item, family)).Select(item => item.Name).Where(name => name.Length > 0).Distinct().ToArray() : [];

    /// <summary>The satiety the bestiary's page shows, « 0/3 » → 3 (its second number); 0 when not of that shape.</summary>
    public static int SatietyShown(string text) => text.Split('/') is [var now, var most]
        && int.TryParse(now, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var eaten) && eaten >= 0
        && int.TryParse(most, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var top) && top > 0 && eaten <= top ? top : 0;
}
