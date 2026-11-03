using TenshoAssistant.Maps;

// N-11 (0.437): pasted « Place ( x , y ) » lines (hunt sites, chat) imported as map bookmarks.
internal static class PastedCoordinates437Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Pasted coordinates: zone found at the end of the text, icons and labels ignored, unknown places counted",() =>
        {
            var zones=new Dictionary<string,MapDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["Thanalan occidental"]=new(20,140,"Thanalan","Thanalan occidental","",0,100),
                ["Kozama'uka"]=new(30,1188,"Tural","Kozama'uka","",0,100),
            };
            MapDefinition? Zone(string name)=>zones.GetValueOrDefault(name);
            var text="Siren: Thanalan occidental ( 12.3 , 7.4 )\nB rank – Kozama'uka (x: 30.1, y: 15.2)\nNowhere ( 1.0 , 2.0 )\nno coordinates here";
            var (bookmarks,unknown)=MapBookmarks.ParseCoordinateList(text,Zone,"Pasted");
            Check(bookmarks.Count==2 && unknown==1,"two found, one unknown place, a line without coordinates skipped");
            Check(bookmarks[0] is { MapId:20, TerritoryId:140, X:12.3f, Y:7.4f, Group:"Pasted" } && bookmarks[0].Name=="Thanalan occidental (12.3, 7.4)","first");
            Check(bookmarks[1] is { MapId:30, X:30.1f, Y:15.2f },"x: / y: labels");
            Check(MapBookmarks.ParseCoordinateList("",Zone,"g").Bookmarks.Count==0,"empty");
        }),
    ];
}
