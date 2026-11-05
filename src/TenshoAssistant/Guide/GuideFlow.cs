namespace TenshoAssistant.Guide;

public enum GuideStep { Welcome, Sources, Settings, Interests, Windows, Visit, End }
public enum GuideInterest { Crafting, Gathering, Collections, TripleTriad, FreeCompany, Market }
/// <summary>A page shown during the visit; no interest: for everyone.</summary>
public sealed record GuideStop(string Page, GuideInterest? Interest = null);
/// <summary>A game window to open once, for what it lets Tensho read; no interest: for everyone.</summary>
public sealed record GuideWindowTask(string Key, GuideInterest? Interest = null);
/// <summary>Stop: the page reached in the visit, 0 otherwise.</summary>
public readonly record struct GuidePosition(GuideStep Step, int Stop);

/// <summary>
/// First-run guide (ONBOARD-001, lots 4 to 6, DEC-031): the steps, their order, what the chosen interests keep. No interface and no game state here.
/// The player acts in the game; the guide only says what to open, and sees when the game has given the data. It can be left at any step.
/// </summary>
public static class GuideFlow
{
    private static readonly GuideStop[] AllStops =
    [
        new("Items"), new("Storage"), new("Market", GuideInterest.Market), new("CraftSimulation", GuideInterest.Crafting), new("Gathering", GuideInterest.Gathering),
        new("Maps"), new("Alarms"), new("Reminders"), new("FreeCompany", GuideInterest.FreeCompany), new("Quests"), new("Collections", GuideInterest.Collections),
        new("Gear"), new("TripleTriad", GuideInterest.TripleTriad), new("Settings"), new("Help"),
    ];
    private static readonly GuideWindowTask[] AllWindows =
    [
        new("Retainer"), new("Dresser"), new("Armoire"), new("CompanyChest", GuideInterest.FreeCompany), new("Workshop", GuideInterest.FreeCompany),
        new("CraftingLog", GuideInterest.Crafting), new("Titles", GuideInterest.Collections), new("Achievements", GuideInterest.Collections), new("Bestiary", GuideInterest.Collections),
    ];
    private static IEnumerable<T> Kept<T>(IEnumerable<T> all, Func<T, GuideInterest?> of, IReadOnlySet<GuideInterest>? chosen)
        => chosen is not { Count: > 0 } ? all : all.Where(row => of(row) is not { } interest || chosen.Contains(interest));

    /// <summary>Nothing chosen: everything is shown. Nothing is ever hidden outside the guide.</summary>
    public static IReadOnlyList<GuideStop> Stops(IReadOnlySet<GuideInterest>? chosen) => Array.AsReadOnly(Kept(AllStops, stop => stop.Interest, chosen).ToArray());
    public static IReadOnlyList<GuideWindowTask> Windows(IReadOnlySet<GuideInterest>? chosen) => Array.AsReadOnly(Kept(AllWindows, task => task.Interest, chosen).ToArray());

    public static GuidePosition Next(GuidePosition at, int stops)
    {
        if (at.Step == GuideStep.Visit && at.Stop + 1 < stops) return at with { Stop = at.Stop + 1 };
        if (at.Step == GuideStep.End) return at;
        var next = at.Step + 1;
        return new(next == GuideStep.Visit && stops == 0 ? GuideStep.End : next, 0);
    }

    public static GuidePosition Previous(GuidePosition at, int stops)
    {
        if (at.Step == GuideStep.Visit && at.Stop > 0) return at with { Stop = at.Stop - 1 };
        if (at.Step == GuideStep.Welcome) return at;
        var previous = at.Step - 1;
        if (previous == GuideStep.Visit && stops == 0) previous = GuideStep.Windows;
        return new(previous, previous == GuideStep.Visit ? stops - 1 : 0);
    }

    /// <summary>From 0 to 1: every step and every stop of the visit counts for one.</summary>
    public static float Progress(GuidePosition at, int stops)
    {
        var before = (int)at.Step + (at.Step > GuideStep.Visit ? Math.Max(0, stops - 1) : at.Step == GuideStep.Visit ? at.Stop : 0);
        var total = (int)GuideStep.End + Math.Max(0, stops - 1);
        return total == 0 ? 1 : Math.Clamp((float)before / total, 0, 1);
    }

    /// <summary>A saved position that no longer exists (interests changed, older version) comes back inside the guide.</summary>
    public static GuidePosition Valid(int step, int stop, int stops)
    {
        var known = (GuideStep)Math.Clamp(step, (int)GuideStep.Welcome, (int)GuideStep.End);
        if (known != GuideStep.Visit) return new(known, 0);
        return stops == 0 ? new(GuideStep.End, 0) : new(known, Math.Clamp(stop, 0, stops - 1));
    }

    /// <summary>A new install opens it once; anyone else is only offered it, once. Never imposed.</summary>
    public static bool OpensByItself(bool newInstall, bool done, bool offered) => newInstall && !done && !offered;
    public static bool Offered(bool newInstall, bool done, bool offered) => !newInstall && !done && !offered;

    public static IReadOnlySet<GuideInterest> ParseInterests(IEnumerable<string>? saved)
    {
        var chosen = new HashSet<GuideInterest>();
        foreach (var name in saved ?? []) if (Enum.TryParse<GuideInterest>(name, out var interest)) chosen.Add(interest);
        return chosen;
    }
}
