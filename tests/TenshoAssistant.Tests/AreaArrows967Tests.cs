// X-86 (0.967): a quest or event area above or below the player gets a faint chevron across it.
internal static class AreaArrows967Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Area arrows 967: chevron across the area, faint, only with height arrows on",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("var areaPlayerHeight=settings.HeightArrows") && map.Contains("MapHeight.Arrow(marker.Position.Y,areaPlayerHeight,settings.HeightArrowYalms)"),"arrow");
            Check(map.Contains("ImGui.GetColorU32(new Vector4(1,1,1,0.3f))"),"faint");
        }),
    ];
}
