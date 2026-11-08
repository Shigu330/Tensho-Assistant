// 0.902: « All » and « None » of the map's layers menu set every box of that menu (the monster layers were left out).
using System.Text.RegularExpressions;
internal static class MapLayersAll902Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map layers menu: every « Show… » box is set by All and None",() =>
        {
            var view=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            var start=view.IndexOf("BeginPopup(\"MapViewLayers\")",StringComparison.Ordinal);
            var end=view.IndexOf("finally{ImGui.EndPopup();}",start,StringComparison.Ordinal);
            Check(start>0 && end>start,"menu found");
            var menu=view[start..end];
            var all=menu[menu.IndexOf("void All(bool shown)",StringComparison.Ordinal)..menu.IndexOf("SmallButton(",StringComparison.Ordinal)];
            var boxes=Regex.Matches(menu,@"SettingBox\(""MapView\.(Show\w+)""").Select(match=>match.Groups[1].Value).Distinct().ToArray();
            Check(boxes.Length>=25,"boxes read: "+boxes.Length);
            var missing=boxes.Where(name=>!all.Contains($"settings.{name}=")).ToArray();
            Check(missing.Length==0,"left out of All and None: "+string.Join(", ",missing));
        }),
    ];
}
