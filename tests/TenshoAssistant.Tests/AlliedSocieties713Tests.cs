using TenshoAssistant.Societies;

// W-10 (0.713): the allied societies.
internal static class AlliedSocieties713Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static AlliedSociety Society(uint row,int max=8)=>new(row,"s"+row,"e","p",max,0,0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Allied societies: a rank beyond the sheets is refused, not shown",() =>
        {
            AlliedSociety[] societies=[Society(1)];
            Check(AlliedBoard.Check(societies,new Dictionary<uint,AlliedStanding>{[1]=new(8,0,0)},9)==AlliedReadStatus.Loaded,"last rank");
            Check(AlliedBoard.Check(societies,new Dictionary<uint,AlliedStanding>{[1]=new(9,0,0)},9)==AlliedReadStatus.Unavailable,"beyond");
        }),
        ("Allied societies: rank names come from the sheet, and the last rank shows as such",() =>
        {
            string[] names=["Aucun","Neutre","Reconnu"];
            Check(AlliedBoard.RankName(names,1)=="Neutre" && AlliedBoard.RankName(names,7)=="7","names");
            Check(AlliedBoard.AtMax(Society(1,5),new(5,0,0)) && !AlliedBoard.AtMax(Society(1,5),new(4,0,0)),"max");
            Check(!AlliedBoard.AtMax(Society(1,0),new(9,0,0)),"no max known");
        }),
        ("Allied societies: the page only reads, on the framework thread, once the character is loaded, and places have the right click",() =>
        {
            Check(Read("Plugin.cs").Contains("mainWindow.ReadAllied=catalog=>framework.RunOnFrameworkThread(()=>"),"framework thread");
            Check(Read("Societies/AlliedSocieties.Dalamud.cs").Contains("player->IsLoaded"),"loaded first");
            Check(Read("MainWindow.AlliedSocieties.cs").Contains("PlaceMenu($\"AlliedPlace{society.Row}\""),"right click");
        }),
    ];
}
