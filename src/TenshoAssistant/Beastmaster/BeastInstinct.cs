namespace TenshoAssistant.Beastmaster;

/// <summary>X-136 (0.1530): the four instinctual affinities of the Inner Compass (« Table des types »), clockwise (GuidePageString 161, 162).</summary>
public enum Affinity : byte { None, Volant, Rampant, Durant, Eldritch }
/// <summary>X-136 (0.1530): the two kinds of intentional combo (« combo instinctif II »): Sunstrider (« Assaut vital »), Moonstalker (« Assaut létal »).</summary>
public enum IntentionalCombo : byte { None, Sunstrider, Moonstalker }

/// <summary>
/// X-136 (0.1530, the user's request of 11/11): each beast's Trick affinity and the beastmaster's skill to chain with it, from the game itself
/// (docs/evidence/0.1530-beast-instinct.md). A beast's Trick is the action 44933 + 2 × its XBMPet row (the fifty Tricks follow the fifty beasts:
/// each Trick's description holds its beast's Assault text, XBMPet column 9, fifty out of fifty); its affinity is the « Type : … » / « Affinity:
/// … » line of its ActionTransient. The beastmaster's instinctual skills are 44889 (Volant), 44884 (Rampant), 44887 (Durant), 44888 (Eldritch):
/// « grants you Instinct … ». An intentional combo is a second skill that runs clockwise on the Compass (Volant → Rampant → Durant → Eldritch →
/// Volant), whoever strikes first; Volant → Rampant and Durant → Eldritch are Sunstrider, Rampant → Durant and Eldritch → Volant Moonstalker
/// (EventTutorialPage 36, 37: the Cu Sith's Trick is Rampant; Gale Axe first, or Mistral Axe after it).
/// </summary>
public static class BeastInstinct
{
    public const uint FirstTrick = 44933;
    private const Affinity V = Affinity.Volant, R = Affinity.Rampant, D = Affinity.Durant, E = Affinity.Eldritch;
    // XBMPet rows 1 to 50, game 2026.11.03 (ActionTransient of 44935 to 45033, French and English agree).
    private static readonly Affinity[] Tricks =
    [
        R, R, R, D, R, E, E, R, D, V,
        V, R, E, R, D, D, E, D, V, V,
        D, R, E, D, E, R, D, E, R, R,
        E, V, E, D, R, E, R, R, R, V,
        D, D, D, V, E, V, D, D, E, E,
    ];
    public static readonly IReadOnlyDictionary<Affinity, uint> TrainerSkill = new Dictionary<Affinity, uint>
    {
        [Affinity.Volant] = 44889, [Affinity.Rampant] = 44884, [Affinity.Durant] = 44887, [Affinity.Eldritch] = 44888,
    };

    /// <summary>The Trick of a beast (its XBMPet row); 0 for a row with no known Trick.</summary>
    public static uint Trick(uint beast) => beast >= 1 && beast <= Tricks.Length ? FirstTrick + 2 * beast : 0;
    public static Affinity Of(uint beast) => beast >= 1 && beast <= Tricks.Length ? Tricks[beast - 1] : Affinity.None;
    public static Affinity Next(Affinity affinity) => affinity switch
    {
        Affinity.Volant => Affinity.Rampant, Affinity.Rampant => Affinity.Durant, Affinity.Durant => Affinity.Eldritch, Affinity.Eldritch => Affinity.Volant, _ => Affinity.None,
    };
    public static Affinity Previous(Affinity affinity) => affinity switch
    {
        Affinity.Volant => Affinity.Eldritch, Affinity.Rampant => Affinity.Volant, Affinity.Durant => Affinity.Rampant, Affinity.Eldritch => Affinity.Durant, _ => Affinity.None,
    };
    /// <summary>The intentional combo of two instinctual skills in this order (none when the second does not run clockwise after the first).</summary>
    public static IntentionalCombo Kind(Affinity first, Affinity second)
        => first == Affinity.None || second != Next(first) ? IntentionalCombo.None : first is Affinity.Volant or Affinity.Durant ? IntentionalCombo.Sunstrider : IntentionalCombo.Moonstalker;

    /// <summary>One way to complete an intentional combo with a beast: the beastmaster's skill, before the Trick or after it.</summary>
    public sealed record Combo(uint Skill, Affinity SkillAffinity, bool AfterTrick, IntentionalCombo Kind);

    /// <summary>The two intentional combos of a beast: its skill before the Trick (the affinity before it), then after it (the one after).</summary>
    public static IReadOnlyList<Combo> Combos(uint beast)
    {
        var trick = Of(beast);
        if (trick == Affinity.None) return [];
        var before = Previous(trick);
        var after = Next(trick);
        return [new(TrainerSkill[before], before, false, Kind(before, trick)), new(TrainerSkill[after], after, true, Kind(trick, after))];
    }
}
