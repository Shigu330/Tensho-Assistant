using System.Text.Json;
using System.Text.RegularExpressions;
using TenshoAssistant.Alarms;

// X-573 M3 (review 0.1539): « Alarms still ring from the app (a line in its status bar, Windows' sound and a Windows notification; by default
// only while the game is not the window in front) ». FaqTruth1538Tests tied the sentence to the desktop app's X-163 hook only; here each
// part is tied to what makes it true: the defaults of AlarmSettings, the settings that change them, and the code that applies them.
internal static class FaqAlarms1542Tests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n", "\n");

    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

    private static string Value(string json, string key)
    {
        var found = Regex.Matches(json, "^    \"" + Regex.Escape(key) + "\": (\".*\"),?$", RegexOptions.Multiline);
        Check(found.Count == 1, $"one key {key} ({found.Count})");
        return JsonSerializer.Deserialize<string>(found[0].Groups[1].Value) ?? "";
    }

    private static readonly (string Language, string Sound, string Notice, string Behind)[] Said =
    [
        ("en", "Windows' sound", "notification", "by default only while the game is not the window in front"),
        ("fr", "le son de Windows", "notification", "par défaut seulement quand le jeu n'est pas la fenêtre au premier plan"),
    ];

    public static IEnumerable<(string Name, Action Run)> Cases =>
    [
        ("FAQ alarms 1542: X-573 M3, the defaults the FAQ and the README state are AlarmSettings' defaults: sound on, notification on, only while the game is behind", () =>
        {
            var defaults = new AlarmSettings();
            Check(defaults.DesktopSound && defaults.DesktopNotification && defaults.DesktopOnlyGameBehind,
                $"defaults: sound {defaults.DesktopSound}, notification {defaults.DesktopNotification}, only behind {defaults.DesktopOnlyGameBehind}");
            var readme = Flat(Read("README.md"));
            foreach (var (language, sound, notice, behind) in Said)
            {
                var faq = Flat(Value(Read("src/TenshoAssistant/Localization/Languages/" + language + ".json"), "AnswerDesktop"));
                foreach (var (where, text) in new[] { ("FAQ", faq), ("README", readme) })
                    Check(text.Contains(sound, StringComparison.Ordinal) && text.Contains(notice, StringComparison.Ordinal) && text.Contains(behind, StringComparison.Ordinal),
                        $"{language} {where}: sound, notification and « {behind} »");
            }
        }),
        ("FAQ alarms 1542: X-573 M3, the three settings exist in both languages with the same defaults, and the code applies them", () =>
        {
            var alarms = Read("src/TenshoAssistant/MainWindow.Alarms.cs");
            foreach (var name in new[] { "DesktopSound", "DesktopOnlyGameBehind", "DesktopNotification" })
            {
                Check(alarms.Contains($"BoolSetting(\"Alarms.{name}\",\"Alarms.{name}Tip\",()=>configuration.AlarmOptions?.{name}!=false,", StringComparison.Ordinal),
                    name + ": a setting, on unless turned off (the default the text states)");
                foreach (var language in new[] { "en", "fr" })
                    Check(Regex.IsMatch(Read("src/TenshoAssistant/Localization/Languages/" + language + ".json"), "^    \"" + name + "\": \"", RegexOptions.Multiline),
                        language + ": the label of " + name);
            }
            var data = Read("src/TenshoAssistant/MainWindow.DesktopData.cs");
            Check(Regex.Matches(data, Regex.Escape("options.DesktopOnlyGameBehind && gameInFront)return ([],false);")).Count == 2,
                "the alarms and the link's alerts are silent while the game is in front, when that option is on");
            Check(data.Contains("internal bool DesktopAlarmNotification()=>(configuration.AlarmOptions ?? new()).DesktopNotification;", StringComparison.Ordinal),
                "the notification follows its option");
            Check(data.Contains("return (texts,options.DesktopSound);", StringComparison.Ordinal), "the sound follows its option");
            Check(Read("src/TenshoAssistant.Desktop/StatusBar.cs").Contains("host.AlarmNotification())notice.Show(Window,lines);", StringComparison.Ordinal),
                "the desktop app shows the notification only when asked");
        }),
    ];
}
