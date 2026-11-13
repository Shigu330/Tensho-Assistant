using System.Text.RegularExpressions;

namespace TenshoAssistant.Alarms;

/// <summary>
/// Blocks of the Alarms page (0.202): the item, or the weather / vista / name the alarm was created for. X-295 (0.1296, user request of
/// 14/11 about 0:25): the alarms made at a date (Alarms › alarm at a date) all bore the group « Dated alarms », so they shared one block with
/// that title; each is now a block of its own titled with its name. They are recognised by that stored group, in whatever language it was
/// written (datedGroups: the text in every language).
/// </summary>
public static class AlarmGroups
{
    public static bool IsDated(TenshoAlarm alarm,IReadOnlyCollection<string> datedGroups)
        =>alarm.ItemId==0 && alarm.Group.Length>0 && datedGroups.Contains(alarm.Group);

    public static string Key(TenshoAlarm alarm,IReadOnlyCollection<string> datedGroups)
        =>alarm.ItemId!=0 ? $"item:{alarm.ItemId}"
            :IsDated(alarm,datedGroups) ? $"dated:{alarm.Id:N}"
            :alarm.Group.Length>0 ? alarm.Group
            :Regex.Replace(alarm.Label,@" · \d\d:\d\d ET$","");

    /// <summary>The block title of a dated alarm: its name, else the fallback (« Dated alarm · » and its date).</summary>
    public static string DatedTitle(TenshoAlarm alarm,Func<TenshoAlarm,string> fallback)
        =>alarm.Label.Trim() is { Length:>0 } name ? name:fallback(alarm);
}
