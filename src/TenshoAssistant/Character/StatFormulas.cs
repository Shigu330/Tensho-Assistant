namespace TenshoAssistant.Character;

/// <summary>Level modifiers of the stat formulas: Sub and Div from the game (ParamGrow.BaseSpeed, ParamGrow.LevelModifier), Main from the community table.</summary>
public readonly record struct LevelModifiers(int Main, int Sub, int Div);
/// <summary>A derived stat: shown value, the raw attribute it comes from and the localization key of its explanation.</summary>
public sealed record DerivedStat(string Key, string Value, int Raw, string Attribute);

/// <summary>
/// Real values of the combat stats (0.182). Sources: level modifiers Sub/Div read from the game sheet ParamGrow (level 100: 420 / 2780,
/// docs/evidence/0.182.0-paramgrow-probe.log); Main and the formulas from the community (Allagan Studies, « How to be a math wizard »
/// third edition, and akhmorning.com), as the game does not publish them. Job category (30 war, 31 magic, 32 gathering, 33 crafting),
/// role (1 tank, 4 healer) and primary stat come from ClassJob.
/// </summary>
public static class StatFormulas
{
    // Main stat modifier per level, levels 1–100 (0.184): not in the game sheets. Taken as data from Character Panel Refined (Kouzukii, MIT,
    // CharacterPanelRefined/LevelModifiers.cs, read 2026-10-31), whose Sub and Div columns match ParamGrow at levels 50/70/80/90/100
    // (docs/evidence/0.182.0-paramgrow-probe.log). Unknown levels give no Main-based value.
    private static readonly IReadOnlyDictionary<int, int> MainByLevel = new Dictionary<int, int>
    {
        [1] = 20, [2] = 21, [3] = 22, [4] = 24, [5] = 26, [6] = 27, [7] = 29, [8] = 31, [9] = 33, [10] = 35,
        [11] = 36, [12] = 38, [13] = 41, [14] = 44, [15] = 46, [16] = 49, [17] = 52, [18] = 54, [19] = 57, [20] = 60,
        [21] = 63, [22] = 67, [23] = 71, [24] = 74, [25] = 78, [26] = 81, [27] = 85, [28] = 89, [29] = 92, [30] = 97,
        [31] = 101, [32] = 106, [33] = 110, [34] = 115, [35] = 119, [36] = 124, [37] = 128, [38] = 134, [39] = 139, [40] = 144,
        [41] = 150, [42] = 155, [43] = 161, [44] = 166, [45] = 171, [46] = 177, [47] = 183, [48] = 189, [49] = 196, [50] = 202,
        [51] = 204, [52] = 205, [53] = 207, [54] = 209, [55] = 210, [56] = 212, [57] = 214, [58] = 215, [59] = 217, [60] = 218,
        [61] = 224, [62] = 228, [63] = 236, [64] = 244, [65] = 252, [66] = 260, [67] = 268, [68] = 276, [69] = 284, [70] = 292,
        [71] = 296, [72] = 300, [73] = 305, [74] = 310, [75] = 315, [76] = 320, [77] = 325, [78] = 330, [79] = 335, [80] = 340,
        [81] = 345, [82] = 350, [83] = 355, [84] = 360, [85] = 365, [86] = 370, [87] = 375, [88] = 380, [89] = 385, [90] = 390,
        [91] = 395, [92] = 400, [93] = 405, [94] = 410, [95] = 415, [96] = 420, [97] = 425, [98] = 430, [99] = 435, [100] = 440,
    };
    public static int? MainOf(int level) => MainByLevel.TryGetValue(level, out var main) ? main : null;

    public static IReadOnlyList<DerivedStat> Combat(IReadOnlyDictionary<string, int> values, int category, int role, LevelModifiers m)
    {
        var rows = new List<DerivedStat>();
        int Value(string key) => values.TryGetValue(key, out var value) ? value : 0;
        static string Percent(double value) => value.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture) + " %";
        if (m.Div <= 0) return rows;
        var crit = Value("CriticalHit");
        rows.Add(new("CritRate", Percent(Math.Floor(200.0 * (crit - m.Sub) / m.Div + 50) / 10), crit, "CriticalHit"));
        rows.Add(new("CritDamage", Percent(Math.Floor(200.0 * (crit - m.Sub) / m.Div + 1400) / 10), crit, "CriticalHit"));
        var direct = Value("DirectHitRate");
        rows.Add(new("DirectRate", Percent(Math.Floor(550.0 * (direct - m.Sub) / m.Div) / 10), direct, "DirectHitRate"));
        if (m.Main > 0)
        {
            var determination = Value("Determination");
            rows.Add(new("Determination", Percent(Math.Floor(140.0 * (determination - m.Main) / m.Div) / 10), determination, "Determination"));
        }
        // Physical jobs use Skill Speed, spellcasters Spell Speed: the speed of the job's category.
        var speedKey = category == 31 ? "SpellSpeed" : "SkillSpeed";
        var speed = Value(speedKey);
        var gcd = Math.Floor(Math.Floor((1000 + Math.Ceiling(-130.0 * (speed - m.Sub) / m.Div)) * 2500 / 1000) / 10) / 100;
        rows.Add(new("Gcd", gcd.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture) + " s", speed, speedKey));
        rows.Add(new("SpeedBonus", Percent(Math.Floor(130.0 * (speed - m.Sub) / m.Div) / 10), speed, speedKey));
        if (role == 1)
        {
            var tenacity = Value("Tenacity");
            rows.Add(new("Tenacity", Percent(Math.Floor(112.0 * (tenacity - m.Sub) / m.Div) / 10), tenacity, "Tenacity"));
        }
        if (role == 4 && m.Main > 0)
        {
            var piety = Value("Piety");
            rows.Add(new("Piety", (200 + Math.Floor(150.0 * (piety - m.Main) / m.Div)).ToString("0", System.Globalization.CultureInfo.CurrentCulture), piety, "Piety"));
        }
        var defense = Value("Defense"); var magic = Value("MagicDefense");
        rows.Add(new("Defense", Percent(Math.Floor(15.0 * defense / m.Div)), defense, "Defense"));
        rows.Add(new("MagicDefense", Percent(Math.Floor(15.0 * magic / m.Div)), magic, "MagicDefense"));
        return rows;
    }

    /// <summary>Attributes useful to the job's category: combat, gathering (32) or crafting (33).</summary>
    public static IReadOnlyList<string> Useful(int category) => category switch
    {
        32 => ["Gathering", "Perception", "GatheringPoints"],
        33 => ["Craftsmanship", "Control", "CraftingPoints"],
        _ => [],
    };
}
