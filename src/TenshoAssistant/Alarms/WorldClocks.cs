using System.Globalization;

namespace TenshoAssistant.Alarms;

/// <summary>
/// g, X-10 (0.386): clocks for a few time zones chosen by the player (system time zone ids, 6 at most) and the date of a manual
/// one-off alarm typed in local time. Pure helpers; the time zone database is the operating system's.
/// </summary>
public static class WorldClocks
{
    public const int Capacity=6;

    public static TimeZoneInfo? Find(string id)
    {
        try{return TimeZoneInfo.FindSystemTimeZoneById(id);}
        catch(Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException){return null;}
    }

    /// <summary>Adds a zone once, up to the capacity; true when the list changed.</summary>
    public static bool Add(List<string> zones,string id)
    {
        if(zones.Count>=Capacity || zones.Contains(id,StringComparer.Ordinal) || Find(id) is null)return false;
        zones.Add(id);return true;
    }

    /// <summary>Local time in each known zone (unknown ids are left out).</summary>
    public static IReadOnlyList<(string Id,string Name,DateTimeOffset Time)> Times(IEnumerable<string>? zones,DateTimeOffset now)
        =>(zones ?? []).Select(id=>(id,Zone:Find(id))).Where(entry=>entry.Zone is not null)
            .Select(entry=>(entry.id,entry.Zone!.DisplayName,TimeZoneInfo.ConvertTime(now,entry.Zone!))).ToArray();

    /// <summary>
    /// A manual one-off alarm's moment from « yyyy-MM-dd HH:mm » in the PC's local time; null when the text is not a date or the moment
    /// is not in the future (up to a year ahead).
    /// </summary>
    public static DateTimeOffset? ParseLocal(string text,DateTimeOffset now,TimeZoneInfo? local=null)
    {
        if(!DateTime.TryParseExact((text ?? "").Trim(),"yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date))return null;
        var zone=local ?? TimeZoneInfo.Local;
        if(zone.IsInvalidTime(date))return null;
        var at=new DateTimeOffset(date,zone.GetUtcOffset(date)).ToUniversalTime();
        return at>now && at<=now.AddDays(366) ? at:null;
    }
}
