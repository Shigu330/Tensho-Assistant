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
/// <summary>X-109 (0.980): AutoRow = the XBMElement row of the automatic attack (9 slashing, 8 piercing, 7 blunt; 0 magical).</summary>
public sealed record Beast(uint Id, string Name, uint Icon, string Family, string AutoAttack, string Assault, string Volley, IReadOnlyList<BeastStats> Ranks, string CatchPlace = "", uint AutoRow = 0)
{
    /// <summary>Attributes at this rank; a rank outside the table is the nearest known one. Null when the table is empty.</summary>
    public BeastStats? At(int rank) => Ranks.Count == 0 ? null : Ranks[Math.Clamp(rank - 1, 0, Ranks.Count - 1)];
}
public sealed record BoardAction(string Name, string Shape, string Target);
/// <summary>
/// X-109 (0.980, docs/evidence/0.980-beast-teams.md): Stars = XBMBattleDetail columns 1 to 5, the stars of the game's board window, in the order
/// Strength, Intelligence, Physical resistance, Magical resistance, Vitality (the user's capture: Chevalier du plateau 2, 1, 2, 2, 3); WeaknessRow = its XBMElement row.
/// </summary>
public sealed record BoardEnemy(string Name, uint Icon, string Weakness, IReadOnlyList<BoardAction> Actions, BeastStats? Stars = null, uint WeaknessRow = 0);
public sealed record BoardBattle(bool Guardian, IReadOnlyList<BoardEnemy> Enemies);
public sealed record BoardBonus(string Name, string Condition, int Points);
/// <summary>X-109 (0.981): Content = its ContentFinderCondition row (XBMContent column 0), which gives the labyrinth's territory.</summary>
public sealed record Board(uint Id, string Name, int Level, int ItemLevel, int TeamSize, int RankLimit, IReadOnlyList<BoardBattle> Battles, IReadOnlyList<BoardBonus> Bonuses, uint Content = 0);
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

    public static BeastCatalog Build(IReadOnlyDictionary<string, IReadOnlyList<XbmRow>> sheets, Func<uint, string> petName, Func<int, string> familyName,
        Func<uint, int> attackType, Func<uint, string> enemyName, Func<uint, string> actionName, Func<uint, (string Name, int Level, int ItemLevel)> content, Func<int, uint, string>? catchPlace = null)
    {
        foreach (var (sheet, columns) in Columns)
        {
            if (!sheets.TryGetValue(sheet, out var rows) || rows.Count == 0) throw new InvalidDataException($"{sheet}: no rows.");
            if (rows[0].Cells.Length != columns) throw new InvalidDataException($"{sheet}: {rows[0].Cells.Length} columns, {columns} expected.");
        }
        var elements = sheets["XBMElement"].ToDictionary(row => row.Row, row => row.Text(0));
        string Element(uint row) => elements.GetValueOrDefault(row, "");
        // Action.AttackType 1, 2, 3 = slashing, piercing, blunt; their XBMElement rows are 9, 8, 7 (Cu sith: 45187, « piercing » on the game's sheet).
        uint AutoRow(uint action) => attackType(action) switch { 1 => 9u, 2 => 8u, 3 => 7u, _ => 0u };
        string Auto(uint action) => AutoRow(action) is var element and not 0 ? Element(element) : "";
        var grow = sheets["XBMPetParamGrow"].GroupBy(row => row.Row).ToDictionary(group => group.Key, group => group.OrderBy(row => row.Sub)
            .Select(row => new BeastStats(row.Int(0), row.Int(1), row.Int(2), row.Int(3), row.Int(4))).ToArray());
        var beasts = sheets["XBMPet"].Where(row => row.Row != 0 && row.UInt(4) != 0).Select(row => new Beast(row.Row, Capitalize(petName(row.UInt(0))), row.UInt(4),
            familyName(row.Int(1)), Auto(row.UInt(5)), row.Text(9), row.Text(10), Array.AsReadOnly(grow.GetValueOrDefault(row.UInt(2)) ?? []),
            // 0.688 (user report): columns 6 and 7, where the beast is caught: docs/evidence/0.688-beast-places.md.
            catchPlace?.Invoke(row.Int(6), row.UInt(7)) ?? "", AutoRow(row.UInt(5)))).ToArray();

        var shapes = sheets["XBMActionEffectType"].ToDictionary(row => row.Row, row => row.Text(0));
        var targets = sheets["XBMActionTarget"].ToDictionary(row => row.Row, row => row.Text(0));
        var actions = sheets["XBMBattleDetailAction"].ToDictionary(row => row.Row, row => new BoardAction(actionName(row.UInt(0)), shapes.GetValueOrDefault(row.UInt(2), ""), targets.GetValueOrDefault(row.UInt(1), "")));
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
            var fights = (battles.GetValueOrDefault(row.Row) ?? []).Select(battle => new BoardBattle(battle.Sub == 0, Array.AsReadOnly(enemies.GetValueOrDefault(battle.UInt(0)) ?? []))).ToArray();
            var scored = bonuses.Where(bonus => FirstBonusColumn + (int)bonus.Row < row.Cells.Length).Select(bonus => new BoardBonus(bonus.Text(1), bonus.Text(2), row.Int(FirstBonusColumn + (int)bonus.Row)))
                .Where(bonus => bonus.Points > 0 && bonus.Name.Length > 0).ToArray();
            return new Board(row.Row, name, level, itemLevel, row.Int(1), row.Int(3), Array.AsReadOnly(fights), Array.AsReadOnly(scored), row.UInt(0));
        }).ToArray();
        return new(Array.AsReadOnly(beasts), Array.AsReadOnly(boards));
    }

    private static string Capitalize(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];

    /// <summary>The list as shown. Attributes are those of this rank. Obtained not read: nothing is hidden by the filter.</summary>
    public IReadOnlyList<Beast> View(string query, string? family, bool onlyObtained, IReadOnlySet<uint>? obtained, int rank, BeastSort sort, bool descending)
    {
        IEnumerable<Beast> rows = Beasts;
        if (query.Length > 0) rows = rows.Where(beast => beast.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || beast.CatchPlace.Contains(query, StringComparison.CurrentCultureIgnoreCase) || beast.Assault.Contains(query, StringComparison.CurrentCultureIgnoreCase) || beast.Volley.Contains(query, StringComparison.CurrentCultureIgnoreCase));
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
