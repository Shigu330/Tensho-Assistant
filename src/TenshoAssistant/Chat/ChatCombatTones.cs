namespace TenshoAssistant.Chat;

/// <summary>X-801 (0.1809): the kind of a combat line for its colour in the Tensho chat window.</summary>
public enum ChatCombatTone { None,DamageDone,DamageTaken,HealDone,HealTaken }

/// <summary>
/// X-801 (0.1809, the user's request of 22/11: the Tensho chat's combat lines all in one colour, the game's in colours with the icon of the type
/// of damage): a combat line's colour by its kind (41 damage, 45 healing: ChatFilterTable.Genre) and its side (done by you, or done to you:
/// Dalamud's XivChatRelationKind, ChatAlert.You). The message carries no colour of the game for these lines (developer-mode line of 22/11 14:27,
/// kind 41: text, icon, text; docs/evidence/0.1809-chat-combat-tones.md): the colours are Tensho's, red for damage and green for healing, each
/// changeable, and one setting brings back the channel's single colour. Display only: no number is read nor counted (DEC-053, R03.1). Pure.
/// </summary>
public static class ChatCombatTones
{
    public const int Damage=41,Healing=45;
    public static readonly IReadOnlyList<ChatCombatTone> All=[ChatCombatTone.DamageDone,ChatCombatTone.DamageTaken,ChatCombatTone.HealDone,ChatCombatTone.HealTaken];

    /// <summary>The tone of a line: damage or healing done by you, or done to you; None for every other line.</summary>
    public static ChatCombatTone Of(int kind,byte source,byte target)=>kind switch
    {
        Damage when source==ChatAlert.You=>ChatCombatTone.DamageDone,
        Damage when target==ChatAlert.You=>ChatCombatTone.DamageTaken,
        Healing when source==ChatAlert.You=>ChatCombatTone.HealDone,
        Healing when target==ChatAlert.You=>ChatCombatTone.HealTaken,
        _=>ChatCombatTone.None,
    };

    /// <summary>The default colour of a tone (0xRRGGBB): light red for the damage you do, a deeper red for the damage you take, light green for
    /// your healing, a deeper green for the healing you receive.</summary>
    public static uint DefaultColor(ChatCombatTone tone)=>tone switch
    {
        ChatCombatTone.DamageDone=>0xFF8C7A,ChatCombatTone.DamageTaken=>0xE0503C,ChatCombatTone.HealDone=>0x8CE68C,ChatCombatTone.HealTaken=>0x4CC864,_=>0xB0B0B0,
    };

    /// <summary>The icons of the game's font that say a type of damage in a combat line (Dalamud 15's BitmapFontIcon, tools/ApiProbe of 22/11:
    /// DamagePhysical 117, DamageMagical 118, DamageSpecial 119; BluntDamage 185, PiercingDamage 186, SlashingDamage 187, MagicalDamage 188).
    /// The window draws them as the game's chat does, instead of « · ».</summary>
    public static bool IsDamageIcon(uint icon)=>icon is >= 117 and <= 119 or >= 185 and <= 188;
}
