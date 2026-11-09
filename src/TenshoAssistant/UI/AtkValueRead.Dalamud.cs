using FFXIVClientStructs.FFXIV.Component.GUI;

namespace TenshoAssistant.UI;

/// <summary>X-76 (step 2): how Tensho reads a game window's values, written once for every reader (framework thread, a copied value).</summary>
internal static class AtkValueRead
{
    /// <summary>A whole number: an Int as it is, a UInt up to int.MaxValue; any other type, or a larger UInt, is -1.</summary>
    public static int Number(AtkValue value)=>value.Type switch
    {
        AtkValueType.Int=>value.Int,
        AtkValueType.UInt=>value.UInt>int.MaxValue ? -1:(int)value.UInt,
        _=>-1,
    };

    /// <summary>Whether a value holds text, in any of the game's three kinds of string.</summary>
    public static bool IsText(AtkValueType type)=>type is AtkValueType.String or AtkValueType.ManagedString or AtkValueType.ConstString;
}
