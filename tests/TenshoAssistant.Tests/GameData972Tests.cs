using TenshoAssistant.Diagnostics;

// X-92 (0.972): what is new in the game's sheets after an update, and what needs a look in Tensho's own tables.
internal static class GameData972Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }

    private static GameDataSnapshot Snap(string version,params (string Name,int Rows,int Columns,uint Last)[] sheets)
        =>new(version,sheets.ToDictionary(sheet=>sheet.Name,sheet=>new SheetShape(sheet.Rows,sheet.Columns,sheet.Last)),sheets.Select(sheet=>sheet.Name).ToArray());

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Game data 972: first snapshot, unchanged, new rows and columns, needs a look",() =>
        {
            var before=Snap("ffxiv=1",("Item",100,90,500),("IKDRoute",20,10,20),("Quest",50,1500,70000));
            Check(GameDataChanges.Compare(null,before).First,"first");
            Check(!GameDataChanges.Compare(before,before).Changed,"unchanged");
            var now=Snap("ffxiv=2",("Item",110,91,520),("IKDRoute",22,10,22),("Quest",50,1500,70000),("NewSheet",1,1,0));
            var report=GameDataChanges.Compare(before,now);
            Check(report.Changed && report.NewSheets.SequenceEqual(["NewSheet"]),"new sheet");
            Check(report.Changes.Count==2 && report.Changes.Any(change=>change is { Sheet:"Item", RowsAdded:10, ColumnsAdded:1, LastRowNow:520 }),"item change");
            Check(report.NeedsLook.Count==1 && report.NeedsLook[0].StartsWith("IKDRoute:",StringComparison.Ordinal),"needs a look");
            Check(GameDataChanges.Summary(report).NeedsLook && GameDataChanges.Lines(report).Any(line=>line.Contains("Item: +10 rows",StringComparison.Ordinal)),"lines");
        }),
        ("Game data 972: unreadable is not gone, file round trip, broken file read as none",() =>
        {
            var before=Snap("v",("Item",1,1,1),("Fate",1,1,1));
            var now=new GameDataSnapshot("v",new Dictionary<string,SheetShape>{["Item"]=new(1,1,1)},["Item","Fate"]);
            Check(!GameDataChanges.Compare(before,now).Changed,"unreadable");
            var gone=new GameDataSnapshot("v",new Dictionary<string,SheetShape>{["Item"]=new(1,1,1)},["Item"]);
            Check(GameDataChanges.Compare(before,gone).Changes.Single() is { Sheet:"Fate", Gone:true },"gone");
            Check(GameDataChanges.Read(GameDataChanges.Write(before)) is { } back && back.Sheets["Fate"]==new SheetShape(1,1,1),"round trip");
            Check(GameDataChanges.Read("{broken")==null,"broken");
        }),
        ("Game data 972: checked once per load off the frame, in the log and the self-check, cancelled on unload",() =>
        {
            var plugin=File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/Plugin.cs"));
            Check(plugin.Contains("Diagnostics.GameDataWatch.Start(data,") && plugin.Contains("gameDataCancel.Cancel();"),"plugin");
            Check(File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/MainWindow.SelfCheckPages.cs")).Contains("Page(\"game data\""),"self-check");
        }),
    ];
}
