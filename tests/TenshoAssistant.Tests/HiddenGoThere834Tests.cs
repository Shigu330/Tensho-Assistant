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
            Check(menu.Contains("GatherAccess?.Invoke(source.BaseId)?.Blocks(source.Level)!=true"),"0.835: unavailable left out");
            var access=new TenshoAssistant.Gathering.GatherPointAccess(null,17,60);
            Check(!access.Blocks(60) && access.Blocks(61),"level");
            Check(new TenshoAssistant.Gathering.GatherPointAccess("Tome",17,90).Blocks(10),"tome");
        }),
    ];
}
