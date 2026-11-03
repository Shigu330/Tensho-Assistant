namespace TenshoAssistant.Reminders;

/// <summary>
/// Game reset times (R-08, 0.300). Read from the game's own Calendar on 2026-11-02 (docs/evidence/0.300-calendar-resets.md):
/// Grand Company supply missions 20:00 UTC daily; custom deliveries and Fashion Report Tuesday 08:00 UTC weekly; leve allowances
/// every 12 h at 00:00 / 12:00 UTC (the game also gives that time, used when read). Daily duties (allied society quests) 15:00 UTC.
/// </summary>
public static class ResetSchedule
{
    public const int SupplyHour=20,DailyHour=15,WeeklyHour=8;
    public const DayOfWeek WeeklyDay=DayOfWeek.Tuesday;

    /// <summary>The next time after <paramref name="now"/> that the clock reads <paramref name="hourUtc"/>:00 UTC.</summary>
    public static DateTimeOffset NextDaily(DateTimeOffset now,int hourUtc)
    {
        var utc=now.ToUniversalTime();
        var today=new DateTimeOffset(utc.Year,utc.Month,utc.Day,hourUtc,0,0,TimeSpan.Zero);
        return today>utc ? today:today.AddDays(1);
    }

    /// <summary>The next <paramref name="day"/> at <paramref name="hourUtc"/>:00 UTC after <paramref name="now"/>.</summary>
    public static DateTimeOffset NextWeekly(DateTimeOffset now,DayOfWeek day,int hourUtc)
    {
        var next=NextDaily(now,hourUtc);
        while(next.DayOfWeek!=day)next=next.AddDays(1);
        return next;
    }

    /// <summary>The last reset at or before <paramref name="now"/> (one period before the next).</summary>
    public static DateTimeOffset PreviousDaily(DateTimeOffset now,int hourUtc)=>NextDaily(now,hourUtc).AddDays(-1);
    public static DateTimeOffset PreviousWeekly(DateTimeOffset now,DayOfWeek day,int hourUtc)=>NextWeekly(now,day,hourUtc).AddDays(-7);

    /// <summary>A task ticked at <paramref name="doneAt"/> still counts as done when no reset happened since.</summary>
    public static bool DoneSince(DateTimeOffset? doneAt,DateTimeOffset lastReset)=>doneAt is { } done && done>=lastReset;
}
