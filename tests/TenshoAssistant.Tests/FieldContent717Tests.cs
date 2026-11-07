using TenshoAssistant.Fields;

// W-14 (0.717): Eureka and Bozja.
internal static class FieldContent717Tests
{
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); }
    private static string Read(string file)=>File.ReadAllText(Path.GetFullPath("src/TenshoAssistant/"+file));
    private static FieldEventRow Row(FieldEventKind kind,uint id,uint instance,int? level)=>new(kind,id,instance,"n"+id,"",level,0);

    public static IEnumerable<(string Name,Action Run)> Cases =>
    [
        ("Eureka and Bozja: the layout places the rows of its own zone, once, FATEs first by level",() =>
        {
            FieldLayoutEntry[] entries=[new(10,1,2),new(20,3,4),new(30,5,6),new(10,9,9)];
            FieldEventRow[] rows=[Row(FieldEventKind.Engagement,1,30,null),Row(FieldEventKind.Fate,2,20,9),Row(FieldEventKind.Fate,3,10,4),Row(FieldEventKind.Fate,4,99,1)];
            var placed=FieldBoard.Place(entries,rows);
            Check(placed.Select(row=>row.Id).SequenceEqual([3u,2u,1u]),"order, and another zone's row left out");
            Check(placed[0].X==1 && placed[0].Z==2,"the first entry of an instance");
            Check(placed[2].Level is null,"no level stays unknown");
            Check(FieldBoard.Place(entries,[new(FieldEventKind.Fate,5,10,"","",1,0)]).Count==0,"a row with no name");
        }),
        ("Eureka and Bozja: the six zones, and the search on name or text",() =>
        {
            Check(FieldBoard.Territories.SequenceEqual([732u,763u,795u,827u,920u,975u]),"zones");
            Check(FieldBoard.Matches("",["a"]) && FieldBoard.Matches("BC",["x","abcd"]) && !FieldBoard.Matches("z",["x"]),"search");
        }),
        ("Eureka and Bozja: the files are read off the frame, only once checked, and a place has its right click",() =>
        {
            var page=Read("MainWindow.FieldContent.cs");
            Check(page.Contains("fieldLoad=Task.Run(load)"),"off the frame");
            Check(page.Contains("PlaceMenu($\"FieldPlace"),"right click");
            Check(Read("Fields/FieldBoard.Dalamud.cs").Contains("Npcs.NpcShopLinks.ValidLayout(bytes)"),"checked before parsing");
        }),
    ];
}
