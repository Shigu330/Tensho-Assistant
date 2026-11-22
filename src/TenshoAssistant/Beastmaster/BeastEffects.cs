namespace TenshoAssistant.Beastmaster;

/// <summary>
/// X-755 (0.1750, the user's reading R-X-553 of 20/11: « lenteur, pétrification/congélation, paralysie, silence, cécité, étourdissement,
/// sommeil, entrave, pesanteur, dégâts/mort subite, poison », the order of the game's icons): the effects a beast inflicts, in the order the
/// game's windows show them. XBMPet columns 11 to 21 are eleven flags; the game's team window (XBMPetParty) gives for each beast the icons
/// 72461 to 72471, the names (Addon 12472 to 12481, the Masked Carnivale's list, then 17755 « Poison ») and the flags in that order: Poison,
/// column 16, comes last (docs/evidence/0.1750-beast-effects.md: 294 beast rows of the window matched against the sheet). Pure (tests).
/// </summary>
public static class BeastEffects
{
    /// <param name="Column">The XBMPet column of the flag.</param>
    /// <param name="Name">The Addon row of its name (the client's language).</param>
    /// <param name="Icon">The game's icon of it.</param>
    public readonly record struct Effect(int Column,uint Name,uint Icon);

    /// <summary>The eleven effects in the game's order.</summary>
    public static readonly IReadOnlyList<Effect> Order=
    [
        new(11,12472,72461), new(12,12473,72462), new(13,12474,72463), new(14,12475,72464), new(15,12476,72465), new(17,12477,72466),
        new(18,12478,72467), new(19,12479,72468), new(20,12480,72469), new(21,12481,72470), new(16,17755,72471),
    ];

    /// <summary>The places in <see cref="Order"/> of the effects whose flag is set, in the game's order.</summary>
    public static IReadOnlyList<int> Of(Func<int,bool> flag)=>Enumerable.Range(0,Order.Count).Where(at=>flag(Order[at].Column)).ToArray();
}
