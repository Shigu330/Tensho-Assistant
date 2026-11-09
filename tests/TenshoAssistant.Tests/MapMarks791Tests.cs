// 0.791 (X-01): where the map's 110-160 ms frames go, measured in parts (developer mode).
internal static class MapMarks791Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map marks: developer mode only, started with the canvas, in order",() =>
        {
            var map=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.MapView.cs"));
            Check(map.Contains("private void MapMarkStart(){if(DeveloperMode)") && map.Contains("if(!DeveloperMode)return;"),"developer mode");
            var parts=new[]{"MapMarkStart();","MapMark(\"canvas texture, names, zones\");","MapMark(\"canvas gathering points\");","MapMark(\"canvas areas\");","MapMark(\"canvas icons\");","MapMark(\"canvas bookmarks, treasure, player\");","finally{MapMark(\"canvas edges and tooltips\");draw.PopClipRect();}"};
            for(var index=1;index<parts.Length;index++)Check(map.IndexOf(parts[index-1])<map.IndexOf(parts[index]) && map.IndexOf(parts[index-1])>0,"order "+index);
            foreach(var mark in new[]{"canvas vendors","canvas icon list","canvas icons drawn","canvas nearby objects","canvas monsters","canvas menus","canvas nearest aetheryte","canvas end"})
                Check(map.Contains("MapMark(\""+mark+"\");"),"0.805: "+mark);
            Check(map.Contains("MeasureMap(\"map overlay controls\","),"layout split (the canvas: SlowFrame745Tests)");
        }),
    ];
}
