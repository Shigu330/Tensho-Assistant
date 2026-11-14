// 0.1220 (X-258, X-261, first-player review of 12/11, G7 and G11). A German or Japanese game client starts Tensho in English
// (those languages translate a few titles only): only a complete language is taken from the client, and the menu marks the partial
// ones. Numbers follow the language of Tensho, never the culture of Windows: « 1,234 » and « 12.5% » in English, « 1 234 » and
// « 12,5 % » in French (no-break spaces), whatever Windows says; the English texts lose their French spaces (« {0} % », « {0} h »).
using System.Globalization;
using System.Text.RegularExpressions;
using TenshoAssistant.Character;
using TenshoAssistant.Localization;

[SerialTests("LocalizationManager.SetLanguage and TenshoCulture.Use set the culture of the whole process")]
internal static class LanguageFormats1220Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path) => File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static LocalizationManager Manager() => new((error,message) => { },Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")));

    private static void WithWindowsCulture(string name,Action run)
    {
        var before = CultureInfo.CurrentCulture;
        var tensho = TenshoCulture.Current;
        try { CultureInfo.CurrentCulture = new CultureInfo(name); run(); }
        finally { CultureInfo.CurrentCulture = before; TenshoCulture.Use(ReferenceEquals(tensho,TenshoCulture.For("fr")) ? "fr" : "en"); }
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Languages 1220: German and Japanese are partial, never taken from the game client, still chosen by hand",() =>
        {
            var loc = Manager();
            var partial = loc.AvailableLanguages.ToDictionary(language => language.Code,language => language.Partial);
            Check(!partial["en"] && !partial["fr"],"English and French are complete");
            Check(partial["de"] && partial["ja"],"German and Japanese are partial");
            loc.SelectInitialLanguage(null,"German");
            Check(loc.CurrentCode == "en","a German client starts in English");
            loc.SelectInitialLanguage(null,"Japanese");
            Check(loc.CurrentCode == "en","a Japanese client starts in English");
            loc.SelectInitialLanguage(null,"French");
            Check(loc.CurrentCode == "fr","a French client starts in French");
            loc.SelectInitialLanguage("de","English");
            Check(loc.CurrentCode == "de","a saved choice of German is kept");
            loc.SetLanguage("en");
            Check(loc.Format("Localization.PartialLanguage","Deutsch") == "Deutsch (partial)","the menu says partial");
            loc.SetLanguage("fr");
            Check(loc.Format("Localization.PartialLanguage","Deutsch") == "Deutsch (partiel)","in French too");
            loc.SetLanguage("en");
            Check(LocalizationManager.IsPartial(new Dictionary<string,string>{["a"]="",["b"]="",["_meta.code"]="x"},3) && !LocalizationManager.IsPartial(new Dictionary<string,string>{["a"]="",["b"]="",["c"]=""},3),"nine texts in ten");
            var settings = Read("src/TenshoAssistant/MainWindow.Settings.cs");
            Check(settings.Contains("ImGui.Selectable(LanguageLabel(language)",StringComparison.Ordinal) && settings.Contains("language.Partial ? localization.Format(\"Localization.PartialLanguage\"",StringComparison.Ordinal),"menu wired");
        }),
        ("Languages 1220: numbers follow Tensho's language, not Windows'",() =>
        {
            WithWindowsCulture("de-DE",() =>
            {
                var loc = Manager();
                loc.SetLanguage("en");
                Check(loc.Format("Home.Catalogues",1234567,2500) == "1,234,567 items · 2,500 recipes loaded","English Tensho on a German Windows: " + loc.Format("Home.Catalogues",1234567,2500));
                Check(TenshoCulture.Percent(12.5,"0.0") == "12.5%" && TenshoCulture.Hours(3,5) == "3h 05m" && TenshoCulture.Colon == ":","English percent, hours and colon");
                Check(1234.ToString("N0",TenshoCulture.Current) == "1,234","English group separator");
                var stats = StatFormulas.Combat(new Dictionary<string,int>{["CriticalHit"]=3000},30,2,new LevelModifiers(440,420,2780));
                Check(stats.First(row => row.Key == "CritRate").Value == "23.5%","stats in English: " + stats.First(row => row.Key == "CritRate").Value);
                loc.SetLanguage("fr");
                Check(loc.Format("Home.Catalogues",1234567,2500).StartsWith("1 234 567 ",StringComparison.Ordinal),"French Tensho: no-break spaces: " + loc.Format("Home.Catalogues",1234567,2500));
                Check(TenshoCulture.Percent(12.5,"0.0") == "12,5 %" && TenshoCulture.Hours(3,5) == "3 h 05" && TenshoCulture.Colon == " :","French percent, hours and colon");
                Check(StatFormulas.Combat(new Dictionary<string,int>{["CriticalHit"]=3000},30,2,new LevelModifiers(440,420,2780)).First(row => row.Key == "CritRate").Value == "23,5 %","stats in French");
                loc.SetLanguage("en");
            });
        }),
        ("Languages 1220: the English texts write « 12%» and « 3h », and the code no longer writes « {0} % » or Windows' culture",() =>
        {
            var english = Read("src/TenshoAssistant/Localization/Languages/en.json");
            var spaced = english.Split('\n').Where(line => !Regex.IsMatch(line,"^\\s*\"[A-Za-z]+Formula\":") && Regex.IsMatch(line,"(\\d|\\}) %")).ToArray();
            Check(spaced.Length == 0,"no « {0} % » in English: " + string.Join(" | ",spaced.Take(3)));
            Check(!Regex.IsMatch(english,"(\\{\\d\\}|\\d) h\\b"),"no « {0} h » in English");
            foreach (var file in new[] { "MainWindow.GearWear.cs","MainWindow.Currencies.cs","MainWindow.FreeCompany.cs","MainWindow.GatheringObservation.cs","MainWindow.Weather.cs","Alarms/CurrencyDtrEntry.Dalamud.cs","MainWindow.AirshipPlanner.cs","Alarms/AlarmDtrEntry.Dalamud.cs" })
            {
                var code = Read("src/TenshoAssistant/" + file);
                Check(!code.Contains("} %\"",StringComparison.Ordinal) && !code.Contains("} h {",StringComparison.Ordinal),file + ": percent and hours through TenshoCulture");
            }
            var formulas = Read("src/TenshoAssistant/Character/StatFormulas.cs");
            Check(!formulas.Contains("CultureInfo.CurrentCulture",StringComparison.Ordinal),"stats never in Windows' culture");
            Check(Read("src/TenshoAssistant/Localization/LocalizationManager.cs").Contains("string.Format(TenshoCulture.For(CurrentCode), Get(key), args)",StringComparison.Ordinal),"Format in Tensho's culture");
        }),
    ];
}
