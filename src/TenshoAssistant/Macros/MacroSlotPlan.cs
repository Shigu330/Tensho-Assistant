namespace TenshoAssistant.Macros;

/// <summary>Where crafting macros may be written in the game (0.191): book (0 individual, 1 shared) and slot range, both included.</summary>
public sealed class MacroCopySettings
{
    public int Book { get; set; }
    public int Start { get; set; } = 80;
    public int End { get; set; } = 89;
    public string Prefix { get; set; } = "Tensho";
    // Off by default: only empty slots or slots already written by Tensho (name starting with Prefix) are used.
    public bool OverwriteAny { get; set; }
    // Tensho Macro Chain (0.196): pages end with « /tenshonext » and the written slots become the chain plan.
    public bool Chain { get; set; } = true;
}

public readonly record struct MacroSlotState(bool Empty, string Name);
public sealed record MacroSlotPlanResult(IReadOnlyList<int> Slots, string? Problem);

/// <summary>
/// Picks the game macro slots for the pages of a crafting macro, inside the configured range only: consecutive usable slots from Start,
/// a slot being usable when empty, already named by Tensho, or when overwriting is allowed. All pages or none.
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
            var ours = settings.Prefix.Length > 0 && state.Name.StartsWith(settings.Prefix, StringComparison.Ordinal);
            if (state.Empty || ours || settings.OverwriteAny) chosen.Add(index);
        }
        return chosen.Count == pages ? new(chosen.AsReadOnly(), null) : new([], "NoRoom");
    }

    /// <summary>Macro name in the game (20 characters at most): prefix, page and page count.</summary>
    public static string Name(string prefix, int page, int pages)
    {
        var name = $"{prefix} {page}/{pages}".Trim();
        return name.Length > 20 ? name[..20] : name;
    }
}
