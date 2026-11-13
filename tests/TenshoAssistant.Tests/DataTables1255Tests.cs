// 0.1255 (X-154, version 4 of the visual base): one way to open a data table (BeginDataTable: striped rows, a thin line between rows,
// columns sharing the width) and numbers aligned on the right of their column (NumberCell). First pages: the garden's alarms, Weather
// (favourites and forecast), the wishlist and the gil journal.
internal static class DataTables1255Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string path)=>File.ReadAllText(Path.GetFullPath(path)).Replace("\r\n","\n");
    private static string Between(string text,string from,string to)
    {
        var start=text.IndexOf(from,StringComparison.Ordinal);
        if(start<0)throw new Exception("not found: "+from);
        var end=text.IndexOf(to,start+from.Length,StringComparison.Ordinal);
        return end<0 ? text[start..]:text[start..end];
    }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Data tables 1255: the shared opening and the right-aligned number",() =>
        {
            var components=Read("src/TenshoAssistant/UI/UiComponents.cs");
            var table=Between(components,"public static bool BeginDataTable(string id,int columns,bool sortable=false,float scrollHeight=0)","\n    }");
            Check(table.Contains("ImGuiTableFlags.RowBg|ImGuiTableFlags.BordersInnerH|ImGuiTableFlags.SizingStretchProp"),"striped rows, inner lines, shared width");
            Check(table.Contains("if(sortable)flags|=ImGuiTableFlags.Sortable;") && table.Contains("if(scrollHeight>0)flags|=ImGuiTableFlags.ScrollY;"),"sorting and a scrolling body on demand");
            var number=Between(components,"public static void NumberCell(string text,Vector4? colour=null)","\n    }");
            Check(number.Contains("ImGui.GetContentRegionAvail().X-ImGui.CalcTextSize(text).X") && number.Contains("if(room>0)ImGui.SetCursorPosX(ImGui.GetCursorPosX()+room);"),"pushed to the right edge, never to the left of the cell");
        }),
        ("Data tables 1255: the first pages open their tables through the component",() =>
        {
            Check(Read("src/TenshoAssistant/MainWindow.Garden.cs").Contains("UiComponents.BeginDataTable(\"GardenAlarms\",4)"),"garden alarms");
            var weather=Read("src/TenshoAssistant/MainWindow.Weather.cs");
            Check(weather.Contains("UiComponents.BeginDataTable(\"WeatherFavorites\",WeatherFavoritePeriods+1)") && weather.Contains("UiComponents.BeginDataTable(\"WeatherForecast\",5)")
                && !weather.Contains("ImGui.BeginTable("),"Weather: both tables");
            var wishlist=Read("src/TenshoAssistant/MainWindow.Wishlist.cs");
            Check(wishlist.Contains("UiComponents.BeginDataTable(\"Wishlist\",4)") && !wishlist.Contains("ImGui.BeginTable("),"wishlist");
            var gil=Read("src/TenshoAssistant/MainWindow.GilJournal.cs");
            Check(gil.Contains("UiComponents.BeginDataTable(\"GilTotals\"+source,4)") && gil.Contains("UiComponents.BeginDataTable(\"GilLatest\",5)") && !gil.Contains("ImGui.BeginTable("),"gil journal: both tables");
            Check(gil.Split("UiComponents.NumberCell(").Length-1==7,"gil journal: gained, spent, net (and the transfers' two), change and balance aligned on the right");
        }),
    ];
}
