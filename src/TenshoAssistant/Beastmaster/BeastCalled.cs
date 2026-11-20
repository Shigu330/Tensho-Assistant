namespace TenshoAssistant.Beastmaster;

/// <summary>X-655 (0.1599): where the beast called was read: the player's own familiar on the field, or the game's « Cornes d'appel » window.</summary>
public enum BeastCalledSource : byte { None, Familiar, Window }

/// <summary>X-655 (0.1599): the beast called (its XBMPet row, 0: none) and where it was read.</summary>
public sealed record BeastCalledRead(uint Beast, BeastCalledSource Source)
{
    public static readonly BeastCalledRead None = new(0, BeastCalledSource.None);
}

/// <summary>
/// X-655 (0.1599, the user's report of 20/11 10:45: no « 1 » or « 2 » in the labyrinth). The log of 20/11 showed the cause: the game's
/// « Cornes d'appel » window (XBMActivePet) is open only while the team is chosen (10:37:20 to 10:37:40); in the fight (10:38:13) it is
/// closed, the beast read fell back to 0 and the highlight stopped without a word (docs/evidence/0.1599-beast-called.md). The beast called is
/// now read first on the field: the player's own battle NPC whose name is a beast's name (one beast only, else none, R08); the window only
/// when no familiar is found. Read only: nothing is sent to the game (R03.3).
/// </summary>
public static class BeastCalled
{
    /// <summary>The beast called: the familiar's name first (the names of the player's own battle NPCs), then the window's beast.</summary>
    public static BeastCalledRead Resolve(IReadOnlyList<string> owned, uint windowBeast, IReadOnlyList<Beast>? beasts)
    {
        uint found = 0;
        if (beasts is { Count: > 0 })
            foreach (var name in owned)
            {
                if (name.Length == 0) continue;
                var beast = beasts.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.CurrentCultureIgnoreCase));
                if (beast is null) continue;
                if (found != 0 && found != beast.Id) return BeastCalledRead.None;
                found = beast.Id;
            }
        if (found != 0) return new(found, BeastCalledSource.Familiar);
        return windowBeast > 0 ? new(windowBeast, BeastCalledSource.Window) : BeastCalledRead.None;
    }
}

/// <summary>One hotbar slot where a combo skill was found: the game's bar window, the slot (from 1), and whether it is the second skill.</summary>
public readonly record struct BeastComboSlot(string Bar, int Slot, bool Second);

/// <summary>
/// X-655 (0.1599): the log line that says where the combo highlight stops, or which bar and slot carry « 1 » and « 2 »; said once per change
/// by the drawing.
/// </summary>
public static class BeastComboTrace
{
    public static string Line(bool option, bool locator, BeastCalledRead called, uint before, uint after, bool read, IReadOnlyList<BeastComboSlot> slots)
    {
        if (!option) return "Beast combo highlight: off (option).";
        if (!locator) return "Beast combo highlight: nothing drawn: the hotbars are not read (no reader).";
        if (called.Beast == 0) return "Beast combo highlight: nothing drawn: no beast called read (no familiar of the player named as a beast, battlehorns window closed).";
        if (before == 0 || after == 0) return $"Beast combo highlight: nothing drawn: beast {called.Beast} has no known Trick affinity.";
        if (!read) return $"Beast combo highlight: beast {called.Beast} ({called.Source}), skills {before} (1) and {after} (2): hotbars not read yet.";
        if (slots.Count == 0) return $"Beast combo highlight: beast {called.Beast} ({called.Source}), skills {before} (1) and {after} (2): on no visible hotbar slot.";
        var where = string.Join(", ", slots.Select(slot => $"{(slot.Second ? "2" : "1")} on {slot.Bar} slot {slot.Slot}"));
        return $"Beast combo highlight: beast {called.Beast} ({called.Source}), skills {before} (1) and {after} (2): {where}.";
    }
}
