// X-100 (0.998, DEC-048): the navigation groups chosen by the user, in order; no page lost.
internal static class Navigation998Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Navigation 998: groups in order, pages in their group, labels in both languages",() =>
        {
            var pages=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.Pages.cs"));
            var groups=System.Text.RegularExpressions.Regex.Matches(pages,@"new\(Page\.\w+,""(\w+)""").Select(match=>match.Groups[1].Value).Distinct().ToArray();
            Check(groups.SequenceEqual(["Objects","Production","World","Tracking","Progression","Content","GoldSaucer","References","Preferences"]),"order: "+string.Join(",",groups));
            foreach(var (page,group) in new[]{("Alarms","Tracking"),("Garden","Tracking"),("FreeCompany","Tracking"),("Beastmaster","Content"),("DeepDungeons","Content"),("RelicWeapons","Content"),
                ("CustomDeliveries","Progression"),("Lexicon","References"),("Help","References"),("Credits","References"),("Settings","Preferences"),("Discoveries","World")})
                Check(pages.Contains($"new(Page.{page},\"{group}\""),page);
            foreach(var lang in new[]{"en","fr"})
            {
                var json=File.ReadAllText(Path.GetFullPath($"src/TenshoAssistant/Localization/Languages/{lang}.json"));
                foreach(var key in new[]{"CategoryTracking","CategoryContent","CategoryReferences"})Check(json.Contains($"\"{key}\""),lang+" "+key);
            }
        }),
    ];
}
