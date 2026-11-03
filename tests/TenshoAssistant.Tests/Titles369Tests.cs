// 0.369 (g, X-03): wear a title through the game's function (address checked), random title, title by job off by default.
internal static class Titles369Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Titles: game function checked first, two seconds apart, by job off by default and never at login",() =>
        {
            var changer = Read("Collections/TitleChanger.Dalamud.cs");
            Check(changer.Contains("(nint)TitleController.MemberFunctionPointers.SendTitleIdUpdate==0",StringComparison.Ordinal)
                && changer.Contains("TimeSpan.FromSeconds(2)",StringComparison.Ordinal),"address checked, spaced");
            Check(changer.Contains("if(!first && byJob() && jobTitles()?.TryGetValue(job,out var title)==true)",StringComparison.Ordinal),"by job: on a real change only, when on");
            Check(!new TenshoAssistant.Configuration().JobTitlesEnabled,"off by default");
            var configuration = Read("Configuration.cs");
            Check(configuration.Contains("\"JobTitles\",",StringComparison.Ordinal) && configuration.Contains("\"UndercutClipboard\", \"JobTitlesEnabled\",",StringComparison.Ordinal),
                "map kept, switch reset");
            Check(Read("Plugin.cs").Contains("()=>titleChanger?.Dispose()",StringComparison.Ordinal),"released");
        }),
    ];
}
