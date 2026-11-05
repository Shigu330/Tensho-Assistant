namespace TenshoAssistant.Reminders;

/// <summary>0.599 (T10): the pure part of Home's « Today » block and tile choices.</summary>
public static class HomeToday
{
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
