// 0.861 (X-73.4): the Fashion Report's categories and theme read in the game; the outfit judged kept raw.
internal static class FashionGame861Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fashion: the game's category id first when known, the name match after; the judgement kept raw",() =>
        {
            var page=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FashionWeek.cs"));
            Check(page.Contains("if(frame.CategoryIds is { } ids && slot<ids.Count && ids[slot] is var id and not 0 && fashionHints.State.Catalog?.Hints.Any(hint=>hint.Id==id)==true)return id;"),"id first");
            Check(page.Contains("var hint=FashionHintOf(frame,slot);"),"used for both lists");
            var reader=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Fashion/FashionCheckReader.Dalamud.cs"));
            Check(reader.Contains("manager->Theme.ItemThemes[slot]") && reader.Contains("if (evaluations.Count > 0 && signature != judgedSeen)"),"read, once per change");
            Check(new TenshoAssistant.Discoveries.DiscoverySettings().FashionReport,"on by default");
        }),
    ];
}
