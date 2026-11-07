// 0.834 (X-64, user report): « Go there » for an item only found as a hidden item of its point.
internal static class HiddenGoThere834Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Go there: hidden sources ranked last, not left out",() =>
        {
            var menu=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.DutyFinder.cs"));
            Check(!menu.Contains("sources.Where(source=>!source.IsHidden)"),"not left out");
            Check(menu.Contains(".OrderBy(pair=>pair.Source.IsHidden ? 1:0)"),"ranked last");
        }),
    ];
}
