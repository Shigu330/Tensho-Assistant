namespace TenshoAssistant.Reminders;

/// <summary>X-155 (0.1261): something with a time the player waits for (an alarm, a vessel's return, a garden bed): its kind (the tool it
/// opens), its words and its time.</summary>
public sealed record HomeDeadline(string Kind,string Label,DateTimeOffset At);

/// <summary>0.599 (T10): the pure part of Home's « Today » block and tile choices.</summary>
public static class HomeToday
{
    /// <summary>X-155 (0.1261): the nearest deadline still to come (one that passed is not a deadline any more); ties by kind, then words.</summary>
    public static HomeDeadline? Next(IEnumerable<HomeDeadline>? deadlines,DateTimeOffset now)
        =>(deadlines ?? []).Where(deadline=>deadline.At>now).OrderBy(deadline=>deadline.At).ThenBy(deadline=>deadline.Kind,StringComparer.Ordinal).ThenBy(deadline=>deadline.Label,StringComparer.Ordinal).FirstOrDefault();

    /// <summary>The first names and how many more, on one line. more: a format with {0} for the number left.</summary>
    public static string Short(IReadOnlyList<string> names,int shown,string more)
    {
        var first=string.Join(", ",names.Take(shown));
        return names.Count<=shown ? first:first+" "+string.Format(System.Globalization.CultureInfo.CurrentCulture,more,names.Count-shown);
    }

    /// <summary>The list of what is hidden, after a box was ticked or unticked: null when nothing stays, so a saved file keeps its defaults.</summary>
    public static List<string>? Toggle(IReadOnlyCollection<string>? hidden,string key,bool shown)
    {
        var rest=(hidden ?? []).Where(row=>row!=key).ToList();
        if(!shown)rest.Add(key);
        return rest.Count==0 ? null:rest;
    }
}
