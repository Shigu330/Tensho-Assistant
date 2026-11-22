namespace TenshoAssistant.Beastmaster;

/// <summary>
/// Beastmaster data (0.500, user request; columns established in docs/evidence/0.500-xbm-columns.md). The sheets are read raw: the typed rows of
/// the current Lumina.Excel do not match the game. Only what is established is kept here: names, family, attacks, attributes per rank, the
/// boards, their enemies and bonuses. Weakness is the column read as such, to confirm in game.
/// </summary>
public sealed record XbmRow(uint Row, ushort Sub, object?[] Cells)
{
    public int Int(int column) => Cells[column] is null ? 0 : Convert.ToInt32(Cells[column]);
    public uint UInt(int column) => Cells[column] is null ? 0 : Convert.ToUInt32(Cells[column]);
    public string Text(int column) => Cells[column] as string ?? "";
}
public sealed record BeastStats(int Strength, int Intelligence, int PhysicalResistance, int MagicalResistance, int Vitality);
/// <summary>AutoAttack: the element's name for a physical one; empty for a magical one. Ranks[0] = rank 1.</summary>
/// <summary>X-109 (0.980): AutoRow = the XBMElement row of the automatic attack (9 slashing, 8 piercing, 7 blunt). X-553 (0.1523): a magical one
/// has its element's row (1 fire, 2 wind, 3 earth, 4 lightning, 5 ice, 6 water; <see cref="BeastCatalog.ElementOfAspect"/>); 0 only for an
/// unaspected one (« Neutre » in the game's beast book). AutoAttack is that row's name, empty for an unaspected one.</summary>
public sealed record Beast(uint Id, string Name, uint Icon, string Family, string AutoAttack, string Assault, string Volley, IReadOnlyList<BeastStats> Ranks, string CatchPlace = "", uint AutoRow = 0)
{
    /// <summary>X-755 (0.1750): the effects it inflicts, as places in <see cref="BeastEffects.Order"/> (the game's order); empty for none.</summary>
    public IReadOnlyList<int> Effects { get; init; } = [];

    /// <summary>X-746 (0.1766): its family's number (XBMPet column 1, 1 to 8; its name is Addon 17740 + it); 0 not known.</summary>
    public int FamilyId { get; init; }

    /// <summary>X-745 (0.1767): where it is caught, XBMPet column 6: 1 a zone, 2 a duty (<see cref="CatchPlace"/> names it), 0 the beast the job starts with.</summary>
    public int CatchKind { get; init; }

    /// <summary>Attributes at this rank; a rank outside the table is the nearest known one. Null when the table is empty.</summary>
    public BeastStats? At(int rank) => Ranks.Count == 0 ? null : Ranks[Math.Clamp(rank - 1, 0, Ranks.Count - 1)];
}
/// <summary>X-486 (0.1529): Damage = the action's type of damage (Action.AttackType, its element when magical: the XBMElement rows of
/// <see cref="Beast.AutoRow"/>), empty for an unaspected one or none. X-486 (0.1559): Effect = the status the action inflicts
/// (XBMBattleDetailAction column 3, a Status row: docs/evidence/0.1559-beast-action-effect.md), empty for none. X-622 M1 (review of 0.1560):
/// EffectCategory = that status's Status.StatusCategory (column 6): 2 harmful (Poison, Glas: inflicted), 1 helpful (a bonus, a barrier,
/// Couvert: granted, not inflicted), 0 not known.</summary>
public sealed record BoardAction(string Name, string Shape, string Target, string Damage = "", string Effect = "")
{
    public int EffectCategory { get; init; }
    public const int Helpful = 1, Harmful = 2;
}
/// <summary>
/// X-109 (0.980, docs/evidence/0.980-beast-teams.md): Stars = XBMBattleDetail columns 1 to 5, the stars of the game's board window, in the order
/// Strength, Intelligence, Physical resistance, Magical resistance, Vitality (the user's capture: Chevalier du plateau 2, 1, 2, 2, 3); WeaknessRow = its XBMElement row.
/// </summary>
public sealed record BoardEnemy(string Name, uint Icon, string Weakness, IReadOnlyList<BoardAction> Actions, BeastStats? Stars = null, uint WeaknessRow = 0);
/// <summary>X-486 (0.1529): Detail = its XBMBattleDetail row (XBMContentBattle column 0), the number the game's board window gives its fights
/// (AgentXBMStageDetailList, StageDetailEntry.BattleDetailId); 0 not known.</summary>
public sealed record BoardBattle(bool Guardian, IReadOnlyList<BoardEnemy> Enemies, uint Detail = 0);
public sealed record BoardBonus(string Name, string Condition, int Points);
/// <summary>
/// X-746 (0.1766, the user's idea I-029 of 18/11): the action Borrow (« Assimilation ») gives with a familiar of this family: the action
/// whose condition is the familiar's Kinship of that family (Action.PrimaryCostType 125, PrimaryCostValue the family, XBMPet column 1;
/// docs/evidence/0.1766-beast-borrow.md), its name and icon from the Action sheet, Effect the first part of its description
/// (ActionTransient: effect, duration, condition), in the client's language.
/// </summary>
public sealed record BeastBorrow(int Family, uint Action, string Name, uint Icon, string Effect);
/// <summary>X-109 (0.981): Content = its ContentFinderCondition row (XBMContent column 0), which gives the labyrinth's territory.
/// X-576 (0.1546): RankRecommended = the first number of the board window's « Rang de bête recommandé : X à Y » (Addon 17912, two numbers
/// given by the game's code; Y is RankLimit, XBMContent column 3): the previous board's RankLimit plus one (<see cref="BeastCatalog.RankFloor"/>;
/// the user's captures: « 1 à 5 » on the 1st board, « 6 à 10 » on the 2nd; column 2, 3 and 8, is not it: docs/evidence/0.1546-beast-rank-range.md).
/// 0: not known.</summary>
public sealed record Board(uint Id, string Name, int Level, int ItemLevel, int TeamSize, int RankLimit, IReadOnlyList<BoardBattle> Battles, IReadOnlyList<BoardBonus> Bonuses, uint Content = 0, int RankRecommended = 0);
public enum BeastSort { Name, Family, Auto, Strength, Intelligence, PhysicalResistance, MagicalResistance, Vitality }

public sealed record BeastCatalog(IReadOnlyList<Beast> Beasts, IReadOnlyList<Board> Boards)
{
    /// <summary>Column count of each sheet read. Another count means the game changed them: nothing is read from columns that moved.</summary>
    public static readonly IReadOnlyDictionary<string, int> Columns = new Dictionary<string, int>
    {
        ["XBMPet"] = 27, ["XBMPetParamGrow"] = 5, ["XBMBattleDetail"] = 11, ["XBMBattleDetailAction"] = 4, ["XBMContent"] = 37, ["XBMContentBattle"] = 1,
        ["XBMElement"] = 1, ["XBMScoreBonus"] = 3, ["XBMActionEffectType"] = 1, ["XBMActionTarget"] = 1,
    };
    private const int FirstBonusColumn = 4;

    /// <summary>
    /// X-551 (0.1574, the user's request of 17/11: « look more like the game's »): the icon the game itself puts before each damage type and
    /// weakness, by the element's name as the other records keep it (Feu, Contondant…). It is in the sheet's own text: XBMElement's name is
    /// « &lt;icon(56)&gt;Feu » (an icon of the game's font: 56 to 61 the six elements, 185 to 187 blunt, piercing, slashing;
    /// docs/evidence/0.1574-beast-element-icons.md). Empty when the sheet's raw text is not given (tests, the desktop application).
    /// </summary>
    public IReadOnlyDictionary<string, uint> ElementIcons { get; init; } = new Dictionary<string, uint>();

    /// <summary>X-755 (0.1750): the names of <see cref="BeastEffects.Order"/> in the client's language (Addon); empty when not given (tests).</summary>
    public IReadOnlyList<string> EffectNames { get; init; } = [];

    /// <summary>X-746 (0.1766): the action Borrow gives, by family number; empty when not read (tests, a game update that moved it).</summary>
    public IReadOnlyDictionary<int, BeastBorrow> Borrows { get; init; } = new Dictionary<int, BeastBorrow>();

    /// <summary>X-746 (0.1766): the game's own label of Borrow in the Beastmaster's windows (Addon 17749, « Assimilation (Nv 22) »); empty when not given.</summary>
    public string BorrowLabel { get; init; } = "";

    /// <summary>X-746 (0.1766): the action Borrow gives with this beast; null when not known.</summary>
    public BeastBorrow? BorrowOf(Beast beast) => Borrows.GetValueOrDefault(beast.FamilyId);

    /// <summary>X-746 (0.1766): Action.PrimaryCostType of the eight actions Borrow gives: « the familiar's Kinship », its value the family.</summary>
    public const int KinshipCost = 125;

    /// <summary>
    /// X-746 (0.1766): one action per family of the beasts, among the Action rows whose condition is a Kinship (<see cref="KinshipCost"/>):
    /// the first row of each family (the game also has three copies of Cloud Skim further on, with another icon, for its directions).
    /// A family with no such row has none (its beasts show nothing).
    /// </summary>
    public static IReadOnlyDictionary<int, BeastBorrow> BorrowsOf(IEnumerable<(uint Row, int CostType, int CostValue, string Name, uint Icon, string Description)> actions, IEnumerable<int> families)
    {
        var wanted = families.Where(family => family > 0).ToHashSet();
        return actions.Where(action => action.CostType == KinshipCost && wanted.Contains(action.CostValue) && action.Name.Length > 0)
            .GroupBy(action => action.CostValue)
            .Select(group => group.OrderBy(action => action.Row).First())
            .ToDictionary(action => action.CostValue, action => new BeastBorrow(action.CostValue, action.Row, action.Name, action.Icon, EffectOf(action.Description)));
    }

    /// <summary>X-746 (0.1766): the first part of an action's description, up to its first empty line (effect, duration, condition; the
    /// game's notes about hotbars and replacements after it); the whole text when it has no empty line.</summary>
    public static string EffectOf(string description)
    {
        var lines = description.Trim().Replace("\r\n", "\n").Split('\n');
        var end = Array.FindIndex(lines, line => line.Trim().Length == 0);
        var kept = end > 0 ? lines[..end] : lines;
        return string.Join("\n", kept.Select(line => line.Trim()).Where(line => line.Length > 0));
    }

    /// <summary>
    /// X-764 M3 (review of 0.1751): true when a beast's XBMPet columns 11 to 21 are not all flags (a game update changed their type, or
    /// moved them at the same column count): no beast shows an effect then, and the bestiary says the effects are unavailable, instead of
    /// « none » for every beast.
    /// </summary>
    public bool EffectsUnavailable { get; init; }

    /// <summary>X-551 (0.1574): the number of the font icon a sheet's text starts with (« &lt;icon(56)&gt;Feu » → 56); 0 when it starts with none.</summary>
    public static uint IconOfMacro(string? macro)
    {
        const string open = "<icon(";
        if (macro is null || !macro.StartsWith(open, StringComparison.Ordinal)) return 0;
        var end = macro.IndexOf(")>", open.Length, StringComparison.Ordinal);
        return end > open.Length && uint.TryParse(macro.AsSpan(open.Length, end - open.Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var icon) ? icon : 0;
    }

    /// <summary>
    /// X-576 (0.1546, the user's capture of 18/11 around 00:05: « Rang de bête recommandé : 6 à 10 » before the 2nd board; « 1 à 5 » on the
    /// 1st): the range's first number is the highest synchronisation rank (XBMContent column 3) below this board's, plus one; 1 for the
    /// lowest board; 0 when the board's own rank is not known. No column of XBMContent holds it (docs/evidence/0.1546-beast-rank-range.md).
    /// </summary>
    public static int RankFloor(IEnumerable<int> limits, int limit) => limit <= 0 ? 0 : limits.Where(other => other > 0 && other < limit).DefaultIfEmpty(0).Max() + 1;

    public static BeastCatalog Build(IReadOnlyDictionary<string, IReadOnlyList<XbmRow>> sheets, Func<uint, string> petName, Func<int, string> familyName,
        Func<uint, int> attackType, Func<uint, string> enemyName, Func<uint, string> actionName, Func<uint, (string Name, int Level, int ItemLevel)> content, Func<int, uint, string>? catchPlace = null,
        Func<uint, int>? aspect = null, Func<uint, string>? statusName = null, Func<uint, int>? statusCategory = null, Func<uint, string>? elementMacro = null,
        Func<uint, string>? effectName = null, IEnumerable<(uint Row, int CostType, int CostValue, string Name, uint Icon, string Description)>? borrowActions = null, string borrowLabel = "")
    {
        foreach (var (sheet, columns) in Columns)
        {
            if (!sheets.TryGetValue(sheet, out var rows) || rows.Count == 0) throw new InvalidDataException($"{sheet}: no rows.");
            if (rows[0].Cells.Length != columns) throw new InvalidDataException($"{sheet}: {rows[0].Cells.Length} columns, {columns} expected.");
        }
        var elements = sheets["XBMElement"].ToDictionary(row => row.Row, row => row.Text(0));
        string Element(uint row) => elements.GetValueOrDefault(row, "");
        // Action.AttackType 1, 2, 3 = slashing, piercing, blunt; their XBMElement rows are 9, 8, 7 (Cu sith: 45187, « piercing » on the game's sheet).
        // X-553 (0.1523): Action.AttackType 5 is magical: its element (Action.Aspect) gives the row; before, every magical beast was 0 and never hit
        // an elemental weakness (Foudre, Glace…), so the panel's trios hit none (docs/evidence/0.1523-beast-weakness.md).
        uint AutoRow(uint action) => attackType(action) switch { 1 => 9u, 2 => 8u, 3 => 7u, 5 => ElementOfAspect(aspect?.Invoke(action) ?? 0), _ => 0u };
        string Auto(uint action) => AutoRow(action) is var element and not 0 ? Element(element) : "";
        var grow = sheets["XBMPetParamGrow"].GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.OrderBy(row => row.Sub)
            .Select(row => new BeastStats(row.Int(0), row.Int(1), row.Int(2), row.Int(3), row.Int(4))).ToArray());
        // X-764 M3 (review of 0.1751): the eleven flags read only when every beast's columns 11 to 21 hold flags (R08; empty, as XbmRow reads it: none).
        var effectsRead = sheets["XBMPet"].Where(row => row.Row != 0 && row.UInt(4) != 0).All(row => BeastEffects.Order.All(effect => effect.Column < row.Cells.Length && row.Cells[effect.Column] is null or bool));
        var beasts = sheets["XBMPet"].Where(row => row.Row != 0 && row.UInt(4) != 0).Select(row => new Beast(row.Row, Capitalize(petName(row.UInt(0))), row.UInt(4),
            familyName(row.Int(1)), Auto(row.UInt(5)), row.Text(9), row.Text(10), Array.AsReadOnly(grow.GetValueOrDefault(row.UInt(2)) ?? []),
            // 0.688 (user report): columns 6 and 7, where the beast is caught: docs/evidence/0.688-beast-places.md.
            catchPlace?.Invoke(row.Int(6), row.UInt(7)) ?? "", AutoRow(row.UInt(5)))
            // X-755 (0.1750): columns 11 to 21, the effects it inflicts, put in the game's order (BeastEffects).
            { Effects = effectsRead ? BeastEffects.Of(column => row.Cells[column] is true) : [], FamilyId = row.Int(1), CatchKind = row.Int(6) }).ToArray();

        var shapes = sheets["XBMActionEffectType"].ToDictionary(row => row.Row, row => row.Text(0));
        var targets = sheets["XBMActionTarget"].ToDictionary(row => row.Row, row => row.Text(0));
        // X-486 (0.1559): column 3 is the status the action inflicts (Poison, Paralysie, Glas…), 0 for none.
        string Effect(uint status) => status == 0 || statusName is null ? "" : statusName(status);
        var actions = sheets["XBMBattleDetailAction"].ToDictionary(row => row.Row, row => new BoardAction(actionName(row.UInt(0)), shapes.GetValueOrDefault(row.UInt(2), ""), targets.GetValueOrDefault(row.UInt(1), ""), Auto(row.UInt(0)), Effect(row.UInt(3))));
        // X-622 M1 (review of 0.1560): whether that status harms or helps (Status column 6), so a bonus the enemy gives is not said « inflicted ».
        if (statusCategory is not null)
            foreach (var row in sheets["XBMBattleDetailAction"].Where(row => row.UInt(3) != 0))
                actions[row.Row] = actions[row.Row] with { EffectCategory = statusCategory(row.UInt(3)) };
        var enemies = sheets["XBMBattleDetail"].Where(row => row.UInt(0) != 0).GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.OrderBy(row => row.Sub).Select(row =>
            new BoardEnemy(Capitalize(enemyName(row.UInt(0))), row.UInt(10), Element(row.UInt(8)),
                Array.AsReadOnly(new[] { row.UInt(6), row.UInt(7) }.Where(id => id != 0 && actions.ContainsKey(id)).Select(id => actions[id]).ToArray()),
                new BeastStats(row.Int(1), row.Int(2), row.Int(3), row.Int(4), row.Int(5)), row.UInt(8))).ToArray());
        var battles = sheets["XBMContentBattle"].GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.OrderBy(row => row.Sub).ToArray());
        var bonuses = sheets["XBMScoreBonus"].OrderBy(row => row.Row).ToArray();
        var boards = sheets["XBMContent"].Where(row => row.Row != 0 && row.UInt(0) != 0).Select(row =>
        {
            var (name, level, itemLevel) = content(row.UInt(0));
            // The guardian's battle comes first in the sheet (« Gardien du plateau : combattre 3 monstres », board 1, battle 6: three enemies).
            var fights = (battles.GetValueOrDefault(row.Row) ?? []).Select(battle => new BoardBattle(battle.Sub == 0, Array.AsReadOnly(enemies.GetValueOrDefault(battle.UInt(0)) ?? []), battle.UInt(0))).ToArray();
            var scored = bonuses.Where(bonus => FirstBonusColumn + (int)bonus.Row < row.Cells.Length).Select(bonus => new BoardBonus(bonus.Text(1), bonus.Text(2), row.Int(FirstBonusColumn + (int)bonus.Row)))
                .Where(bonus => bonus.Points > 0 && bonus.Name.Length > 0).ToArray();
            return new Board(row.Row, name, level, itemLevel, row.Int(1), row.Int(3), Array.AsReadOnly(fights), Array.AsReadOnly(scored), row.UInt(0));
        }).ToArray();
        // X-576 (0.1546): each board's recommended range starts after the previous board's synchronisation rank (not column 2).
        var limits = boards.Select(board => board.RankLimit).ToArray();
        boards = boards.Select(board => board with { RankRecommended = RankFloor(limits, board.RankLimit) }).ToArray();
        // X-551 (0.1574): each element's own icon, from the sheet's raw text (its macro form); a name without one is left out.
        var icons = new Dictionary<string, uint>();
        if (elementMacro is not null)
            foreach (var (row, name) in elements)
                if (name.Length > 0 && IconOfMacro(elementMacro(row)) is var icon and not 0) icons[name] = icon;
        return new(Array.AsReadOnly(beasts), Array.AsReadOnly(boards))
        {
            ElementIcons = icons,
            EffectNames = effectName is null ? [] : BeastEffects.Order.Select(effect => effectName(effect.Name)).ToArray(),
            EffectsUnavailable = !effectsRead,
            // X-746 (0.1766): the action Borrow gives, per family of the beasts read.
            Borrows = borrowActions is null ? new Dictionary<int, BeastBorrow>() : BorrowsOf(borrowActions, beasts.Select(beast => beast.FamilyId)),
            BorrowLabel = borrowLabel,
        };
    }

    /// <summary>
    /// X-553 (0.1523): the XBMElement row of a magical attack's element (Action.Aspect): 1 fire → 1, 2 ice → 5, 3 wind → 2, 4 earth → 3,
    /// 5 lightning → 4, 6 water → 6; 7 (unaspected) and anything else → 0. Each value matched against the game's beast book (« Feu », « Glace »,
    /// « Foudre »… beside « Attaque auto. », read on 17/11) through a beast found by its attributes alone (docs/evidence/0.1523-beast-weakness.md).
    /// </summary>
    public static uint ElementOfAspect(int aspect) => aspect switch { 1 => 1u, 2 => 5u, 3 => 2u, 4 => 3u, 5 => 4u, 6 => 6u, _ => 0u };

    private static string Capitalize(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    /// <summary>The list as shown. Attributes are those of this rank. Obtained not read: nothing is hidden by the filter.</summary>
    public IReadOnlyList<Beast> View(string query, string? family, bool onlyObtained, IReadOnlySet<uint>? obtained, int rank, BeastSort sort, bool descending)
    {
        IEnumerable<Beast> rows = Beasts;
        if (query.Length > 0) rows = rows.Where(beast => SearchText.Contains(beast.Name,query,StringComparison.CurrentCultureIgnoreCase) || SearchText.Contains(beast.CatchPlace,query,StringComparison.CurrentCultureIgnoreCase) || SearchText.Contains(beast.Assault,query,StringComparison.CurrentCultureIgnoreCase) || SearchText.Contains(beast.Volley,query,StringComparison.CurrentCultureIgnoreCase));
        if (family is not null) rows = rows.Where(beast => beast.Family == family);
        if (onlyObtained && obtained is not null) rows = rows.Where(beast => obtained.Contains(beast.Id));
        int Stat(Beast beast, Func<BeastStats, int> read) => beast.At(rank) is { } stats ? read(stats) : -1;
        Func<Beast, IComparable> key = sort switch
        {
            BeastSort.Family => beast => beast.Family,
            BeastSort.Auto => beast => beast.AutoAttack,
            BeastSort.Strength => beast => Stat(beast, stats => stats.Strength),
            BeastSort.Intelligence => beast => Stat(beast, stats => stats.Intelligence),
            BeastSort.PhysicalResistance => beast => Stat(beast, stats => stats.PhysicalResistance),
            BeastSort.MagicalResistance => beast => Stat(beast, stats => stats.MagicalResistance),
            BeastSort.Vitality => beast => Stat(beast, stats => stats.Vitality),
            _ => beast => beast.Name,
        };
        var ordered = descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
        return Array.AsReadOnly(ordered.ThenBy(beast => beast.Id).ToArray());
    }
}

/// <summary>ObtainedUnavailable: the native read failed (maybe after a game update); Obtained is then null. Null alone: the game has not given the list yet.</summary>
public sealed record BeastState(BeastCatalog? Catalog, string? Error, IReadOnlySet<uint>? Obtained, bool ObtainedUnavailable = false);

public sealed class BeastService
{
    private readonly object gate = new();
    private BeastState state = new(null, null, null);
    public BeastState State => Volatile.Read(ref state);
    internal void Publish(BeastCatalog catalog) { lock (gate) Volatile.Write(ref state, State with { Catalog = catalog, Error = null }); }
    internal void Fail(string error) { lock (gate) Volatile.Write(ref state, State with { Error = error }); }
    internal void SetObtained(IReadOnlySet<uint>? obtained, bool unavailable = false) { lock (gate) Volatile.Write(ref state, State with { Obtained = obtained, ObtainedUnavailable = unavailable }); }
}
