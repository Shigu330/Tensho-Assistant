namespace TenshoAssistant.Reminders;

/// <summary>T27 (0.651): an active company action: its Status row and its end (null = not known; docs/evidence/0.651-company-actions.md).</summary>
public sealed record CompanyBenefit(uint Status,DateTimeOffset? Ends);

public static class CompanyBenefits
{
    /// <summary>
    /// The player's company statuses, paired in order with the game's timers (seconds left at their last update, and seconds since it).
    /// The timers stay at 0 until the player opens the Actions tab. If their number differs from the statuses', or one has run out, no end is said.
    /// </summary>
    public static IReadOnlyList<CompanyBenefit> Build(IReadOnlyList<uint> active,IEnumerable<uint> timers,uint since,DateTimeOffset now)
    {
        var running=timers.Where(left=>left>0).ToArray();
        var paired=running.Length==active.Count && running.All(left=>left>since);
        return active.Select((status,index)=>new CompanyBenefit(status,paired ? now.AddSeconds(running[index]-(double)since):null)).ToArray();
    }
}
