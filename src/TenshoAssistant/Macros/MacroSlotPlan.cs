namespace TenshoAssistant.Macros;

/// <summary>Where crafting macros may be written in the game (0.191): book (0 individual, 1 shared) and slot range, both included.</summary>
public sealed class MacroCopySettings
{
    public int Book { get; set; }
    public int Start { get; set; } = 80;
    public int End { get; set; } = 89;
    public string Prefix { get; set; } = "Tensho";
    // Off by default: only empty slots or slots already written by Tensho (named exactly « Prefix page/pages », see IsTenshoName) are used.
    public bool OverwriteAny { get; set; }
    // Tensho Macro Chain (0.196): pages end with « /tenshonext » and the written slots become the chain plan.
    public bool Chain { get; set; } = true;
}

public readonly record struct MacroSlotState(bool Empty, string Name);
public sealed record MacroSlotPlanResult(IReadOnlyList<int> Slots, string? Problem);

/// <summary>
/// Picks the game macro slots for the pages of a crafting macro, inside the configured range only: usable slots from Start, in order (occupied slots are skipped),
/// a slot being usable when empty, named exactly as <see cref="Name"/> writes it, or when overwriting is allowed. All pages or none.
/// </summary>
public static class MacroSlotPlan
{
    public const int Slots = 100;

    public static MacroSlotPlanResult Plan(int pages, MacroCopySettings settings, Func<int, MacroSlotState> slot)
    {
        ArgumentNullException.ThrowIfNull(settings); ArgumentNullException.ThrowIfNull(slot);
        if (pages <= 0) return new([], "Empty");
        if (settings.Book is not (0 or 1) || settings.Start is < 0 or >= Slots || settings.End is < 0 or >= Slots || settings.End < settings.Start)
            return new([], "Range");
        var chosen = new List<int>();
        for (var index = settings.Start; index <= settings.End && chosen.Count < pages; index++)
        {
            var state = slot(index);
            if (state.Empty || settings.OverwriteAny || IsTenshoName(settings.Prefix, state.Name)) chosen.Add(index);
        }
        return chosen.Count == pages ? new(chosen.AsReadOnly(), null) : new([], "NoRoom");
    }

    /// <summary>
    /// True when the trimmed name is exactly what <see cref="Name"/> writes for this prefix (« Tensho 2/3 »: prefix, space, page/pages,
    /// 1 ≤ page ≤ pages ≤ 99). 0.330: no longer any name starting with the prefix (« Crafting buffs » with the prefix « Craft »). An empty
    /// or blank prefix recognises no name, since « 1/3 » may be the player's own macro.
    /// </summary>
    public static bool IsTenshoName(string? prefix, string? name)
    {
        var text = (name ?? "").Trim();
        var space = text.LastIndexOf(' ');
        if (string.IsNullOrWhiteSpace(prefix) || space <= 0 || text[(space + 1)..].Split('/') is not [var first, var second]
            || !Number(first, out var page) || !Number(second, out var pageCount)) return false;
        return page >= 1 && page <= pageCount && Name(prefix, page, pageCount) == text;
    }

    // One or two ASCII digits, no sign (a leading zero, never written by Name, fails the comparison with Name above).
    private static bool Number(string text, out int value)
    {
        value = 0;
        if (text.Length is not (1 or 2) || !text.All(char.IsAsciiDigit)) return false;
        value = int.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>Macro name in the game (20 characters at most): prefix, page and page count.</summary>
    public static string Name(string prefix, int page, int pages)
    {
        var name = $"{prefix} {page}/{pages}".Trim();
        return name.Length > 20 ? name[..20] : name;
    }
}
