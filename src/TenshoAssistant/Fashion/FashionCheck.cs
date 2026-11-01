using TenshoAssistant.Items;

namespace TenshoAssistant.Fashion;

/// <summary>
/// The game's Fashion Report window read live (0.213): the hint category of each of its 11 slots and the window rectangle.
/// Slot order of the window: weapon, head, body, hands, legs, feet, ears, neck, wrists, right ring, left ring.
/// </summary>
public sealed record FashionCheckFrame(IReadOnlyList<string> Categories, float X, float Y, float Width, float Height, DateTimeOffset ObservedAt,
    IReadOnlyList<FashionSlotRect>? Slots = null);
/// <summary>Screen rectangle of one slot row of the game window (0.219), where Tensho puts its button.</summary>
public readonly record struct FashionSlotRect(float X, float Y, float Width, float Height);

public sealed class FashionOptions
{
    public bool OwnedFirst { get; set; } = true;
    public bool DimUnowned { get; set; } = true;
    public bool ByPopularity { get; set; } = true;
    public bool ShowPanel { get; set; } = true;
}

public sealed class FashionCheckService
{
    private FashionCheckFrame? live, last;
    /// <summary>Frame while the game window is open, else null.</summary>
    public FashionCheckFrame? Live => Volatile.Read(ref live);
    /// <summary>Last frame read this session (kept after the window closes, for the Tensho page).</summary>
    public FashionCheckFrame? Last => Volatile.Read(ref last);
    internal void Publish(FashionCheckFrame? frame)
    {
        Volatile.Write(ref live, frame);
        if (frame is not null) Volatile.Write(ref last, frame);
    }
}

public static class FashionWeek
{
    public const int SlotCount = 11;
    /// <summary>Equipment kind of each slot of the game window, in its order (both rings take ring items).</summary>
    public static readonly EquipmentSlots[] Slots =
    [
        EquipmentSlots.MainHand, EquipmentSlots.Head, EquipmentSlots.Body, EquipmentSlots.Hands, EquipmentSlots.Legs, EquipmentSlots.Feet,
        EquipmentSlots.Ears, EquipmentSlots.Neck, EquipmentSlots.Wrists, EquipmentSlots.Ring, EquipmentSlots.Ring,
    ];
    // Weeks of the Fashion Report counted from Tuesday 2018-01-30 08:00 UTC (weekly reset); FashionCheckWeeklyTheme row = week + 9
    // (relation used by the community tools for this sheet; the shown theme name lets the player check it).
    private static readonly DateTimeOffset Start = new(2018, 1, 30, 8, 0, 0, TimeSpan.Zero);

    public static int Number(DateTimeOffset now) => now < Start ? 0 : (int)((now - Start).Ticks / TimeSpan.FromDays(7).Ticks);
    public static uint ThemeRow(DateTimeOffset now) => (uint)(Number(now) + 9);

    /// <summary>Start of the current week's report (Tuesday 08:00 UTC) and of the next one.</summary>
    public static (DateTimeOffset Start, DateTimeOffset Next) Bounds(DateTimeOffset now)
    {
        var start = Start.AddDays(7 * Number(now));
        return (start, start.AddDays(7));
    }
}
