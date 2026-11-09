using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Alarms;

// Queue step e (audit 2026-11-02): « No sound in duties » can also keep the [Tensho] chat line out of a duty (MuteChatInDuties, off by
// default, so the line stays as before); Discord and the alarms copied into the game are not affected, and the edit of the alarm
// settings on the settings page keeps every AlarmSettings field (a copy since X-76 H3, 0.914).
internal static class AlarmChatInDutiesTests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static readonly string Source = Path.GetFullPath("src/TenshoAssistant");
    private static string Read(string file) => File.ReadAllText(Path.Combine(Source,file));
    private static string Quoted(string text) => '"' + text + '"';

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Alarm chat line in duties: off by default it stays; on, it is dropped only with « No sound in duties » and in a duty",() =>
        {
            var fresh = new AlarmSettings();
            Check(!fresh.MuteChatInDuties && fresh.MuteInDuties && fresh.Chat && fresh.PlaySound,"defaults");
            Check(!new TenshoAssistant.Configuration().AlarmOptions.MuteChatInDuties,"configuration default off");
            Check(fresh.ChatAllowed(true) && fresh.ChatAllowed(false),"default: the chat line stays in a duty, as before");
            Check(!fresh.SoundAllowed(true) && fresh.SoundAllowed(false),"default: no sound in a duty, sound outside, as before");
            var muted = new AlarmSettings { MuteChatInDuties = true };
            Check(!muted.ChatAllowed(true) && muted.ChatAllowed(false) && !muted.SoundAllowed(true),"on: neither line nor sound in a duty, line outside");
            var parentOff = new AlarmSettings { MuteInDuties = false, MuteChatInDuties = true };
            Check(parentOff.ChatAllowed(true) && parentOff.SoundAllowed(true),"no effect while « No sound in duties » is off");
            Check(!new AlarmSettings { Chat = false }.ChatAllowed(false) && !new AlarmSettings { PlaySound = false }.SoundAllowed(false),"chat or sound off stays off");
        }),
        ("Alarm service: the chat line and the sound follow the shared rules, Discord does not",() =>
        {
            var service = Read("Alarms/AlarmService.Dalamud.cs");
            var rule = service.IndexOf("var inDuty = settings.MuteInDuties && condition[ConditionFlag.BoundByDuty];",StringComparison.Ordinal);
            var chat = service.IndexOf("if (settings.ChatAllowed(inDuty))",StringComparison.Ordinal);
            var sound = service.IndexOf("if (settings.SoundAllowed(inDuty))",StringComparison.Ordinal);
            var discord = service.IndexOf("if (discord)",StringComparison.Ordinal);
            Check(rule > 0 && chat > rule && sound > chat && discord > sound,"duty read, then chat line, sound and Discord");
            Check(!service.Contains("if (settings.Chat)",StringComparison.Ordinal),"no chat line outside the rule");
            Check(!service[discord..].Contains("inDuty",StringComparison.Ordinal) && !service[discord..].Contains("BoundByDuty",StringComparison.Ordinal),"Discord is still sent in a duty");
        }),
        ("Alarm settings page: the edit keeps every AlarmSettings field; the new row has its tooltip and reset button",() =>
        {
            var page = Read("MainWindow.Alarms.cs");
            // X-76 H3 (0.914): the page edits a Copy() of the saved options instead of rebuilding them field by field, so no field can be
            // forgotten (the command delay, the duty ready alert and the automatic alarms are saved on their own and must survive).
            Check(page.Contains("var next=configuration.AlarmOptions.Copy();",StringComparison.Ordinal) && !page.Contains("configuration.AlarmOptions=new(){",StringComparison.Ordinal),"edited on a copy");
            var source = new AlarmSettings();
            foreach (var property in typeof(AlarmSettings).GetProperties().Where(property => property.CanWrite))
                property.SetValue(source,property.GetValue(source) switch { bool on => !on, int number => number + 7, var other => throw new Exception("new field type to cover: " + property.Name + " " + other) });
            var copy = source.Copy();
            var lost = typeof(AlarmSettings).GetProperties().Where(property => property.CanWrite && !Equals(property.GetValue(copy),property.GetValue(source))).Select(property => property.Name).ToArray();
            Check(!ReferenceEquals(copy,source) && lost.Length == 0,"fields lost when the alarm settings are copied: " + string.Join(", ",lost));
            Check(page.Contains("Tip(" + Quoted("Advanced.MuteChatTip") + ");",StringComparison.Ordinal),"tooltip");
            Check(page.Contains("DefaultButton(" + Quoted("AlarmMuteChat") + ",muteChat!=defaults.MuteChatInDuties)",StringComparison.Ordinal),"reset button");
            Check(page.Contains("var muteChatOff=!mute || !chat;",StringComparison.Ordinal),"greyed while the sound silence or the chat line is off");
        }),
        ("Alarm chat line in duties: French and English texts, both tooltips end with their default",() =>
        {
            foreach (var (code,parentEnd,ownEnd) in new[] { ("fr","\nPar défaut : activé.","\nPar défaut : désactivé."),("en","\nDefault: on.","\nDefault: off.") })
            {
                using var json = JsonDocument.Parse(Read("Localization/Languages/" + code + ".json"));
                var label = json.RootElement.GetProperty("Alarms").GetProperty("MuteChatInDuties").GetString();
                var advanced = json.RootElement.GetProperty("Advanced");
                var tip = advanced.GetProperty("MuteChatTip").GetString(); var parent = advanced.GetProperty("MuteTip").GetString();
                Check(!string.IsNullOrWhiteSpace(label),code + ": label");
                Check(tip is not null && tip.EndsWith(ownEnd,StringComparison.Ordinal),code + " tip: " + tip);
                Check(parent is not null && parent.EndsWith(parentEnd,StringComparison.Ordinal),code + " parent tip: " + parent);
            }
        }),
    ];
}
