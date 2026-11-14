namespace TenshoAssistant.UI;

/// <summary>
/// X-279 (0.1292, user report of 13/11 about 21:55, Alarms › Alarms at a date: « next 00:00 (in 24631 min) », the hour alone for an alarm of 1/12):
/// the one way to write a time left, as the garden and the Home page already wrote it (0.1256): « 17 d 2 h », « 3 h 05 », « 45 min » through the
/// keys Common.LeftDays, Common.LeftHours and Common.LeftMinutes, each language its own form. Pure.
/// </summary>
public static class TimeLeft
{
    /// <summary>A time left in whole days, hours and minutes, rounded down (never early); zero once passed.</summary>
    public static (int Days,int Hours,int Minutes) Split(TimeSpan left)
    {
        if(left<=TimeSpan.Zero)return (0,0,0);
        var minutes=(long)Math.Floor(left.TotalMinutes);
        return ((int)Math.Min(int.MaxValue,minutes/1440),(int)(minutes/60%24),(int)(minutes%60));
    }

    /// <summary>The text of a time left: its key (Common.LeftDays « {0} d {1} h », LeftHours « {0} h {1:00} », LeftMinutes « {0} min ») and its two numbers.</summary>
    public static (string Key,int First,int Second) Text((int Days,int Hours,int Minutes) left)
        =>left.Days>0 ? ("Common.LeftDays",left.Days,left.Hours):left.Hours>0 ? ("Common.LeftHours",left.Hours,left.Minutes):("Common.LeftMinutes",left.Minutes,0);

    /// <summary>
    /// X-312 (0.1316): a time left rounded up to the whole minute, for the countdowns that never say « 0 min » while time is left (GATE, Eureka,
    /// vista, boarding, alarms); zero once passed.
    /// </summary>
    public static TimeSpan UpToMinute(TimeSpan left)=>left<=TimeSpan.Zero ? TimeSpan.Zero:TimeSpan.FromMinutes(Math.Ceiling(left.TotalMinutes));

    /// <summary>X-312 (0.1316): a length rounded to the nearest whole minute (a fishing window's length); zero when negative.</summary>
    public static TimeSpan NearestMinute(TimeSpan length)=>length<=TimeSpan.Zero ? TimeSpan.Zero:TimeSpan.FromMinutes(Math.Round(length.TotalMinutes));

    /// <summary>
    /// X-312 (0.1316): the whole text of a time left through a formatter (the localization's Format with the key and its two numbers), for the
    /// places outside the main window (the alarm's chat line, the Discord message).
    /// </summary>
    public static string Write(TimeSpan left,Func<string,int,int,string> format)
    {
        ArgumentNullException.ThrowIfNull(format);
        var (key,first,second)=Text(Split(left));
        return format(key,first,second);
    }

    /// <summary>True when <paramref name="at"/> falls on the same day as <paramref name="now"/> in that time zone: the hour alone is then enough.</summary>
    public static bool SameDay(DateTimeOffset at,DateTimeOffset now,TimeZoneInfo zone)=>TimeZoneInfo.ConvertTime(at,zone).Date==TimeZoneInfo.ConvertTime(now,zone).Date;
}
