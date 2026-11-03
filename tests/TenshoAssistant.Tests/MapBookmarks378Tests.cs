using TenshoAssistant.Maps;

// 0.378 (g, M-08): bookmarks with an optional note and group (older ones load unchanged), shared as a TENSHO-MAP1 code, merged
// without duplicates within the capacity, drawn on the Tensho map.
internal static class MapBookmarks378Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file) => File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/" + file));
    private static MapBookmark Mark(string name,float x,string? note=null,string? group=null)=>new(Guid.NewGuid(),name,10,20,x,12,note,group);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Map bookmarks: note and group checked, older bookmarks without them still valid",() =>
        {
            Check(MapBookmarks.CleanNote("  ")==null && MapBookmarks.CleanNote("a\nb")=="a\nb" && MapBookmarks.CleanGroup(" Trains ")=="Trains","cleaned");
            var thrown = 0;
            try{MapBookmarks.CleanNote(new string('x',301));}catch(ArgumentException){thrown++;}
            try{MapBookmarks.CleanGroup("a\nb");}catch(ArgumentException){thrown++;}
            Check(thrown==2,"limits");
            var old = new MapBookmark(Guid.NewGuid(),"Old",10,20,5,5);
            Check(MapBookmarks.Add([],old).Count==1 && old.Note is null && old.Group is null,"older bookmark");
        }),
        ("Map bookmarks: share code round trip, malformed codes refused, merge skips duplicates and stops at the capacity",() =>
        {
            IReadOnlyList<MapBookmark> mine = [Mark("A",5,"note","G"),Mark("B",6)];
            var code = MapBookmarks.ShareCode(mine);
            Check(code.StartsWith(MapBookmarks.SharePrefix,StringComparison.Ordinal) && !code.Contains('='),"prefixed base64url");
            var back = MapBookmarks.ParseShareCode(code);
            Check(back.Count==2 && back[0].Name=="A" && back[0].Note=="note" && back[0].Group=="G" && back[0].Id!=mine[0].Id,"round trip with new ids");
            foreach (var bad in new[]{"","TENSHO-GEAR1:abc",MapBookmarks.SharePrefix+"!!!",MapBookmarks.SharePrefix+"W10"})
            {
                var refused = false;
                try{MapBookmarks.ParseShareCode(bad);}catch(FormatException){refused=true;}
                Check(refused,"refused: "+bad);
            }
            var merged = MapBookmarks.Merge(mine,[..back,Mark("C",7)]);
            Check(merged.Added==1 && merged.Skipped==2 && merged.Bookmarks.Count==3,"duplicates skipped");
            var full = Enumerable.Range(0,MapBookmarks.Capacity).Select(index=>Mark("P"+index,1+index*0.1f)).ToArray();
            Check(MapBookmarks.Merge(full,[Mark("Z",40)]).Added==0,"capacity kept");
        }),
        ("Map bookmarks: drawn on the Tensho map and added from its right-click menu",() =>
        {
            var view = Read("MainWindow.MapView.cs");
            Check(view.Contains("GlyphBookmark=0xFFFF0005",StringComparison.Ordinal) && view.Contains("FlagFromMap(map,clicked.X,clicked.Y)",StringComparison.Ordinal)
                && view.Contains("MapBookmarks.Add(items,MapBookmarks.Create(map,name,",StringComparison.Ordinal),"layer, click and menu");
            Check(new TenshoAssistant.MapViewSettings().ShowBookmarks,"on by default");
        }),
    ];
}
