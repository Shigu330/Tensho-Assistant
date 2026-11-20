using TenshoAssistant.Maps;

// 0.1583 (X-541, seen while reading test T-0.437-1): coordinates pasted from a hunt list keep their lines (the field is multi-line) and
// a zone named in another language of the game than the client's is found (MapBookmarks.ZoneByName, names from the PlaceName sheet).
internal static class PastedZones1583Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static readonly MapDefinition Western=new(20,140,"Thanalan","Thanalan occidental","",0,100,PlaceNameId:45);
    private static readonly MapDefinition Kozama=new(30,1188,"Tural","Kozama'uka","",0,100,PlaceNameId:4505);
    private static readonly Dictionary<uint,IReadOnlyList<string>> Other=new()
    {
        [45]=["Western Thanalan","Thanalan occidental","Westliches Thanalan"],
        [4505]=["Kozama'uka"],
    };

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pasted zones 1583: three lines, one English on a French client, one unknown: two bookmarks, one unknown",() =>
        {
            var zone=MapBookmarks.ZoneByName([Western,Kozama],Other);
            var text="Western Thanalan ( 12.3 , 7.4 )\r\nKozama'uka (x: 30.1, y: 15.2)\r\nNulle part ( 1.0 , 2.0 )";
            var (bookmarks,unknown)=MapBookmarks.ParseCoordinateList(text,zone,"Collé");
            Check(bookmarks.Count==2 && unknown==1,$"two bookmarks and one unknown, got {bookmarks.Count} and {unknown}");
            Check(bookmarks[0] is { MapId:20, X:12.3f, Y:7.4f } && bookmarks[0].Name=="Thanalan occidental (12.3, 7.4)","the English line takes the client's zone name");
            Check(bookmarks[1] is { MapId:30, X:30.1f },"the second line is its own bookmark");
        }),
        ("Pasted zones 1583: the client's name first, case ignored, no other names still works",() =>
        {
            Check(MapBookmarks.ZoneByName([Western],null)("thanalan occidental") == Western,"client name, any case");
            Check(MapBookmarks.ZoneByName([Western],null)("Western Thanalan") is null,"without the other names, the English name is unknown");
            Check(MapBookmarks.ZoneByName([Western],Other)("westliches thanalan") == Western,"German name");
            Check(MapBookmarks.ZoneByName([Western],Other)("Nowhere") is null,"unknown");
        }),
        ("Pasted zones 1583: the import field is multi-line and the zone names are read in the game's languages",() =>
        {
            var root=new DirectoryInfo(AppContext.BaseDirectory);
            while(root!=null && !Directory.Exists(Path.Combine(root.FullName,"src","TenshoAssistant")))root=root.Parent;
            var ui=File.ReadAllText(Path.Combine(root!.FullName,"src","TenshoAssistant","MainWindow.MapBookmarks.cs"));
            Check(ui.Contains("ImGui.InputTextMultiline(\"##BookmarkImport\"") && !ui.Contains("InputTextWithHint(\"##BookmarkImport\""),"multi-line field");
            Check(ui.Contains("MapBookmarks.ZoneByName(catalog.Entries, zoneNamesRead)"),"the import uses the names in every language");
        }),
    ];
}
