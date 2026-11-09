// 0.859 (X-73.3): the game's fishing log flags first; a community window or weather is left out when the game says there is none.
internal static class FishRestrictions859Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Fish restrictions: the game's « none » hides the community window and weather; unknown keeps them",() =>
        {
            var panel=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.FishingPanel.cs"));
            Check(panel.Contains("if(timeLimited!=false && known.StartMinute is { } start && known.EndMinute is { } stop)"),"window hidden only on « none »");
            Check(panel.Contains("if(weatherLimited!=false && known.Weathers.Count>0)") && panel.Contains("if(weatherLimited!=false && found.Weather is { Count:>0 } names)"),"weather, both sources");
            Check(panel.Contains("return list.All(value=>value==false) ? false:list.All(value=>value==true) ? true:null;"),"several rows must agree");
        }),
    ];
}
